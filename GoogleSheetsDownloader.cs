using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MasterDataSheetParser;

/// <summary>
/// Google SheetsからCSVファイルをダウンロードするためのユーティリティクラス
/// </summary>
public class GoogleSheetsDownloader
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    /// <summary>
    /// Google Sheetsのスプレッドシートから指定されたシートのCSVをダウンロード
    /// </summary>
    /// <param name="spreadsheetId">Google SheetsのスプレッドシートID</param>
    /// <param name="sheetName">ダウンロードするシート名</param>
    /// <param name="downloadPath">ダウンロード先のファイルパス</param>
    /// <returns>ダウンロードが成功した場合はtrue</returns>
    public static async Task<bool> DownloadSheetAsCsvAsync(string spreadsheetId, string sheetName, string downloadPath)
    {
        try
        {
            // Google SheetsのCSVエクスポートURL構築
            string csvUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&sheet={sheetName}";
            
            Console.WriteLine($"ダウンロード中: {sheetName} -> {downloadPath}");
            
            // HTTPリクエストでCSVを取得
            using var response = await httpClient.GetAsync(csvUrl);
            
            if (response.IsSuccessStatusCode)
            {
                // レスポンス内容をファイルに保存
                await using var fileStream = new FileStream(downloadPath, FileMode.Create);
                await response.Content.CopyToAsync(fileStream);
                
                Console.WriteLine($"完了: {sheetName} ({new FileInfo(downloadPath).Length} bytes)");
                return true;
            }
            else
            {
                Console.WriteLine($"エラー: {sheetName} のダウンロードに失敗しました (HTTP {response.StatusCode})");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: {sheetName} のダウンロード中に例外が発生しました - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// スプレッドシートURLからスプレッドシートIDを抽出
    /// </summary>
    /// <param name="spreadsheetUrl">Google SheetsのURL</param>
    /// <returns>抽出されたスプレッドシートID、抽出に失敗した場合はnull</returns>
    public static string? ExtractSpreadsheetId(string spreadsheetUrl)
    {
        try
        {
            // URLの形式: https://docs.google.com/spreadsheets/d/{spreadsheetId}/edit?usp=sharing
            var uri = new Uri(spreadsheetUrl);
            var segments = uri.Segments;
            
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "d/")
                {
                    // "d/"の次のセグメントがスプレッドシートID
                    if (i + 1 < segments.Length)
                    {
                        return segments[i + 1].TrimEnd('/');
                    }
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: スプレッドシートIDの抽出に失敗しました - {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 複数のシートを一括ダウンロード
    /// </summary>
    /// <param name="spreadsheetId">Google SheetsのスプレッドシートID</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト</param>
    /// <param name="downloadFolder">ダウンロード先のフォルダパス</param>
    /// <returns>すべてのダウンロードが成功した場合はtrue</returns>
    public static async Task<bool> DownloadMultipleSheetsAsync(string spreadsheetId, IEnumerable<string> sheetNames, string downloadFolder)
    {
        bool allSuccess = true;
        
        // ダウンロード先フォルダが存在しない場合は作成
        if (!Directory.Exists(downloadFolder))
        {
            Directory.CreateDirectory(downloadFolder);
            Console.WriteLine($"フォルダを作成しました: {downloadFolder}");
        }
        
        foreach (string sheetName in sheetNames)
        {
            string downloadPath = Path.Combine(downloadFolder, $"{sheetName}.csv");
            bool success = await DownloadSheetAsCsvAsync(spreadsheetId, sheetName, downloadPath);
            
            if (!success)
            {
                allSuccess = false;
            }
        }
        
        return allSuccess;
    }
}