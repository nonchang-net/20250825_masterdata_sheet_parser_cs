using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace MasterDataSheetParser;

/// <summary>
/// Google Sheets API v4を使用してスプレッドシートからデータを取得するクラス
/// </summary>
public class GoogleSheetsApiDownloader
{
    private readonly SheetsService sheetsService;
    private readonly string applicationName;

    /// <summary>
    /// 認証方法の種類
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>サービスアカウントキーファイルを使用した認証</summary>
        ServiceAccountKey,
        /// <summary>サービスアカウントの環境変数を使用した認証</summary>
        ServiceAccountEnvironment,
        /// <summary>アプリケーションデフォルト認証情報を使用</summary>
        ApplicationDefault
    }

    /// <summary>
    /// GoogleSheetsApiDownloaderのコンストラクタ
    /// </summary>
    /// <param name="authenticationType">認証方法</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルのパス（ServiceAccountKey使用時）</param>
    /// <param name="applicationName">アプリケーション名</param>
    public GoogleSheetsApiDownloader(
        AuthenticationType authenticationType = AuthenticationType.ApplicationDefault,
        string? serviceAccountKeyPath = null,
        string applicationName = "MasterDataSheetParser")
    {
        this.applicationName = applicationName;
        this.sheetsService = CreateSheetsService(authenticationType, serviceAccountKeyPath);
    }

    /// <summary>
    /// GoogleCredentialを作成
    /// </summary>
    /// <param name="authenticationType">認証方法</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルのパス</param>
    /// <returns>GoogleCredential</returns>
    private GoogleCredential CreateCredential(AuthenticationType authenticationType, string? serviceAccountKeyPath)
    {
        return authenticationType switch
        {
            AuthenticationType.ServiceAccountKey => CreateServiceAccountKeyCredential(serviceAccountKeyPath!),
            AuthenticationType.ServiceAccountEnvironment => CreateServiceAccountEnvironmentCredential(),
            AuthenticationType.ApplicationDefault => GoogleCredential.GetApplicationDefault(),
            _ => throw new ArgumentException($"サポートされていない認証タイプです: {authenticationType}")
        };
    }

    /// <summary>
    /// サービスアカウントキーファイルから認証情報を作成
    /// </summary>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルのパス</param>
    /// <returns>GoogleCredential</returns>
    private GoogleCredential CreateServiceAccountKeyCredential(string serviceAccountKeyPath)
    {
        if (!File.Exists(serviceAccountKeyPath))
        {
            throw new FileNotFoundException($"サービスアカウントキーファイルが見つかりません: {serviceAccountKeyPath}");
        }

        using var stream = new FileStream(serviceAccountKeyPath, FileMode.Open, FileAccess.Read);
        return GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
    }

    /// <summary>
    /// 環境変数からサービスアカウント認証情報を作成
    /// </summary>
    /// <returns>GoogleCredential</returns>
    private GoogleCredential CreateServiceAccountEnvironmentCredential()
    {
        var serviceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
        if (string.IsNullOrEmpty(serviceAccountJson))
        {
            throw new InvalidOperationException("環境変数 GOOGLE_APPLICATION_CREDENTIALS_JSON が設定されていません");
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serviceAccountJson));
        return GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
    }

    /// <summary>
    /// SheetsServiceを作成
    /// </summary>
    /// <param name="authenticationType">認証方法</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルのパス</param>
    /// <returns>SheetsService</returns>
    private SheetsService CreateSheetsService(AuthenticationType authenticationType, string? serviceAccountKeyPath)
    {
        try
        {
            var credential = CreateCredential(authenticationType, serviceAccountKeyPath);

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Google Sheets APIサービスの初期化に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// スプレッドシートからシートのデータを取得してCSV形式で保存
    /// </summary>
    /// <param name="spreadsheetId">スプレッドシートID</param>
    /// <param name="sheetName">シート名</param>
    /// <param name="downloadPath">保存先のファイルパス</param>
    /// <returns>ダウンロードが成功した場合はtrue</returns>
    public async Task<bool> DownloadSheetAsCsvAsync(string spreadsheetId, string sheetName, string downloadPath)
    {
        try
        {
            Console.WriteLine($"Google Sheets API でダウンロード中: {sheetName} -> {downloadPath}");

            // シートのデータを取得
            var range = $"{sheetName}!A:ZZ"; // A列からZZ列まで取得
            var request = sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            if (response.Values == null || response.Values.Count == 0)
            {
                Console.WriteLine($"警告: シート '{sheetName}' にデータが見つかりませんでした");
                return false;
            }

            // CSV形式で保存
            await SaveValuesAsCsvAsync(response.Values, downloadPath);

            var fileSize = new FileInfo(downloadPath).Length;
            Console.WriteLine($"完了: {sheetName} ({fileSize} bytes)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: {sheetName} のダウンロード中に例外が発生しました - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ValueRange.ValuesをCSV形式でファイルに保存
    /// </summary>
    /// <param name="values">保存するデータ</param>
    /// <param name="filePath">保存先のファイルパス</param>
    private async Task SaveValuesAsCsvAsync(IList<IList<object>> values, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        
        foreach (var row in values)
        {
            var csvRow = ConvertToCsvRow(row);
            await writer.WriteLineAsync(csvRow);
        }
    }

    /// <summary>
    /// 行データをCSV形式の文字列に変換
    /// </summary>
    /// <param name="row">行データ</param>
    /// <returns>CSV形式の文字列</returns>
    private string ConvertToCsvRow(IList<object> row)
    {
        var csvFields = new List<string>();
        
        foreach (var cell in row)
        {
            var cellValue = cell?.ToString() ?? "";
            
            // セル値にカンマ、ダブルクォート、改行が含まれる場合はエスケープ
            if (cellValue.Contains(',') || cellValue.Contains('"') || cellValue.Contains('\n') || cellValue.Contains('\r'))
            {
                cellValue = cellValue.Replace("\"", "\"\"");
                cellValue = $"\"{cellValue}\"";
            }
            
            csvFields.Add(cellValue);
        }
        
        return string.Join(",", csvFields);
    }

    /// <summary>
    /// 複数のシートを一括ダウンロード
    /// </summary>
    /// <param name="spreadsheetId">スプレッドシートID</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト</param>
    /// <param name="downloadFolder">ダウンロード先のフォルダパス</param>
    /// <returns>すべてのダウンロードが成功した場合はtrue</returns>
    public async Task<bool> DownloadMultipleSheetsAsync(string spreadsheetId, IEnumerable<string> sheetNames, string downloadFolder)
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

    /// <summary>
    /// スプレッドシートの全シート情報を取得
    /// </summary>
    /// <param name="spreadsheetId">スプレッドシートID</param>
    /// <returns>シート情報のリスト</returns>
    public async Task<List<string>> GetSheetNamesAsync(string spreadsheetId)
    {
        try
        {
            var request = sheetsService.Spreadsheets.Get(spreadsheetId);
            var response = await request.ExecuteAsync();

            var sheetNames = new List<string>();
            foreach (var sheet in response.Sheets)
            {
                if (sheet.Properties?.Title != null)
                {
                    sheetNames.Add(sheet.Properties.Title);
                }
            }

            return sheetNames;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: スプレッドシートの情報取得に失敗しました - {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// リソースの解放
    /// </summary>
    public void Dispose()
    {
        sheetsService?.Dispose();
    }
}