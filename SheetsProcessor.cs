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
    /// データ取得方法の種類
    /// </summary>
    public enum DownloadMethod
    {
        /// <summary>HTTP経由でのCSVダウンロード（既存方式）</summary>
        HttpCsvDownload,
        /// <summary>Google Sheets API v4を使用したダウンロード</summary>
        GoogleSheetsApi
    }

    /// <summary>
    /// Google Sheetsからデータを取得してJSONに変換する統合処理
    /// </summary>
    /// <param name="spreadsheetUrl">Google SheetsのURL</param>
    /// <param name="downloadFolder">CSVダウンロード先フォルダ（デフォルト: downloads/）</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト（nullの場合はデフォルトのシート名を使用）</param>
    /// <param name="cleanupCsv">変換後にCSVファイルを削除する場合はtrue（デフォルト: false）</param>
    /// <param name="downloadMethod">データ取得方法（デフォルト: HttpCsvDownload）</param>
    /// <param name="serviceAccountKeyPath">Google Sheets API使用時のサービスアカウントキーファイルパス（nullの場合はアプリケーションデフォルト認証を使用）</param>
    /// <returns>処理が成功した場合はtrue</returns>
    public static async Task<bool> ProcessGoogleSheetsToJsonAsync(
        string spreadsheetUrl, 
        string downloadFolder = "downloads", 
        IEnumerable<string>? sheetNames = null, 
        bool cleanupCsv = false,
        DownloadMethod downloadMethod = DownloadMethod.HttpCsvDownload,
        string? serviceAccountKeyPath = null)
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
            Console.WriteLine($"\n=== CSVファイルダウンロード開始 ({downloadMethod}) ===");
            bool downloadSuccess = await DownloadSheetsAsync(
                spreadsheetId, targetSheetNames, absoluteDownloadFolder, downloadMethod, serviceAccountKeyPath);
                
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
    /// 指定された方法でシートをダウンロード
    /// </summary>
    /// <param name="spreadsheetId">スプレッドシートID</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト</param>
    /// <param name="downloadFolder">ダウンロード先フォルダ</param>
    /// <param name="downloadMethod">ダウンロード方法</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルパス</param>
    /// <returns>すべてのダウンロードが成功した場合はtrue</returns>
    private static async Task<bool> DownloadSheetsAsync(
        string spreadsheetId, 
        IEnumerable<string> sheetNames, 
        string downloadFolder, 
        DownloadMethod downloadMethod,
        string? serviceAccountKeyPath)
    {
        return downloadMethod switch
        {
            DownloadMethod.HttpCsvDownload => 
                await GoogleSheetsDownloader.DownloadMultipleSheetsAsync(spreadsheetId, sheetNames, downloadFolder),
            
            DownloadMethod.GoogleSheetsApi => 
                await DownloadViaGoogleSheetsApiAsync(spreadsheetId, sheetNames, downloadFolder, serviceAccountKeyPath),
                
            _ => throw new ArgumentException($"サポートされていないダウンロード方法です: {downloadMethod}")
        };
    }

    /// <summary>
    /// Google Sheets API v4を使用してシートをダウンロード
    /// </summary>
    /// <param name="spreadsheetId">スプレッドシートID</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト</param>
    /// <param name="downloadFolder">ダウンロード先フォルダ</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルパス</param>
    /// <returns>すべてのダウンロードが成功した場合はtrue</returns>
    private static async Task<bool> DownloadViaGoogleSheetsApiAsync(
        string spreadsheetId, 
        IEnumerable<string> sheetNames, 
        string downloadFolder,
        string? serviceAccountKeyPath)
    {
        GoogleSheetsApiDownloader? downloader = null;
        try
        {
            // 認証方法を決定
            var authType = string.IsNullOrEmpty(serviceAccountKeyPath) 
                ? GoogleSheetsApiDownloader.AuthenticationType.ApplicationDefault
                : GoogleSheetsApiDownloader.AuthenticationType.ServiceAccountKey;
            
            downloader = new GoogleSheetsApiDownloader(authType, serviceAccountKeyPath);
            
            return await downloader.DownloadMultipleSheetsAsync(spreadsheetId, sheetNames, downloadFolder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: Google Sheets API によるダウンロードに失敗しました - {ex.Message}");
            return false;
        }
        finally
        {
            downloader?.Dispose();
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