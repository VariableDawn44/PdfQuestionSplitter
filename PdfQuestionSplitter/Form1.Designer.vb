<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form 覆寫 Dispose 以清除元件清單。
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    '為 Windows Form 設計工具的必要項
    Private components As System.ComponentModel.IContainer

    '注意: 以下為 Windows Form 設計工具所需的程序
    '可以使用 Windows Form 設計工具進行修改。
    '請勿使用程式碼編輯器進行修改。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.lblFiles = New System.Windows.Forms.Label()
        Me.lstFiles = New System.Windows.Forms.ListBox()
        Me.btnAddFiles = New System.Windows.Forms.Button()
        Me.btnAddFolder = New System.Windows.Forms.Button()
        Me.btnRemove = New System.Windows.Forms.Button()
        Me.btnClear = New System.Windows.Forms.Button()
        Me.lblOut = New System.Windows.Forms.Label()
        Me.txtOut = New System.Windows.Forms.TextBox()
        Me.btnBrowseOut = New System.Windows.Forms.Button()
        Me.lblDpi = New System.Windows.Forms.Label()
        Me.numDpi = New System.Windows.Forms.NumericUpDown()
        Me.chkSubFolder = New System.Windows.Forms.CheckBox()
        Me.btnStart = New System.Windows.Forms.Button()
        Me.btnOpenOut = New System.Windows.Forms.Button()
        Me.progress = New System.Windows.Forms.ProgressBar()
        Me.txtLog = New System.Windows.Forms.TextBox()
        CType(Me.numDpi, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lblFiles
        '
        Me.lblFiles.AutoSize = True
        Me.lblFiles.Location = New System.Drawing.Point(12, 12)
        Me.lblFiles.Name = "lblFiles"
        Me.lblFiles.Text = "術科 PDF 檔案（可直接把檔案拖曳到清單）："
        '
        'lstFiles
        '
        Me.lstFiles.AllowDrop = True
        Me.lstFiles.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstFiles.HorizontalScrollbar = True
        Me.lstFiles.IntegralHeight = False
        Me.lstFiles.Location = New System.Drawing.Point(12, 34)
        Me.lstFiles.Name = "lstFiles"
        Me.lstFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended
        Me.lstFiles.Size = New System.Drawing.Size(600, 160)
        Me.lstFiles.TabIndex = 0
        '
        'btnAddFiles
        '
        Me.btnAddFiles.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnAddFiles.Location = New System.Drawing.Point(624, 34)
        Me.btnAddFiles.Name = "btnAddFiles"
        Me.btnAddFiles.Size = New System.Drawing.Size(120, 32)
        Me.btnAddFiles.TabIndex = 1
        Me.btnAddFiles.Text = "加入 PDF…"
        '
        'btnAddFolder
        '
        Me.btnAddFolder.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnAddFolder.Location = New System.Drawing.Point(624, 72)
        Me.btnAddFolder.Name = "btnAddFolder"
        Me.btnAddFolder.Size = New System.Drawing.Size(120, 32)
        Me.btnAddFolder.TabIndex = 2
        Me.btnAddFolder.Text = "加入資料夾…"
        '
        'btnRemove
        '
        Me.btnRemove.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRemove.Location = New System.Drawing.Point(624, 120)
        Me.btnRemove.Name = "btnRemove"
        Me.btnRemove.Size = New System.Drawing.Size(120, 32)
        Me.btnRemove.TabIndex = 3
        Me.btnRemove.Text = "移除選取"
        '
        'btnClear
        '
        Me.btnClear.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnClear.Location = New System.Drawing.Point(624, 158)
        Me.btnClear.Name = "btnClear"
        Me.btnClear.Size = New System.Drawing.Size(120, 32)
        Me.btnClear.TabIndex = 4
        Me.btnClear.Text = "全部清除"
        '
        'lblOut
        '
        Me.lblOut.AutoSize = True
        Me.lblOut.Location = New System.Drawing.Point(12, 208)
        Me.lblOut.Name = "lblOut"
        Me.lblOut.Text = "輸出資料夾："
        '
        'txtOut
        '
        Me.txtOut.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtOut.Location = New System.Drawing.Point(12, 230)
        Me.txtOut.Name = "txtOut"
        Me.txtOut.Size = New System.Drawing.Size(600, 23)
        Me.txtOut.TabIndex = 5
        '
        'btnBrowseOut
        '
        Me.btnBrowseOut.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseOut.Location = New System.Drawing.Point(624, 226)
        Me.btnBrowseOut.Name = "btnBrowseOut"
        Me.btnBrowseOut.Size = New System.Drawing.Size(120, 32)
        Me.btnBrowseOut.TabIndex = 6
        Me.btnBrowseOut.Text = "選擇資料夾…"
        '
        'lblDpi
        '
        Me.lblDpi.AutoSize = True
        Me.lblDpi.Location = New System.Drawing.Point(12, 272)
        Me.lblDpi.Name = "lblDpi"
        Me.lblDpi.Text = "解析度 (DPI)："
        '
        'numDpi
        '
        Me.numDpi.Increment = New Decimal(New Integer() {50, 0, 0, 0})
        Me.numDpi.Location = New System.Drawing.Point(112, 269)
        Me.numDpi.Maximum = New Decimal(New Integer() {300, 0, 0, 0})
        Me.numDpi.Minimum = New Decimal(New Integer() {100, 0, 0, 0})
        Me.numDpi.Name = "numDpi"
        Me.numDpi.Size = New System.Drawing.Size(70, 23)
        Me.numDpi.TabIndex = 7
        Me.numDpi.Value = New Decimal(New Integer() {150, 0, 0, 0})
        '
        'chkSubFolder
        '
        Me.chkSubFolder.AutoSize = True
        Me.chkSubFolder.Checked = True
        Me.chkSubFolder.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSubFolder.Location = New System.Drawing.Point(204, 271)
        Me.chkSubFolder.Name = "chkSubFolder"
        Me.chkSubFolder.TabIndex = 8
        Me.chkSubFolder.Text = "每個 PDF 各建一個子資料夾（例如 114年）"
        '
        'btnStart
        '
        Me.btnStart.Location = New System.Drawing.Point(12, 306)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Size = New System.Drawing.Size(150, 36)
        Me.btnStart.TabIndex = 9
        Me.btnStart.Text = "開始轉換"
        '
        'btnOpenOut
        '
        Me.btnOpenOut.Location = New System.Drawing.Point(170, 306)
        Me.btnOpenOut.Name = "btnOpenOut"
        Me.btnOpenOut.Size = New System.Drawing.Size(150, 36)
        Me.btnOpenOut.TabIndex = 10
        Me.btnOpenOut.Text = "開啟輸出資料夾"
        '
        'progress
        '
        Me.progress.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.progress.Location = New System.Drawing.Point(332, 312)
        Me.progress.Name = "progress"
        Me.progress.Size = New System.Drawing.Size(412, 24)
        Me.progress.TabIndex = 11
        '
        'txtLog
        '
        Me.txtLog.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtLog.Location = New System.Drawing.Point(12, 354)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ReadOnly = True
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(732, 220)
        Me.txtLog.TabIndex = 12
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(756, 586)
        Me.Controls.Add(Me.lblFiles)
        Me.Controls.Add(Me.lstFiles)
        Me.Controls.Add(Me.btnAddFiles)
        Me.Controls.Add(Me.btnAddFolder)
        Me.Controls.Add(Me.btnRemove)
        Me.Controls.Add(Me.btnClear)
        Me.Controls.Add(Me.lblOut)
        Me.Controls.Add(Me.txtOut)
        Me.Controls.Add(Me.btnBrowseOut)
        Me.Controls.Add(Me.lblDpi)
        Me.Controls.Add(Me.numDpi)
        Me.Controls.Add(Me.chkSubFolder)
        Me.Controls.Add(Me.btnStart)
        Me.Controls.Add(Me.btnOpenOut)
        Me.Controls.Add(Me.progress)
        Me.Controls.Add(Me.txtLog)
        Me.Font = New System.Drawing.Font("Microsoft JhengHei UI", 9.0!)
        Me.MinimumSize = New System.Drawing.Size(640, 520)
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "術科題目轉圖片"
        CType(Me.numDpi, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents lblFiles As System.Windows.Forms.Label
    Friend WithEvents lstFiles As System.Windows.Forms.ListBox
    Friend WithEvents btnAddFiles As System.Windows.Forms.Button
    Friend WithEvents btnAddFolder As System.Windows.Forms.Button
    Friend WithEvents btnRemove As System.Windows.Forms.Button
    Friend WithEvents btnClear As System.Windows.Forms.Button
    Friend WithEvents lblOut As System.Windows.Forms.Label
    Friend WithEvents txtOut As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseOut As System.Windows.Forms.Button
    Friend WithEvents lblDpi As System.Windows.Forms.Label
    Friend WithEvents numDpi As System.Windows.Forms.NumericUpDown
    Friend WithEvents chkSubFolder As System.Windows.Forms.CheckBox
    Friend WithEvents btnStart As System.Windows.Forms.Button
    Friend WithEvents btnOpenOut As System.Windows.Forms.Button
    Friend WithEvents progress As System.Windows.Forms.ProgressBar
    Friend WithEvents txtLog As System.Windows.Forms.TextBox

End Class
