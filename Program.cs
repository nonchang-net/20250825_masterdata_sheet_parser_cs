using System;
using System.IO;
using System.Threading.Tasks;

namespace MasterDataSheetParser;

/// <summary>
/// CSVファイルを読み込んでJSONまたはダンプ形式で出力するCLIツール
/// </summary>
public class Program
{
    /// <summary>
    /// アプリケーションのエントリーポイント
    /// </summary>
    /// <param name="args">コマンドライン引数。[出力モード] CSVファイルパスを期待</param>
    /// <returns>正常終了時は0、エラー時は1</returns>
    public static int Main(string[] args)
    {
        // コマンドライン引数の検証
        if (args.Length == 0)
        {
            Console.WriteLine("使用方法: dotnet run [出力モード] <CSVファイルパス/フォルダパス/Google SheetsURL> [オプション/シート名...]");
            Console.WriteLine("出力モード: json2 (デフォルト), json, dump, batchConvert, sheetsDownload, または sheetsApi");
            Console.WriteLine("");
            Console.WriteLine("基本的な使用例:");
            Console.WriteLine("  dotnet run data.csv                           (JSON2出力)");
            Console.WriteLine("  dotnet run json data.csv                      (JSON出力)");
            Console.WriteLine("  dotnet run json2 data.csv                     (JSON2連想配列出力)");
            Console.WriteLine("  dotnet run dump data.csv                      (ダンプ出力)");
            Console.WriteLine("  dotnet run batchConvert ./csv/                (フォルダ内CSVを一括JSON2変換)");
            Console.WriteLine("");
            Console.WriteLine("Google Sheetsダウンロードの使用例:");
            Console.WriteLine("  dotnet run sheetsDownload <Google SheetsURL>");
            Console.WriteLine("    (デフォルト: messages, commands, inventories, actors を downloads/ にダウンロード)");
            Console.WriteLine("  dotnet run sheetsDownload <URL> sheet1 sheet2");
            Console.WriteLine("    (指定したシートのみダウンロード)");
            Console.WriteLine("  dotnet run sheetsDownload <URL> --folder=output");
            Console.WriteLine("    (output/ フォルダにダウンロード)");
            Console.WriteLine("  dotnet run sheetsDownload <URL> --cleanup");
            Console.WriteLine("    (変換後にCSVファイルを削除)");
            Console.WriteLine("  dotnet run sheetsDownload <URL> --folder=data --cleanup sheet1 sheet2");
            Console.WriteLine("    (data/ フォルダに指定シートをダウンロード後、CSVを削除)");
            Console.WriteLine("");
            Console.WriteLine("Google Sheets API (高信頼モード):");
            Console.WriteLine("  dotnet run sheetsApi <Google SheetsURL>");
            Console.WriteLine("    (Google Sheets API経由: より高信頼でデータ欠損のないダウンロード)");
            Console.WriteLine("  dotnet run sheetsApi <URL> --key=service-account.json");
            Console.WriteLine("    (サービスアカウントキーファイルを使用)");
            Console.WriteLine("  dotnet run sheetsApi <URL> --cleanup --key=path/to/key.json sheet1 sheet2");
            Console.WriteLine("    (指定シートをAPIでダウンロード後、CSVを削除)");
            return 1;
        }

        // 引数を解析
        string outputMode = "json2"; // デフォルトはJSON2出力
        string csvFilePath;
        List<string>? additionalSheets = null;
        string downloadFolder = "downloads"; // デフォルトフォルダ
        bool cleanupCsv = false;
        string? serviceAccountKeyPath = null;
        
        if (args.Length == 1)
        {
            // ファイルパスのみの場合
            csvFilePath = args[0];
        }
        else if (args.Length >= 2)
        {
            // 出力モードとファイルパスが指定された場合
            string firstArg = args[0].ToLower();
            if (firstArg == "json" || firstArg == "json2" || firstArg == "dump" || firstArg == "batchconvert" || firstArg == "sheetsdownload" || firstArg == "sheetsapi")
            {
                outputMode = firstArg;
                csvFilePath = args[1];
                
                // sheetsDownloadまたはsheetsApiモードで追加のオプションが指定されている場合
                if ((firstArg == "sheetsdownload" || firstArg == "sheetsapi") && args.Length > 2)
                {
                    // オプションとシート名を解析
                    ParseSheetsDownloadOptions(args[2..], out downloadFolder, out cleanupCsv, out additionalSheets, out serviceAccountKeyPath);
                }
            }
            else
            {
                Console.WriteLine("エラー: 無効な出力モードです。'json', 'json2', 'dump', 'batchConvert', 'sheetsDownload', または 'sheetsApi' を指定してください。");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("エラー: 引数が不足しています。");
            return 1;
        }

        // ファイルまたはフォルダの存在確認
        if (outputMode == "batchconvert")
        {
            if (!Directory.Exists(csvFilePath))
            {
                Console.WriteLine($"エラー: フォルダ '{csvFilePath}' が見つかりません。");
                return 1;
            }
        }
        else if (outputMode == "sheetsdownload" || outputMode == "sheetsapi")
        {
            // Google Sheets URLの場合は存在確認をスキップ
            if (!csvFilePath.Contains("docs.google.com/spreadsheets"))
            {
                Console.WriteLine("エラー: Google SheetsのURLを指定してください。");
                return 1;
            }
        }
        else
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"エラー: ファイル '{csvFilePath}' が見つかりません。");
                return 1;
            }
        }

        try
        {
            // 出力モードに応じた処理
            if (outputMode == "json")
            {
                // JSON出力時はログ出力を抑制
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = CSVParser.ParseSystemFlags(csvFilePath, suppressOutput: true);
                var jsonOutput = JsonOutputter.OutputAsJson(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
                Console.WriteLine(jsonOutput);
            }
            else if (outputMode == "json2")
            {
                // JSON2出力時はログ出力を抑制
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = CSVParser.ParseSystemFlags(csvFilePath, suppressOutput: true);
                var json2Output = JsonOutputter.OutputAsJson2(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
                Console.WriteLine(json2Output);
            }
            else if (outputMode == "batchconvert")
            {
                // バッチ変換処理
                BatchProcessor.BatchConvertCsvToJson2(csvFilePath);
            }
            else if (outputMode == "sheetsdownload")
            {
                // Google Sheetsダウンロード処理（HTTP方式、非同期）
                return ProcessGoogleSheetsAsync(csvFilePath, downloadFolder, additionalSheets, cleanupCsv, SheetsProcessor.DownloadMethod.HttpCsvDownload, serviceAccountKeyPath).GetAwaiter().GetResult();
            }
            else if (outputMode == "sheetsapi")
            {
                // Google Sheets APIダウンロード処理（API方式、非同期）
                return ProcessGoogleSheetsAsync(csvFilePath, downloadFolder, additionalSheets, cleanupCsv, SheetsProcessor.DownloadMethod.GoogleSheetsApi, serviceAccountKeyPath).GetAwaiter().GetResult();
            }
            else
            {
                // ダンプ出力時は詳細ログを出力
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = CSVParser.ParseSystemFlags(csvFilePath, suppressOutput: false);
                DumpOutputter.DumpActualData(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: ファイルの読み込み中に問題が発生しました - {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Google Sheetsからデータをダウンロードして変換する非同期処理
    /// </summary>
    /// <param name="spreadsheetUrl">Google SheetsのURL</param>
    /// <param name="downloadFolder">ダウンロード先フォルダ</param>
    /// <param name="sheetNames">ダウンロードするシート名のリスト</param>
    /// <param name="cleanupCsv">CSVファイルをクリーンアップするかどうか</param>
    /// <param name="downloadMethod">ダウンロード方法</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルのパス</param>
    /// <returns>処理が成功した場合は0、失敗した場合は1</returns>
    private static async Task<int> ProcessGoogleSheetsAsync(
        string spreadsheetUrl, 
        string downloadFolder = "downloads", 
        IEnumerable<string>? sheetNames = null, 
        bool cleanupCsv = false,
        SheetsProcessor.DownloadMethod downloadMethod = SheetsProcessor.DownloadMethod.HttpCsvDownload,
        string? serviceAccountKeyPath = null)
    {
        try
        {
            // Google Sheetsから変換処理を実行
            bool success = await SheetsProcessor.ProcessGoogleSheetsToJsonAsync(spreadsheetUrl, downloadFolder, sheetNames, cleanupCsv, downloadMethod, serviceAccountKeyPath);
            
            if (success)
            {
                // 結果を表示
                SheetsProcessor.ShowConversionResults(downloadFolder);
                return 0;
            }
            else
            {
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: Google Sheets処理中に例外が発生しました - {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// sheetsDownloadモードのオプション引数を解析
    /// </summary>
    /// <param name="options">解析する引数配列</param>
    /// <param name="downloadFolder">ダウンロード先フォルダ（出力パラメータ）</param>
    /// <param name="cleanupCsv">CSVクリーンアップフラグ（出力パラメータ）</param>
    /// <param name="sheetNames">シート名リスト（出力パラメータ）</param>
    /// <param name="serviceAccountKeyPath">サービスアカウントキーファイルパス（出力パラメータ）</param>
    private static void ParseSheetsDownloadOptions(string[] options, out string downloadFolder, out bool cleanupCsv, out List<string>? sheetNames, out string? serviceAccountKeyPath)
    {
        downloadFolder = "downloads"; // デフォルト
        cleanupCsv = false;
        sheetNames = null;
        serviceAccountKeyPath = null;
        
        var sheets = new List<string>();
        
        foreach (string option in options)
        {
            if (option == "--cleanup")
            {
                cleanupCsv = true;
            }
            else if (option.StartsWith("--folder="))
            {
                downloadFolder = option.Substring("--folder=".Length);
                if (string.IsNullOrWhiteSpace(downloadFolder))
                {
                    downloadFolder = "downloads";
                }
            }
            else if (option.StartsWith("--key="))
            {
                serviceAccountKeyPath = option.Substring("--key=".Length);
                if (string.IsNullOrWhiteSpace(serviceAccountKeyPath))
                {
                    serviceAccountKeyPath = null;
                }
            }
            else if (!option.StartsWith("--"))
            {
                // オプションでない場合はシート名として扱う
                sheets.Add(option);
            }
        }
        
        if (sheets.Count > 0)
        {
            sheetNames = sheets;
        }
    }
}