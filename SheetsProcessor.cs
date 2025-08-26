using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MasterDataSheetParser;

/// <summary>
/// Google SheetsからCSVをダウンロードし、JSONに変換する統合処理クラス
/// </summary>
public class SheetsProcessor
{
    /// <summary>
    /// Google Sheetsからデータを取得してJSONに変換する統合処理
    /// </summary>
    /// <param name="spreadsheetUrl">Google SheetsのURL</param>
    /// <param name="downloadFolder">CSVダウンロード先フォルダ（デフォルト: downloads/）</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト（nullの場合はデフォルトのシート名を使用）</param>
    /// <param name="cleanupCsv">変換後にCSVファイルを削除する場合はtrue（デフォルト: false）</param>
    /// <returns>処理が成功した場合はtrue</returns>
    public static async Task<bool> ProcessGoogleSheetsToJsonAsync(string spreadsheetUrl, string downloadFolder = "downloads", IEnumerable<string>? sheetNames = null, bool cleanupCsv = false)
    {
        try
        {
            Console.WriteLine("=== Google Sheetsデータ処理開始 ===");
            
            // スプレッドシートIDを抽出
            string? spreadsheetId = GoogleSheetsDownloader.ExtractSpreadsheetId(spreadsheetUrl);
            if (spreadsheetId == null)
            {
                Console.WriteLine("エラー: Google SheetsのURLからスプレッドシートIDを抽出できませんでした。");
                return false;
            }
            
            Console.WriteLine($"スプレッドシートID: {spreadsheetId}");
            
            // ダウンロードするシート名のリスト（パラメータで指定されていない場合はデフォルトを使用）
            var targetSheetNames = sheetNames ?? new List<string> { "messages", "commands", "inventories", "actors" };
            
            Console.WriteLine($"ダウンロード対象シート: {string.Join(", ", targetSheetNames)}");
            
            // 絶対パスに変換
            string absoluteDownloadFolder = Path.GetFullPath(downloadFolder);
            
            // ダウンロードフォルダが存在しない場合は作成
            if (!Directory.Exists(absoluteDownloadFolder))
            {
                Directory.CreateDirectory(absoluteDownloadFolder);
                Console.WriteLine($"フォルダを作成しました: {absoluteDownloadFolder}");
            }
            
            // 複数シートを一括ダウンロード
            Console.WriteLine("\n=== CSVファイルダウンロード開始 ===");
            bool downloadSuccess = await GoogleSheetsDownloader.DownloadMultipleSheetsAsync(
                spreadsheetId, targetSheetNames, absoluteDownloadFolder);
                
            if (!downloadSuccess)
            {
                Console.WriteLine("エラー: 一部またはすべてのCSVダウンロードに失敗しました。");
                return false;
            }
            
            // CSVからJSONへの変換
            Console.WriteLine("\n=== CSV -> JSON変換開始 ===");
            bool conversionSuccess = await ConvertCsvFilesToJsonAsync(absoluteDownloadFolder, targetSheetNames);
            
            if (!conversionSuccess)
            {
                Console.WriteLine("エラー: 一部またはすべてのCSV変換に失敗しました。");
                return false;
            }
            
            Console.WriteLine("\n=== 処理完了 ===");
            Console.WriteLine("すべてのファイルが正常に処理されました。");
            
            // クリーンアップオプションが有効な場合、CSVファイルを削除
            if (cleanupCsv)
            {
                Console.WriteLine("\n=== CSVファイルクリーンアップ開始 ===");
                CleanupCsvFiles(absoluteDownloadFolder, targetSheetNames);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: 処理中に例外が発生しました - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 指定フォルダ内のCSVファイルを一括でJSONに変換
    /// </summary>
    /// <param name="csvFolder">CSVファイルが格納されているフォルダ</param>
    /// <param name="fileNames">変換するファイル名のリスト（拡張子なし）</param>
    /// <returns>すべての変換が成功した場合はtrue</returns>
    private static async Task<bool> ConvertCsvFilesToJsonAsync(string csvFolder, IEnumerable<string> fileNames)
    {
        bool allSuccess = true;
        
        foreach (string fileName in fileNames)
        {
            string csvFilePath = Path.Combine(csvFolder, $"{fileName}.csv");
            string jsonFilePath = Path.Combine(csvFolder, $"{fileName}.json");
            
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"警告: CSVファイルが見つかりません: {csvFilePath}");
                allSuccess = false;
                continue;
            }
            
            try
            {
                Console.WriteLine($"変換中: {fileName}.csv -> {fileName}.json");
                
                // CSVファイルを解析してJSONに変換
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = 
                    CSVParser.ParseSystemFlags(csvFilePath, suppressOutput: true);
                
                var jsonOutput = JsonOutputter.OutputAsJson2(
                    csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
                
                // JSONファイルに保存
                await File.WriteAllTextAsync(jsonFilePath, jsonOutput);
                
                Console.WriteLine($"完了: {fileName}.json ({new FileInfo(jsonFilePath).Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {fileName}の変換に失敗しました - {ex.Message}");
                allSuccess = false;
            }
        }
        
        return allSuccess;
    }
    
    /// <summary>
    /// 指定されたフォルダ内の変換結果を確認
    /// </summary>
    /// <param name="folder">確認するフォルダパス</param>
    public static void ShowConversionResults(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Console.WriteLine($"フォルダが見つかりません: {folder}");
            return;
        }
        
        Console.WriteLine($"\n=== {folder} 内のファイル ===");
        
        var csvFiles = Directory.GetFiles(folder, "*.csv");
        var jsonFiles = Directory.GetFiles(folder, "*.json");
        
        Console.WriteLine($"CSVファイル: {csvFiles.Length}個");
        foreach (var file in csvFiles)
        {
            var fileInfo = new FileInfo(file);
            Console.WriteLine($"  - {fileInfo.Name} ({fileInfo.Length} bytes)");
        }
        
        Console.WriteLine($"JSONファイル: {jsonFiles.Length}個");
        foreach (var file in jsonFiles)
        {
            var fileInfo = new FileInfo(file);
            Console.WriteLine($"  - {fileInfo.Name} ({fileInfo.Length} bytes)");
        }
    }
    
    /// <summary>
    /// 指定されたフォルダ内のCSVファイルを削除してクリーンアップ
    /// </summary>
    /// <param name="folder">対象フォルダパス</param>
    /// <param name="fileNames">削除するファイル名のリスト（拡張子なし）</param>
    private static void CleanupCsvFiles(string folder, IEnumerable<string> fileNames)
    {
        int deletedCount = 0;
        int totalSize = 0;
        
        foreach (string fileName in fileNames)
        {
            string csvFilePath = Path.Combine(folder, $"{fileName}.csv");
            
            if (File.Exists(csvFilePath))
            {
                try
                {
                    var fileInfo = new FileInfo(csvFilePath);
                    int fileSize = (int)fileInfo.Length;
                    
                    File.Delete(csvFilePath);
                    Console.WriteLine($"削除: {fileName}.csv ({fileSize} bytes)");
                    
                    deletedCount++;
                    totalSize += fileSize;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"警告: {fileName}.csv の削除に失敗しました - {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"スキップ: {fileName}.csv (ファイルが存在しません)");
            }
        }
        
        Console.WriteLine($"クリーンアップ完了: {deletedCount}個のCSVファイルを削除 (合計 {totalSize} bytes)");
    }
}