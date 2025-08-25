using System;
using System.IO;

namespace MasterDataSheetParser;

/// <summary>
/// CSVファイルを読み込んでJSONまたはダンプ形式で出力するCLIツール
/// </summary>
class Program
{
    /// <summary>
    /// アプリケーションのエントリーポイント
    /// </summary>
    /// <param name="args">コマンドライン引数。[出力モード] CSVファイルパスを期待</param>
    /// <returns>正常終了時は0、エラー時は1</returns>
    static int Main(string[] args)
    {
        // コマンドライン引数の検証
        if (args.Length == 0)
        {
            Console.WriteLine("使用方法: dotnet run [出力モード] <CSVファイルパス/フォルダパス>");
            Console.WriteLine("出力モード: json2 (デフォルト), json, dump, または batchConvert");
            Console.WriteLine("例: dotnet run data.csv            (JSON2出力)");
            Console.WriteLine("例: dotnet run json data.csv       (JSON出力)");
            Console.WriteLine("例: dotnet run json2 data.csv      (JSON2連想配列出力)");
            Console.WriteLine("例: dotnet run dump data.csv       (ダンプ出力)");
            Console.WriteLine("例: dotnet run batchConvert ./csv/ (フォルダ内CSVを一括JSON2変換)");
            return 1;
        }

        // 引数を解析
        string outputMode = "json2"; // デフォルトはJSON2出力
        string csvFilePath;
        
        if (args.Length == 1)
        {
            // ファイルパスのみの場合
            csvFilePath = args[0];
        }
        else if (args.Length == 2)
        {
            // 出力モードとファイルパスが指定された場合
            string firstArg = args[0].ToLower();
            if (firstArg == "json" || firstArg == "json2" || firstArg == "dump" || firstArg == "batchconvert")
            {
                outputMode = firstArg;
                csvFilePath = args[1];
            }
            else
            {
                Console.WriteLine("エラー: 無効な出力モードです。'json', 'json2', 'dump', または 'batchConvert' を指定してください。");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("エラー: 引数が多すぎます。");
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
                JsonOutputter.OutputAsJson(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
            }
            else if (outputMode == "json2")
            {
                // JSON2出力時はログ出力を抑制
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = CSVParser.ParseSystemFlags(csvFilePath, suppressOutput: true);
                JsonOutputter.OutputAsJson2(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
            }
            else if (outputMode == "batchconvert")
            {
                // バッチ変換処理
                BatchProcessor.BatchConvertCsvToJson2(csvFilePath);
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
}