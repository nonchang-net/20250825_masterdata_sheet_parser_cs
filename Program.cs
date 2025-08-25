using System;
using System.IO;
using System.Collections.Generic;

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
            
            // システム処理フラグを解析
            ParseSystemFlags(csvFilePath);
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

    /// <summary>
    /// CSVファイルからシステム処理フラグ（server_needed、client_needed、is_array）を解析してList型変数に格納する
    /// </summary>
    /// <param name="filePath">解析するCSVファイルのパス</param>
    static void ParseSystemFlags(string filePath)
    {
        var serverNeededFlags = new List<bool>();
        var clientNeededFlags = new List<bool>();
        var isArrayFlags = new List<bool>();
        var columnNames = new List<string>();

        Console.WriteLine();
        Console.WriteLine("=== システム処理フラグの解析 ===");

        using (var reader = new StreamReader(filePath))
        {
            int lineNumber = 1;
            string? line;
            
            while ((line = reader.ReadLine()) != null && lineNumber <= 4)
            {
                var columns = line.Split(',');
                
                switch (lineNumber)
                {
                    case 1: // server_needed行
                        if (columns[0] == "server_needed")
                        {
                            for (int i = 1; i < columns.Length; i++)
                            {
                                serverNeededFlags.Add(columns[i].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        break;
                        
                    case 2: // client_needed行
                        if (columns[0] == "client_needed")
                        {
                            for (int i = 1; i < columns.Length; i++)
                            {
                                clientNeededFlags.Add(columns[i].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        break;
                        
                    case 3: // is_array行
                        if (columns[0] == "is_array")
                        {
                            for (int i = 1; i < columns.Length; i++)
                            {
                                isArrayFlags.Add(columns[i].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        break;
                        
                    case 4: // column_name行
                        if (columns[0] == "column_name")
                        {
                            for (int i = 1; i < columns.Length; i++)
                            {
                                columnNames.Add(columns[i].Trim());
                            }
                        }
                        break;
                }
                
                lineNumber++;
            }
        }

        // 解析結果を出力
        Console.WriteLine($"カラム数: {columnNames.Count}");
        Console.WriteLine();
        
        for (int i = 0; i < columnNames.Count; i++)
        {
            var serverFlag = i < serverNeededFlags.Count ? serverNeededFlags[i] : false;
            var clientFlag = i < clientNeededFlags.Count ? clientNeededFlags[i] : false;
            var arrayFlag = i < isArrayFlags.Count ? isArrayFlags[i] : false;
            
            Console.WriteLine($"カラム[{i}]: {columnNames[i]}");
            Console.WriteLine($"  server_needed: {serverFlag}");
            Console.WriteLine($"  client_needed: {clientFlag}");
            Console.WriteLine($"  is_array: {arrayFlag}");
            Console.WriteLine();
        }
    }
}