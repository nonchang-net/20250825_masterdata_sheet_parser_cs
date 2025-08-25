using System;
using System.IO;

namespace MasterDataSheetParser;

/// <summary>
/// CSVファイルを読み込んで標準出力にダンプするCLIツール
/// </summary>
class Program
{
    /// <summary>
    /// アプリケーションのエントリーポイント
    /// </summary>
    /// <param name="args">コマンドライン引数。CSVファイルのパスを期待</param>
    /// <returns>正常終了時は0、エラー時は1</returns>
    static int Main(string[] args)
    {
        // コマンドライン引数の検証
        if (args.Length == 0)
        {
            Console.WriteLine("使用方法: dotnet run <CSVファイルパス>");
            Console.WriteLine("例: dotnet run data.csv");
            return 1;
        }

        string csvFilePath = args[0];

        // ファイルの存在確認
        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"エラー: ファイル '{csvFilePath}' が見つかりません。");
            return 1;
        }

        try
        {
            // CSVファイルの内容を標準出力にダンプ
            DumpCsvFile(csvFilePath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: ファイルの読み込み中に問題が発生しました - {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// CSVファイルを読み込んで全行を標準出力にダンプする
    /// </summary>
    /// <param name="filePath">読み込むCSVファイルのパス</param>
    static void DumpCsvFile(string filePath)
    {
        Console.WriteLine($"=== CSVファイル '{filePath}' の内容 ===");
        Console.WriteLine();

        int lineNumber = 1;
        using (var reader = new StreamReader(filePath))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine($"{lineNumber:D4}: {line}");
                lineNumber++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"=== 合計 {lineNumber - 1} 行 ===");
    }
}