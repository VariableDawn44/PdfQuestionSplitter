Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging
Imports Windows.Data.Pdf
Imports Windows.Storage
Imports Windows.Storage.Streams
Imports Windows.Graphics.Imaging
Imports Windows.Media.Ocr

''' <summary>
''' 將術科 PDF 依「試題1 / 題目二 / 第三題」等標題切割，每一題輸出成一張長圖片。
''' 使用 Windows 內建功能：Windows.Data.Pdf 轉圖、Windows.Media.Ocr 辨識標題（掃描檔也適用）。
''' </summary>
Public Class QuestionExtractor

    ''' <summary>OCR 辨識出的一行文字（像素座標）</summary>
    Private Class OcrTextLine
        Public Text As String
        Public Left As Integer
        Public Top As Integer
        Public Bottom As Integer
    End Class

    ''' <summary>一頁的影像與辨識結果</summary>
    Private Class PageData
        Public Image As Bitmap
        Public Gray As Byte()          ' 灰階值，用來找出有內容的範圍
        Public Width As Integer
        Public Height As Integer
        Public Lines As New List(Of OcrTextLine)
        Public ContentTop As Integer    ' 扣除頁首頁碼後的內容上緣
        Public ContentBottom As Integer ' 扣除頁尾頁碼後的內容下緣
    End Class

    ''' <summary>找到的題目標題</summary>
    Private Class Heading
        Public Number As Integer
        Public Title As String
        Public PageIndex As Integer
        Public Top As Integer
    End Class

    ''' <summary>一題在某一頁上的範圍</summary>
    Private Class Segment
        Public PageIndex As Integer
        Public Rect As Rectangle
    End Class

    ''' <summary>輸出解析度（每英吋像素）</summary>
    Public Property Dpi As Integer = 150

    ''' <summary>是否輸出 OCR 辨識結果（除錯用）</summary>
    Public Property DumpOcr As Boolean = False

    ''' <summary>訊息輸出</summary>
    Public Property Log As Action(Of String)

    ' 標題格式：試題1：、題目二:、試題 6：、第三題：……
    Private Shared ReadOnly HeadingRegex As New Regex(
        "^(?:(?:試題|題目|试题|题目)(?<n>[0-9０-９lI|]{1,2}|[一二三四五六七八九十]{1,2})題?|第(?<n>[0-9０-９]{1,2}|[一二三四五六七八九十]{1,2})題)(?<c>[:：;；、.．])?(?<t>.*)$")
    ' 只有題號的標題：6：共享停車位(16 分)
    Private Shared ReadOnly NumberOnlyHeadingRegex As New Regex("^(?<n>[0-9]{1,2})[:：]+(?<t>.+)$")
    ' 配分：(17 分)
    Private Shared ReadOnly ScoreRegex As New Regex("[(（]\s*\d+\s*分.*$")
    ' 頁碼：1、- 1 -、第1頁、1/12、第1頁，共12頁
    Private Shared ReadOnly PageNumberRegex As New Regex("^[-－—]?第?\d{1,3}頁?([,，]?共?[/／]?\d{1,3}頁?)?[-－—]?$")

    ''' <summary>
    ''' 處理一個 PDF：依題目切割並存成圖片，傳回輸出的檔案清單。
    ''' </summary>
    Public Async Function ProcessAsync(pdfPath As String, outDir As String) As Task(Of List(Of String))
        Dim outputs As New List(Of String)
        Dim pages = Await LoadPagesAsync(pdfPath)
        Try
            Directory.CreateDirectory(outDir)
            Dim prefix = GetPrefix(pdfPath)
            If DumpOcr Then
                Dim sbDump As New System.Text.StringBuilder()
                For p = 0 To pages.Count - 1
                    sbDump.AppendLine($"===== 第 {p + 1} 頁 =====")
                    For Each ln In pages(p).Lines
                        sbDump.AppendLine($"[{ln.Left},{ln.Top}] {ln.Text}")
                    Next
                Next
                File.WriteAllText(Path.Combine(outDir, prefix & "_OCR.txt"), sbDump.ToString(), System.Text.Encoding.UTF8)
            End If
            Dim heads = FindHeadings(pages)

            If heads.Count = 0 Then
                Log?.Invoke("  ⚠ 找不到題目標題，改成每一頁輸出一張圖片")
                For i = 0 To pages.Count - 1
                    Dim f = Path.Combine(outDir, $"{prefix}_第{i + 1}頁.png")
                    pages(i).Image.Save(f, ImageFormat.Png)
                    outputs.Add(f)
                Next
                Return outputs
            End If

            For k = 0 To heads.Count - 1
                Dim h = heads(k)
                Dim nextHead = If(k < heads.Count - 1, heads(k + 1), Nothing)
                Dim segs = GetSegments(pages, h, nextHead)
                If segs.Count = 0 Then
                    Log?.Invoke($"  ⚠ 第 {h.Number} 題沒有內容，略過")
                    Continue For
                End If

                Dim name = $"{prefix}_Q{h.Number}"
                If h.Title <> "" Then name &= "_" & h.Title
                Dim f = Path.Combine(outDir, name & ".png")
                SaveStitched(pages, segs, f)
                outputs.Add(f)

                Dim firstPage = segs.First().PageIndex + 1
                Dim lastPage = segs.Last().PageIndex + 1
                Dim pageText = If(firstPage = lastPage, $"第 {firstPage} 頁", $"第 {firstPage}～{lastPage} 頁")
                Log?.Invoke($"  Q{h.Number} {h.Title}（{pageText}）→ {Path.GetFileName(f)}")
            Next
        Finally
            For Each p In pages
                p.Image.Dispose()
            Next
        End Try
        Return outputs
    End Function

    ''' <summary>檔名前綴：檔名開頭是年度（例如 114_術科題目.pdf）就用年度，否則用檔名</summary>
    Public Shared Function GetPrefix(pdfPath As String) As String
        Dim baseName = Path.GetFileNameWithoutExtension(pdfPath)
        Dim m = Regex.Match(baseName, "^(\d{2,3})(?!\d)")
        Return If(m.Success, m.Groups(1).Value, baseName)
    End Function

    ' ---------------------------------------------------------------
    '  1. PDF 每頁轉成圖片並做 OCR
    ' ---------------------------------------------------------------
    Private Async Function LoadPagesAsync(pdfPath As String) As Task(Of List(Of PageData))
        Dim engine = OcrEngine.TryCreateFromLanguage(New Windows.Globalization.Language("zh-Hant-TW"))
        If engine Is Nothing Then engine = OcrEngine.TryCreateFromUserProfileLanguages()
        If engine Is Nothing Then
            Throw New InvalidOperationException("這台電腦沒有可用的 Windows OCR 語言，請在 Windows 設定中安裝「中文(繁體，台灣)」語言套件。")
        End If

        Dim file = Await StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdfPath))
        Dim doc = Await PdfDocument.LoadFromFileAsync(file)
        Dim pages As New List(Of PageData)
        If doc.PageCount = 0UI Then Return pages

        Try
            Await LoadAllPagesAsync(doc, engine, pages)
        Catch
            ' 讀到一半失敗時，釋放已經讀進來的頁面圖片
            For Each p In pages
                p.Image?.Dispose()
            Next
            Throw
        End Try
        Return pages
    End Function

    Private Async Function LoadAllPagesAsync(doc As PdfDocument, engine As OcrEngine, pages As List(Of PageData)) As Task
        For i As UInteger = 0UI To doc.PageCount - 1UI
            Using page = doc.GetPage(i)
                Dim opts As New PdfPageRenderOptions()
                opts.DestinationWidth = CUInt(Math.Round(page.Size.Width * Dpi / 96.0))

                Using ras As New InMemoryRandomAccessStream()
                    Await page.RenderToStreamAsync(ras, opts)

                    ' 轉成 System.Drawing.Bitmap
                    Dim pd As New PageData()
                    ' 另開一個讀取串流複製到記憶體（直接包裝 ras 再關閉會把 ras 一起關掉）
                    Dim ms As New MemoryStream()
                    Using input = ras.GetInputStreamAt(0).AsStreamForRead()
                        Await input.CopyToAsync(ms)
                    End Using
                    ms.Position = 0
                    Using tmp As New Bitmap(ms)
                        pd.Image = New Bitmap(tmp.Width, tmp.Height, PixelFormat.Format32bppArgb)
                        pd.Image.SetResolution(Dpi, Dpi)
                        Using g = Graphics.FromImage(pd.Image)
                            g.Clear(Color.White)
                            g.DrawImage(tmp, 0, 0, tmp.Width, tmp.Height)
                        End Using
                    End Using
                    ms.Dispose()
                    pd.Width = pd.Image.Width
                    pd.Height = pd.Image.Height
                    pd.Gray = ToGray(pd.Image)

                    ' OCR（太大時先縮小，再把座標放大回來）
                    ras.Seek(0)
                    Dim dec = Await BitmapDecoder.CreateAsync(ras)
                    Dim maxDim = CDbl(OcrEngine.MaxImageDimension)
                    Dim scale = Math.Min(1.0, maxDim / Math.Max(dec.PixelWidth, dec.PixelHeight))
                    Dim tf As New BitmapTransform()
                    tf.ScaledWidth = CUInt(Math.Floor(dec.PixelWidth * scale))
                    tf.ScaledHeight = CUInt(Math.Floor(dec.PixelHeight * scale))
                    Using sb = Await dec.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, tf,
                                                                 ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)
                        Dim result = Await engine.RecognizeAsync(sb)
                        For Each ln In result.Lines
                            If ln.Words.Count = 0 Then Continue For
                            Dim t As New OcrTextLine()
                            t.Text = ln.Text
                            t.Left = CInt(ln.Words.Min(Function(w) w.BoundingRect.X) / scale)
                            t.Top = CInt(ln.Words.Min(Function(w) w.BoundingRect.Y) / scale)
                            t.Bottom = CInt(ln.Words.Max(Function(w) w.BoundingRect.Y + w.BoundingRect.Height) / scale)
                            pd.Lines.Add(t)
                        Next
                    End Using

                    MergeRowLines(pd)
                    FindPageNumberArea(pd)
                    pages.Add(pd)
                End Using
            End Using
            Log?.Invoke($"  已讀取第 {i + 1}/{doc.PageCount} 頁")
        Next
    End Function

    ''' <summary>
    ''' OCR 有時會把同一列拆成兩段（例如「試題六」和「派屈網路的模擬(17 分)」），
    ''' 把高度重疊的文字段依左右順序合併成一行。
    ''' </summary>
    Private Shared Sub MergeRowLines(pd As PageData)
        Dim merged As New List(Of OcrTextLine)
        For Each ln In pd.Lines.OrderBy(Function(x) x.Top)
            Dim h = ln.Bottom - ln.Top
            Dim row = merged.FirstOrDefault(
                Function(r) Math.Min(r.Bottom, ln.Bottom) - Math.Max(r.Top, ln.Top) > 0.5 * Math.Min(h, r.Bottom - r.Top))
            If row Is Nothing Then
                merged.Add(New OcrTextLine With {.Text = ln.Text, .Left = ln.Left, .Top = ln.Top, .Bottom = ln.Bottom})
            Else
                row.Text = If(ln.Left < row.Left, ln.Text & " " & row.Text, row.Text & " " & ln.Text)
                row.Left = Math.Min(row.Left, ln.Left)
                row.Top = Math.Min(row.Top, ln.Top)
                row.Bottom = Math.Max(row.Bottom, ln.Bottom)
            End If
        Next
        pd.Lines = merged
    End Sub

    ''' <summary>頁首、頁尾若只有頁碼就排除</summary>
    Private Sub FindPageNumberArea(pd As PageData)
        pd.ContentTop = 0
        pd.ContentBottom = pd.Height
        For Each ln In pd.Lines
            Dim t = Normalize(ln.Text).Replace(" ", "")
            If Not PageNumberRegex.IsMatch(t) Then Continue For
            If ln.Top > pd.Height * 0.88 Then
                pd.ContentBottom = Math.Min(pd.ContentBottom, ln.Top - 2)
            ElseIf ln.Bottom < pd.Height * 0.08 Then
                pd.ContentTop = Math.Max(pd.ContentTop, ln.Bottom + 2)
            End If
        Next
    End Sub

    ' ---------------------------------------------------------------
    '  2. 找出題目標題
    ' ---------------------------------------------------------------
    Private Function FindHeadings(pages As List(Of PageData)) As List(Of Heading)
        Dim found As New List(Of Heading)
        Dim last = 0
        Dim expected = 0
        For p = 0 To pages.Count - 1
            Dim pd = pages(p)
            For Each ln In pd.Lines.OrderBy(Function(x) x.Top)
                If ln.Left > pd.Width * 0.35 Then Continue For   ' 標題在左側
                Dim text = Normalize(ln.Text)
                Dim compact = text.Replace(" ", "")

                ' 試卷說明中的「本試卷共六題」→ 記下應有題數
                Dim mc = Regex.Match(compact, "共([0-9]{1,2}|[一二三四五六七八九十]{1,2})題")
                If mc.Success AndAlso expected = 0 Then expected = ParseNumber(mc.Groups(1).Value)

                Dim n As Integer
                Dim title As String
                Dim m = HeadingRegex.Match(compact)
                If m.Success Then
                    n = ParseNumber(m.Groups("n").Value)
                    ' 要有冒號或配分，避免抓到內文
                    If Not m.Groups("c").Success AndAlso Not ScoreRegex.IsMatch(text) Then Continue For
                    title = TitleFromLine(text)
                Else
                    ' 「試題」二字被排版到別處時，只剩「6：共享停車位(16 分)」：必須是下一題號且有配分
                    Dim m2 = NumberOnlyHeadingRegex.Match(compact)
                    If Not m2.Success OrElse Not ScoreRegex.IsMatch(text) Then Continue For
                    n = CInt(m2.Groups("n").Value)
                    If n <> last + 1 Then Continue For
                    title = ScoreRegex.Replace(Regex.Replace(text, "^\s*\d{1,2}(\s*[:：])+\s*", ""), "")
                End If
                If n <= last OrElse n > 20 Then Continue For
                If n > last + 1 Then Log?.Invoke($"  ⚠ 沒有找到第 {last + 1} 題的標題（可能併入前一題的圖片）")

                Dim h As New Heading()
                h.Number = n
                h.Title = CleanTitle(title)
                h.PageIndex = p
                h.Top = ln.Top
                found.Add(h)
                last = n
            Next
        Next
        If expected > 0 AndAlso last < expected AndAlso found.Count > 0 Then
            Log?.Invoke($"  ⚠ 試卷說明共 {expected} 題，只找到 {found.Count} 題的標題，請檢查最後一張圖片")
        End If
        Return found
    End Function

    ''' <summary>從原始行文字取出標題（保留英文單字間的空白）</summary>
    Private Shared Function TitleFromLine(text As String) As String
        Dim m = Regex.Match(text, "^\s*(?:試\s*題|題\s*目|试\s*题|题\s*目|第)\s*(?:[0-9０-９lI|]{1,2}|[一二三四五六七八九十]{1,2})\s*題?\s*[:：;；、.．]?\s*(.*)$")
        Dim t = If(m.Success, m.Groups(1).Value, text)
        t = ScoreRegex.Replace(t, "")
        Return t.Trim(" "c, ":"c, "："c, "-"c)
    End Function

    ''' <summary>OCR 的中文字之間常有空白，把中文字旁的空白去掉，英文單字間的空白保留</summary>
    Private Shared Function Normalize(s As String) As String
        s = Regex.Replace(s, "(?<=[^\x00-\x7F])\s+|\s+(?=[^\x00-\x7F])", "")
        Return Regex.Replace(s, "\s{2,}", " ").Trim()
    End Function

    Private Shared Function ParseNumber(s As String) As Integer
        s = s.Replace("l", "1").Replace("I", "1").Replace("|", "1")
        Dim digits = ""
        For Each ch In s
            If ch >= "０"c AndAlso ch <= "９"c Then
                digits &= ChrW(AscW(ch) - AscW("０"c) + AscW("0"c))
            ElseIf Char.IsDigit(ch) Then
                digits &= ch
            End If
        Next
        If digits <> "" Then Return CInt(digits)

        Const cn = "一二三四五六七八九"
        If s = "十" Then Return 10
        If s.StartsWith("十") Then Return 10 + cn.IndexOf(s(1)) + 1
        If s.Length = 2 AndAlso s(1) = "十"c Then Return (cn.IndexOf(s(0)) + 1) * 10
        Return cn.IndexOf(s(0)) + 1
    End Function

    ''' <summary>標題整理成可以當檔名的文字</summary>
    Private Shared Function CleanTitle(t As String) As String
        ' 修正常見的 OCR 英文誤認
        t = Regex.Replace(t, "(?<=[A-Za-z])I(?=[a-z])", "l")      ' PIanning → Planning、ProbIem → Problem
        t = Regex.Replace(t, "\blm(?=[a-z])", "Im")               ' lmage → Image
        t = Regex.Replace(t, "(?<=[A-Za-z])[。о](?=[A-Za-z])", "o") ' enc。ding → encoding
        t = Regex.Replace(t, "([(（])\s+", "$1")                  ' ( Huffman → (Huffman
        t = Regex.Replace(t, "\s+([)）])", "$1")
        t = Regex.Replace(t, "(?<=[a-z])Ⅱ", "il")                 ' EmaⅡ → Email
        t = t.Replace("幂", "冪").Replace("·", "").Replace("・", "")
        t = t.Trim(" "c, ":"c, "："c, "-"c).TrimEnd("。"c, "."c)
        For Each ch In Path.GetInvalidFileNameChars()
            t = t.Replace(ch, "_"c)
        Next
        t = t.Trim()
        If t.Length > 60 Then t = t.Substring(0, 60).Trim()
        Return t
    End Function

    ' ---------------------------------------------------------------
    '  3. 切割範圍並拼接成長圖
    ' ---------------------------------------------------------------
    Private Function GetSegments(pages As List(Of PageData), h As Heading, nextHead As Heading) As List(Of Segment)
        Dim pad = CInt(Dpi * 0.08)
        Dim endPage = If(nextHead Is Nothing, pages.Count - 1, nextHead.PageIndex)
        Dim segs As New List(Of Segment)

        For p = h.PageIndex To endPage
            Dim pd = pages(p)
            Dim top = If(p = h.PageIndex, Math.Max(pd.ContentTop, h.Top - pad), pd.ContentTop)
            Dim bottom = pd.ContentBottom
            If nextHead IsNot Nothing AndAlso p = nextHead.PageIndex Then
                bottom = Math.Min(bottom, nextHead.Top - pad)
            End If
            If bottom - top < 4 Then Continue For

            Dim r = InkBounds(pd, top, bottom)
            If r.HasValue Then
                Dim sg As New Segment()
                sg.PageIndex = p
                sg.Rect = r.Value
                segs.Add(sg)
            End If
        Next
        Return segs
    End Function

    ''' <summary>在指定的上下範圍內找出有內容（深色像素）的區域</summary>
    Private Function InkBounds(pd As PageData, top As Integer, bottom As Integer) As Rectangle?
        Const threshold As Byte = 200
        Dim minX = Integer.MaxValue, maxX = -1, minY = -1, maxY = -1
        For y = Math.Max(0, top) To Math.Min(pd.Height, bottom) - 1
            Dim rowStart = y * pd.Width
            Dim count = 0, rowMin = -1, rowMax = -1
            For x = 0 To pd.Width - 1
                If pd.Gray(rowStart + x) < threshold Then
                    count += 1
                    If rowMin < 0 Then rowMin = x
                    rowMax = x
                End If
            Next
            If count >= 2 Then
                If minY < 0 Then minY = y
                maxY = y
                minX = Math.Min(minX, rowMin)
                maxX = Math.Max(maxX, rowMax)
            End If
        Next
        If minY < 0 Then Return Nothing

        Dim margin = CInt(Dpi * 0.1)
        Dim l = Math.Max(0, minX - margin)
        Dim t = Math.Max(Math.Max(0, top), minY - margin)
        Dim r = Math.Min(pd.Width, maxX + margin)
        Dim b = Math.Min(Math.Min(pd.Height, bottom), maxY + margin)
        Return New Rectangle(l, t, r - l, b - t)
    End Function

    Private Sub SaveStitched(pages As List(Of PageData), segs As List(Of Segment), file As String)
        Dim gap = CInt(Dpi * 0.1)
        Dim left = segs.Min(Function(s) s.Rect.Left)
        Dim right = segs.Max(Function(s) s.Rect.Right)
        Dim width = right - left
        Dim height = segs.Sum(Function(s) s.Rect.Height) + gap * (segs.Count - 1)

        Using bmp As New Bitmap(width, height, PixelFormat.Format24bppRgb)
            bmp.SetResolution(Dpi, Dpi)
            Using g = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                Dim y = 0
                For Each s In segs
                    Dim img = pages(s.PageIndex).Image
                    Dim w = Math.Min(width, img.Width - left)
                    If w > 0 Then   ' 頁面比其他頁窄時可能沒有可畫的範圍
                        Dim src As New Rectangle(left, s.Rect.Top, w, s.Rect.Height)
                        g.DrawImage(img, New Rectangle(0, y, w, s.Rect.Height), src, GraphicsUnit.Pixel)
                    End If
                    y += s.Rect.Height + gap
                Next
            End Using
            bmp.Save(file, ImageFormat.Png)
        End Using
    End Sub

    Private Shared Function ToGray(bmp As Bitmap) As Byte()
        Dim w = bmp.Width, h = bmp.Height
        Dim data = bmp.LockBits(New Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride = data.Stride
        Dim raw(stride * h - 1) As Byte
        Runtime.InteropServices.Marshal.Copy(data.Scan0, raw, 0, raw.Length)
        bmp.UnlockBits(data)

        Dim gray(w * h - 1) As Byte
        For y = 0 To h - 1
            Dim row = y * stride
            For x = 0 To w - 1
                Dim i = row + x * 4
                gray(y * w + x) = CByte((raw(i + 2) * 299 + raw(i + 1) * 587 + raw(i) * 114) \ 1000)
            Next
        Next
        Return gray
    End Function

End Class
