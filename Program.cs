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
            // システム処理フラグを解析
            var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = ParseSystemFlags(csvFilePath);
            
            // 実データをダンプ
            DumpActualData(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: ファイルの読み込み中に問題が発生しました - {ex.Message}");
            return 1;
        }
    }


    /// <summary>
    /// CSVファイルからシステム処理フラグ（server_needed、client_needed、is_array）を解析してList型変数に格納する
    /// </summary>
    /// <param name="filePath">解析するCSVファイルのパス</param>
    /// <returns>システム処理フラグとカラム名のタプル</returns>
    static (List<bool> ServerNeeded, List<bool> ClientNeeded, List<bool> IsArray, List<string> ColumnNames) ParseSystemFlags(string filePath)
    {
        var serverNeededFlags = new List<bool>();
        var clientNeededFlags = new List<bool>();
        var isArrayFlags = new List<bool>();
        var columnNames = new List<string>();

        Console.WriteLine();
        Console.WriteLine("=== システム処理フラグの解析 ===");

        using (var reader = new StreamReader(filePath))
        {
            string? line;
            bool foundColumnNameRow = false;
            
            while ((line = reader.ReadLine()) != null && !foundColumnNameRow)
            {
                var columns = line.Split(',');
                var firstColumn = columns[0].Trim();
                
                if (firstColumn == "server_needed")
                {
                    for (int i = 1; i < columns.Length; i++)
                    {
                        serverNeededFlags.Add(columns[i].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (firstColumn == "client_needed")
                {
                    for (int i = 1; i < columns.Length; i++)
                    {
                        clientNeededFlags.Add(columns[i].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (firstColumn == "is_array")
                {
                    for (int i = 1; i < columns.Length; i++)
                    {
                        isArrayFlags.Add(columns[i].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (firstColumn == "column_name")
                {
                    for (int i = 1; i < columns.Length; i++)
                    {
                        columnNames.Add(columns[i].Trim());
                    }
                    foundColumnNameRow = true;
                }
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
        
        return (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames);
    }

    /// <summary>
    /// column_name行以降の実データをダンプする（システム処理フラグ情報付き）
    /// </summary>
    /// <param name="filePath">読み込むCSVファイルのパス</param>
    /// <param name="columnNames">カラム名のリスト</param>
    /// <param name="serverNeededFlags">サーバーAPIに必要なカラムフラグ</param>
    /// <param name="clientNeededFlags">クライアントAPIに必要なカラムフラグ</param>
    /// <param name="isArrayFlags">配列を示すカラムフラグ</param>
    static void DumpActualData(string filePath, List<string> columnNames, List<bool> serverNeededFlags, List<bool> clientNeededFlags, List<bool> isArrayFlags)
    {
        Console.WriteLine();
        Console.WriteLine("=== 実データのダンプ ===");

        using (var reader = new StreamReader(filePath))
        {
            string? line;
            bool foundColumnNameRow = false;
            
            // column_name行が見つかるまでスキップ
            while ((line = reader.ReadLine()) != null && !foundColumnNameRow)
            {
                var firstColumn = line.Split(',')[0].Trim();
                if (firstColumn == "column_name")
                {
                    foundColumnNameRow = true;
                }
            }
            
            // 実データを読み込み・表示
            int dataRowNumber = 1;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var columns = line.Split(',');
                
                Console.WriteLine($"データ行 {dataRowNumber}:");
                for (int i = 0; i < Math.Min(columns.Length, columnNames.Count); i++)
                {
                    var serverFlag = i < serverNeededFlags.Count ? serverNeededFlags[i] : false;
                    var clientFlag = i < clientNeededFlags.Count ? clientNeededFlags[i] : false;
                    var arrayFlag = i < isArrayFlags.Count ? isArrayFlags[i] : false;
                    
                    var flagIndicator = "";
                    if (serverFlag) flagIndicator += "[S]";
                    if (clientFlag) flagIndicator += "[C]";
                    if (arrayFlag) flagIndicator += "[A]";
                    
                    Console.WriteLine($"  {columnNames[i]}{flagIndicator}: {columns[i]}");
                }
                Console.WriteLine();
                dataRowNumber++;
            }
            
            Console.WriteLine($"=== 実データ合計 {dataRowNumber - 1} 行 ===");
        }
    }
}