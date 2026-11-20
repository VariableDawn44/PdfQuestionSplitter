Imports System.IO

''' <summary>
''' 術科題目轉圖片：把術科 PDF 依題目切割，每題輸出一張長圖片。
''' 也可用命令列批次執行：
'''   PdfQuestionSplitter.exe --out 輸出資料夾 [--dpi 150] [--nosub] 檔案或資料夾 ...
''' </summary>
Public Class Form1

    Private _dumpOcr As Boolean = False   ' 命令列 --dump：輸出 OCR 結果（除錯用）
    Private _batchMode As Boolean = False ' 命令列批次模式（不顯示訊息視窗）
    Private _busy As Boolean = False      ' 轉換中

    ' 轉換中關閉視窗會讓背景工作寫入已關閉的畫面，所以先擋下來
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If _busy AndAlso e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            MessageBox.Show(Me, "正在轉換中，請等待完成後再關閉。", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ' ===== 加入 / 移除 PDF =====

    Private Sub btnAddFiles_Click(sender As Object, e As EventArgs) Handles btnAddFiles.Click
        Using dlg As New OpenFileDialog()
            dlg.Title = "選擇術科 PDF 檔案"
            dlg.Filter = "PDF 檔案 (*.pdf)|*.pdf"
            dlg.Multiselect = True
            If dlg.ShowDialog(Me) = DialogResult.OK Then AddFiles(dlg.FileNames)
        End Using
    End Sub

    Private Sub btnAddFolder_Click(sender As Object, e As EventArgs) Handles btnAddFolder.Click
        Using dlg As New FolderBrowserDialog()
            dlg.Description = "選擇含有術科 PDF 的資料夾（會加入資料夾內所有 PDF）"
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                AddFiles(Directory.GetFiles(dlg.SelectedPath, "*.pdf", SearchOption.TopDirectoryOnly))
            End If
        End Using
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        For i = lstFiles.SelectedIndices.Count - 1 To 0 Step -1
            lstFiles.Items.RemoveAt(lstFiles.SelectedIndices(i))
        Next
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        lstFiles.Items.Clear()
    End Sub

    Private Sub lstFiles_DragEnter(sender As Object, e As DragEventArgs) Handles lstFiles.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then e.Effect = DragDropEffects.Copy
    End Sub

    Private Sub lstFiles_DragDrop(sender As Object, e As DragEventArgs) Handles lstFiles.DragDrop
        Dim paths = CType(e.Data.GetData(DataFormats.FileDrop), String())
        For Each p In paths
            If Directory.Exists(p) Then
                AddFiles(Directory.GetFiles(p, "*.pdf"))
            Else
                AddFiles({p})
            End If
        Next
    End Sub

    Private Sub AddFiles(files As IEnumerable(Of String))
        ' 依年度數字排序（99 會排在 100 前面）
        Dim sorted = files.Where(Function(f) f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) _
                          .OrderBy(Function(f) SortKey(f))
        For Each f In sorted
            If Not lstFiles.Items.Contains(f) Then lstFiles.Items.Add(f)
        Next
    End Sub

    Private Shared Function SortKey(f As String) As String
        Dim prefix = QuestionExtractor.GetPrefix(f)
        Dim n As Integer
        Return If(Integer.TryParse(prefix, n), n.ToString("D5"), prefix)
    End Function

    ' ===== 輸出資料夾 =====

    Private Sub btnBrowseOut_Click(sender As Object, e As EventArgs) Handles btnBrowseOut.Click
        Using dlg As New FolderBrowserDialog()
            dlg.Description = "選擇圖片要輸出的資料夾"
            If txtOut.Text <> "" AndAlso Directory.Exists(txtOut.Text) Then dlg.SelectedPath = txtOut.Text
            If dlg.ShowDialog(Me) = DialogResult.OK Then txtOut.Text = dlg.SelectedPath
        End Using
    End Sub

    Private Sub btnOpenOut_Click(sender As Object, e As EventArgs) Handles btnOpenOut.Click
        If txtOut.Text <> "" AndAlso Directory.Exists(txtOut.Text) Then
            Process.Start("explorer.exe", """" & txtOut.Text & """")
        Else
            MessageBox.Show(Me, "輸出資料夾還不存在。", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ' ===== 開始轉換 =====

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If lstFiles.Items.Count = 0 Then
            MessageBox.Show(Me, "請先加入術科 PDF 檔案。", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If txtOut.Text.Trim() = "" Then
            btnBrowseOut.PerformClick()
            If txtOut.Text.Trim() = "" Then Return
        End If
        Await RunAsync(lstFiles.Items.Cast(Of String)().ToList(), txtOut.Text.Trim(), CInt(numDpi.Value), chkSubFolder.Checked)
    End Sub

    Private Async Function RunAsync(files As List(Of String), outRoot As String, dpi As Integer, subFolder As Boolean) As Task(Of Integer)
        txtLog.Clear()
        If files.Count = 0 Then
            AppendLog("沒有可轉換的 PDF 檔案。")
            Return 1
        End If
        ' 先確認輸出資料夾可以建立（路徑錯誤時不要讓程式當掉）
        Try
            outRoot = Path.GetFullPath(outRoot)
            Directory.CreateDirectory(outRoot)
        Catch ex As Exception
            AppendLog("✖ 無法建立輸出資料夾：" & ex.Message)
            If Not _batchMode Then MessageBox.Show(Me, "無法建立輸出資料夾：" & ex.Message, Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return 1
        End Try

        SetBusy(True)
        progress.Maximum = files.Count
        progress.Value = 0
        Dim ok = 0, imageCount = 0

        Dim extractor As New QuestionExtractor()
        extractor.Dpi = dpi
        extractor.DumpOcr = _dumpOcr
        extractor.Log = Sub(msg) AppendLog(msg)

        For Each f In files
            AppendLog($"【{Path.GetFileName(f)}】")
            Dim outDir = outRoot
            If subFolder Then
                Dim prefix = QuestionExtractor.GetPrefix(f)
                Dim n As Integer
                outDir = Path.Combine(outRoot, If(Integer.TryParse(prefix, n), prefix & "年", prefix))
            End If
            Try
                ' 在背景執行緒處理，畫面才不會卡住
                Dim result = Await Task.Run(Function() extractor.ProcessAsync(f, outDir))
                imageCount += result.Count
                ok += 1
            Catch ex As Exception
                AppendLog("  ✖ 失敗：" & FriendlyError(ex))
            End Try
            progress.Value += 1
        Next

        AppendLog("")
        AppendLog($"完成：{ok}/{files.Count} 個 PDF，共輸出 {imageCount} 張圖片 → {outRoot}")
        Try
            File.WriteAllText(Path.Combine(outRoot, "轉換紀錄.txt"), txtLog.Text, System.Text.Encoding.UTF8)
        Catch
        End Try
        SetBusy(False)
        Return If(ok = files.Count, 0, 1)
    End Function

    ''' <summary>把常見的錯誤代碼轉成看得懂的說明</summary>
    Private Shared Function FriendlyError(ex As Exception) As String
        Select Case ex.HResult
            Case &H80070002, &H80070003 : Return "找不到檔案。"
            Case &H80070020 : Return "檔案正被其他程式使用中。"
            Case &H80048014 : Return "無法開啟 PDF（檔案可能損毀、加密，或不是 PDF 檔）。"
        End Select
        Return ex.Message
    End Function

    Private Sub SetBusy(busy As Boolean)
        _busy = busy
        lstFiles.AllowDrop = Not busy
        For Each c As Control In New Control() {btnAddFiles, btnAddFolder, btnRemove, btnClear, btnBrowseOut, btnStart, numDpi, chkSubFolder, txtOut}
            c.Enabled = Not busy
        Next
        UseWaitCursor = busy
    End Sub

    Private Sub AppendLog(msg As String)
        If InvokeRequired Then
            BeginInvoke(New Action(Of String)(AddressOf AppendLog), msg)
            Return
        End If
        txtLog.AppendText(msg & Environment.NewLine)
    End Sub

    ' ===== 命令列批次模式 =====

    Private Async Sub Form1_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Dim args = Environment.GetCommandLineArgs().Skip(1).ToList()
        If args.Count = 0 Then Return

        Dim outDir = ""
        Dim dpi = 150
        Dim subFolder = True
        Dim inputs As New List(Of String)
        Dim i = 0
        While i < args.Count
            Dim hasValue = i + 1 < args.Count
            Select Case args(i).ToLowerInvariant()
                Case "--out"
                    If hasValue Then i += 1 : outDir = args(i)
                Case "--dpi"
                    Dim v As Integer
                    If hasValue AndAlso Integer.TryParse(args(i + 1), v) Then dpi = v
                    If hasValue Then i += 1
                Case "--nosub" : subFolder = False
                Case "--dump" : _dumpOcr = True
                Case Else : inputs.Add(args(i))
            End Select
            i += 1
        End While
        If outDir = "" OrElse inputs.Count = 0 Then
            AppendLog("命令列用法：PdfQuestionSplitter.exe --out 輸出資料夾 [--dpi 150] [--nosub] 檔案或資料夾 ...")
            Return
        End If

        _batchMode = True
        dpi = CInt(Math.Max(numDpi.Minimum, Math.Min(numDpi.Maximum, dpi)))   ' 限制在 100～300
        Dim files As New List(Of String)
        For Each p In inputs
            If Directory.Exists(p) Then files.AddRange(Directory.GetFiles(p, "*.pdf")) Else files.Add(p)
        Next
        lstFiles.Items.Clear()
        AddFiles(files)
        txtOut.Text = outDir
        numDpi.Value = dpi
        chkSubFolder.Checked = subFolder

        Dim code = Await RunAsync(lstFiles.Items.Cast(Of String)().ToList(), outDir, dpi, subFolder)
        If code <> 0 Then
            ' 失敗時（例如輸出資料夾無法建立）把紀錄寫到程式旁邊，方便查看
            Try
                File.WriteAllText(Path.Combine(Application.StartupPath, "轉換紀錄_最後一次.txt"), txtLog.Text, System.Text.Encoding.UTF8)
            Catch
            End Try
        End If
        Environment.ExitCode = code
        Close()
    End Sub

End Class
