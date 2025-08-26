using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MasterDataSheetParser;

/// <summary>
/// CSVファイルの解析とシステム処理フラグの処理を担当するクラス
/// </summary>
public class CSVParser
{
    /// <summary>
    /// CSVファイルからシステム処理フラグ（server_needed、client_needed、is_array）を解析してList型変数に格納する
    /// </summary>
    /// <param name="filePath">解析するCSVファイルのパス</param>
    /// <param name="suppressOutput">出力を抑制するかどうか（JSON出力時はtrue）</param>
    /// <returns>システム処理フラグとカラム名のタプル</returns>
    public static (List<bool> ServerNeeded, List<bool> ClientNeeded, List<bool> IsArray, List<string> ColumnNames) ParseSystemFlags(string filePath, bool suppressOutput = false)
    {
        var serverNeededFlags = new List<bool>(10);
        var clientNeededFlags = new List<bool>(10);
        var isArrayFlags = new List<bool>(10);
        var columnNames = new List<string>(10);

        if (!suppressOutput)
        {
            Console.WriteLine();
            Console.WriteLine("=== システム処理フラグの解析 ===");
        }

        using (var reader = new StreamReader(filePath))
        {
            string[]? columns;
            
            while ((columns = ParseCsvLine(reader)) != null)
            {
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
                    break; // column_name行を処理したらループを抜ける
                }
            }
        }

        // 解析結果を出力（JSON出力時は抑制）
        if (!suppressOutput)
        {
            Console.WriteLine($"カラム数: {columnNames.Count}");
            Console.WriteLine();
            
            for (int i = 0; i < columnNames.Count; i++)
            {
                var serverFlag = i < serverNeededFlags.Count && serverNeededFlags[i];
                var clientFlag = i < clientNeededFlags.Count && clientNeededFlags[i];
                var arrayFlag = i < isArrayFlags.Count && isArrayFlags[i];
                
                Console.WriteLine($"カラム[{i}]: {columnNames[i]}");
                Console.WriteLine($"  server_needed: {serverFlag}");
                Console.WriteLine($"  client_needed: {clientFlag}");
                Console.WriteLine($"  is_array: {arrayFlag}");
                Console.WriteLine();
            }
        }
        
        return (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames);
    }

    /// <summary>
    /// CSV行を適切にパースし、ダブルクォート内の改行やカンマを考慮してフィールドに分割する
    /// </summary>
    /// <param name="reader">StreamReader</param>
    /// <returns>パースされたフィールド配列、またはnull（EOF）</returns>
    public static string[]? ParseCsvLine(StreamReader reader)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;
        bool fieldStarted = false;

        while (true)
        {
            int ch = reader.Read();
            if (ch == -1) // EOF
            {
                if (fieldStarted || fields.Count > 0)
                {
                    fields.Add(currentField.ToString());
                    return fields.ToArray();
                }
                return null;
            }

            char c = (char)ch;

            if (!fieldStarted)
            {
                if (c == '"')
                {
                    inQuotes = true;
                    fieldStarted = true;
                }
                else if (c == ',')
                {
                    fields.Add("");
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r')
                    {
                        // CRLFの場合、LFもスキップ
                        if (reader.Peek() == '\n')
                            reader.Read();
                    }
                    if (fields.Count > 0 || currentField.Length > 0)
                    {
                        fields.Add(currentField.ToString());
                        return fields.ToArray();
                    }
                }
                else
                {
                    currentField.Append(c);
                    fieldStarted = true;
                }
            }
            else if (inQuotes)
            {
                if (c == '"')
                {
                    // 次の文字を確認
                    int nextCh = reader.Peek();
                    if (nextCh == '"')
                    {
                        // エスケープされたクォート
                        reader.Read();
                        currentField.Append('"');
                    }
                    else
                    {
                        // クォート終了
                        inQuotes = false;
                    }
                }
                else if (c == '\r' || c == '\n')
                {
                    // クォート内の改行はエスケープシーケンスに変換
                    if (c == '\r' && reader.Peek() == '\n')
                    {
                        reader.Read(); // LFも読み込み
                        currentField.Append("\\r\\n");
                    }
                    else if (c == '\n')
                    {
                        currentField.Append("\\n");
                    }
                    else
                    {
                        currentField.Append("\\r");
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                    fieldStarted = false;
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r')
                    {
                        // CRLFの場合、LFもスキップ
                        if (reader.Peek() == '\n')
                            reader.Read();
                    }
                    fields.Add(currentField.ToString());
                    return fields.ToArray();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }
    }

    /// <summary>
    /// メイン行かどうかを判定する（配列データの続きの行でないかを確認）
    /// </summary>
    /// <param name="row">チェックする行</param>
    /// <param name="columnNames">カラム名リスト</param>
    /// <param name="isArrayFlags">配列フラグリスト</param>
    /// <returns>メイン行の場合true</returns>
    public static bool IsMainDataRow(string[] row, List<string> columnNames, List<bool> isArrayFlags)
    {
        if (row.Length < 2) return false;
        
        // 1列目が空でない場合はメイン行
        if (!string.IsNullOrWhiteSpace(row[0])) return true;
        
        // 配列行の判定：is_arrayフラグがfalseのカラムで重要なカラム（id, name等）にデータがあるかチェック
        int dataStartIndex = 1;
        
        for (int i = 0; i < columnNames.Count && (i + dataStartIndex) < row.Length; i++)
        {
            var columnName = columnNames[i];
            var isArrayColumn = i < isArrayFlags.Count && isArrayFlags[i];
            var cellValue = row[i + dataStartIndex].Trim();
            
            // is_arrayフラグがfalseで、かつ重要なカラム（id, name）にデータがある場合のみメイン行と判定
            if (!isArrayColumn && !string.IsNullOrWhiteSpace(cellValue))
            {
                // id, name カラムにデータがある場合のみメイン行と判定
                if (columnName.Equals("id", StringComparison.OrdinalIgnoreCase) || 
                    columnName.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        
        return false; // is_arrayカラムのみにデータがある、またはid/name以外のカラムのみにデータがある場合は配列データ行
    }

    /// <summary>
    /// 指定されたカラムが属するis_arrayフラグのグループを取得する
    /// </summary>
    /// <param name="columnIndex">対象カラムのインデックス</param>
    /// <param name="isArrayFlags">配列フラグリスト</param>
    /// <returns>連続する配列カラムのインデックスリスト</returns>
    public static List<int> GetArrayColumnGroup(int columnIndex, List<bool> isArrayFlags)
    {
        var group = new List<int>();
        
        if (columnIndex >= isArrayFlags.Count || !isArrayFlags[columnIndex])
        {
            return group; // 対象カラムが配列でない場合は空のグループを返す
        }
        
        // 対象カラムから左方向に連続する配列カラムを探す
        int start = columnIndex;
        while (start > 0 && start - 1 < isArrayFlags.Count && isArrayFlags[start - 1])
        {
            start--;
        }
        
        // 対象カラムから右方向に連続する配列カラムを探す
        int end = columnIndex;
        while (end + 1 < isArrayFlags.Count && isArrayFlags[end + 1])
        {
            end++;
        }
        
        // グループに追加
        for (int i = start; i <= end; i++)
        {
            if (i < isArrayFlags.Count && isArrayFlags[i])
            {
                group.Add(i);
            }
        }
        
        return group;
    }
}