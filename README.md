# 術科題目轉圖片 (PdfQuestionSplitter)

把工科技藝競賽「05 電腦軟體設計」的術科 PDF，依題目（試題1、題目二、第三題…）切割，每一題輸出成一張長圖片。

## 需求

- Windows 10 / 11，並安裝「中文(繁體，台灣)」語言（用於 Windows 內建 OCR 辨識題目標題）
- Visual Studio 2022 以上（.NET Framework 4.7.2，Visual Basic WinForms）

## 使用方式

1. 開啟 `PdfQuestionSplitter.slnx`，建置並執行。
2. 按「加入 PDF…」或「加入資料夾…」，也可以直接把檔案拖曳到清單。
3. 按「選擇資料夾…」指定輸出位置，再按「開始轉換」。
4. 輸出檔名例如 `114_Q5_加權框融合(Weighted Boxes Fusion, WBF).png`，並產生 `轉換紀錄.txt`。

## 命令列批次模式

```
PdfQuestionSplitter.exe --out 輸出資料夾 [--dpi 150] [--nosub] [--dump] 檔案或資料夾 ...
```

| 參數 | 說明 |
|---|---|
| `--out` | 輸出資料夾（必填） |
| `--dpi` | 解析度，100～300，預設 150 |
| `--nosub` | 不要依年度建立子資料夾 |
| `--dump` | 另外輸出 OCR 辨識結果（除錯用） |

結束代碼：全部成功為 0，有任何失敗為 1。

## 運作方式

1. `Windows.Data.Pdf` 將每頁轉成圖片。
2. `Windows.Media.Ocr` 辨識文字，找出題目標題的位置（掃描檔也適用）。
3. 從一題標題切到下一題標題，跨頁時上下拼接，並去除頁碼與空白邊。

## 已知限制

- 檔名中的題目名稱來自 OCR，少數字可能辨識錯誤（圖片內容不受影響）。
- 原始試題沒有寫題目名稱時（例如 111 年第 6 題），檔名只有題號。

## 授權

MIT License，詳見 [LICENSE](LICENSE)。
