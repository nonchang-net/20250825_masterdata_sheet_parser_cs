using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MasterDataSheetParser;

/// <summary>
/// バッチ処理を担当するクラス
/// </summary>
public class BatchProcessor
{
    /// <summary>
    /// 指定フォルダ内のCSVファイルをすべてJSON2形式に変換し、元のCSVファイルを削除する
    /// </summary>
    /// <param name="folderPath">変換対象のフォルダパス</param>
    public static void BatchConvertCsvToJson2(string folderPath)
    {
        try
        {
            // フォルダ内のCSVファイルを取得
            string[] csvFiles = Directory.GetFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly);
            
            if (csvFiles.Length == 0)
            {
                Console.WriteLine($"フォルダ '{folderPath}' 内にCSVファイルが見つかりません。");
                return;
            }
            
            Console.WriteLine($"フォルダ '{folderPath}' 内の {csvFiles.Length} 個のCSVファイルを変換します...");
            
            int successCount = 0;
            int errorCount = 0;
            
            foreach (string csvFilePath in csvFiles)
            {
                try
                {
                    // ファイル名から拡張子を除いてJSONファイル名を作成
                    string fileName = Path.GetFileNameWithoutExtension(csvFilePath);
                    string jsonFilePath = Path.Combine(folderPath, fileName + ".json");
                    
                    Console.WriteLine($"変換中: {Path.GetFileName(csvFilePath)} -> {Path.GetFileName(jsonFilePath)}");
                    
                    // システム処理フラグを解析
                    var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = CSVParser.ParseSystemFlags(csvFilePath, suppressOutput: true);
                    
                    // JSON2形式でデータを取得
                    var dataRows = ParseDataRowsForJson2(csvFilePath, columnNames, clientNeededFlags, isArrayFlags);
                    
                    // ID列のインデックスを取得
                    int idColumnIndex = columnNames.FindIndex(name => name.Equals("id", StringComparison.OrdinalIgnoreCase));
                    
                    var resultData = new Dictionary<string, Dictionary<string, object>>();
                    
                    foreach (var row in dataRows)
                    {
                        // ID列の値をキーとして使用
                        string idKey = "";
                        if (idColumnIndex >= 0 && row.TryGetValue("id", out var idValue))
                        {
                            idKey = idValue?.ToString() ?? "";
                            // オブジェクト内容からidを除外
                            row.Remove("id");
                        }
                        
                        if (!string.IsNullOrEmpty(idKey))
                        {
                            resultData[idKey] = row;
                        }
                    }
                    
                    // JSONファイルとして保存
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    
                    string jsonContent = JsonSerializer.Serialize(resultData, jsonOptions);
                    File.WriteAllText(jsonFilePath, jsonContent, System.Text.Encoding.UTF8);
                    
                    // 元のCSVファイルを削除
                    File.Delete(csvFilePath);
                    
                    successCount++;
                    Console.WriteLine($"完了: {Path.GetFileName(jsonFilePath)}");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"エラー: {Path.GetFileName(csvFilePath)} の変換に失敗しました - {ex.Message}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine($"バッチ変換完了: {successCount} 件成功, {errorCount} 件失敗");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"バッチ変換エラー: {ex.Message}");
        }
    }
    
    /// <summary>
    /// CSVファイルから実データを解析してJSON2用の構造化データを作成する（idを含む）
    /// </summary>
    /// <param name="filePath">読み込むCSVファイルのパス</param>
    /// <param name="columnNames">カラム名のリスト</param>
    /// <param name="clientNeededFlags">クライアントAPIに必要なカラムフラグ</param>
    /// <param name="isArrayFlags">配列を示すカラムフラグ</param>
    /// <returns>JSON用の構造化データ</returns>
    static List<Dictionary<string, object>> ParseDataRowsForJson2(string filePath, List<string> columnNames, List<bool> clientNeededFlags, List<bool> isArrayFlags)
    {
        var dataRows = new List<Dictionary<string, object>>();
        
        // envsカラムのインデックスを取得
        int envsColumnIndex = columnNames.FindIndex(name => name.Equals("envs", StringComparison.OrdinalIgnoreCase));
        
        using var reader = new StreamReader(filePath);
        string[]? columns;
        
        // column_name行が見つかるまでスキップ
        while ((columns = CSVParser.ParseCsvLine(reader)) != null)
        {
            var firstColumn = columns[0].Trim();
            if (firstColumn == "column_name")
            {
                break; // column_name行を見つけたらループを抜ける
            }
        }
        
        // 実データを読み込み（column_name行の次の行から）
        var allRows = new List<string[]>();
        while ((columns = CSVParser.ParseCsvLine(reader)) != null)
        {
            allRows.Add(columns);
        }
        
        // 配列データを集計しながらJSON形式に変換
        for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
        {
            var currentRow = allRows[rowIndex];
            
            // メイン行かどうかを判定
            bool isMainRow = CSVParser.IsMainDataRow(currentRow, columnNames, isArrayFlags);
            
            if (isMainRow)
            {
                // envsカラムに「DISABLED」が設定されている場合は除外
                if (envsColumnIndex >= 0)
                {
                    int dataStartIndex = 1;
                    int envsDataIndex = envsColumnIndex + dataStartIndex;
                    if (envsDataIndex < currentRow.Length)
                    {
                        string envsValue = currentRow[envsDataIndex].Trim();
                        if (envsValue.Equals("DISABLED", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // この行は除外してスキップ
                        }
                    }
                }
                
                var rowData = new Dictionary<string, object>();
                int dataStartIndex2 = 1;
                var processedArrayColumns = new HashSet<int>();
                
                for (int i = 0; i < columnNames.Count && (i + dataStartIndex2) < currentRow.Length; i++)
                {
                    var columnName = columnNames[i];
                    var arrayFlag = i < isArrayFlags.Count && isArrayFlags[i];
                    var clientNeeded = i < clientNeededFlags.Count && clientNeededFlags[i];
                    
                    // JSON2用の出力対象の判定（idは含める、verとenvsは除外）
                    bool shouldInclude = false;
                    if (columnName.Equals("id", StringComparison.OrdinalIgnoreCase) || 
                        columnName.Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldInclude = true; // id、nameは常に含める
                    }
                    else if (columnName.Equals("ver", StringComparison.OrdinalIgnoreCase) || 
                            columnName.Equals("envs", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldInclude = false; // ver、envsは除外
                    }
                    else
                    {
                        shouldInclude = clientNeeded; // その他はclient_needed=TRUEのみ
                    }
                    
                    if (!shouldInclude) continue;
                    
                    if (arrayFlag)
                    {
                        // 配列グループの最初のカラムかどうかを確認
                        var arrayGroup = CSVParser.GetArrayColumnGroup(i, isArrayFlags);
                        if (arrayGroup.Count > 0 && i == arrayGroup.Min() && !processedArrayColumns.Contains(i))
                        {
                            // 配列グループ内でclient_needed=TRUEのカラムのみを取得
                            var filteredArrayGroup = arrayGroup.Where(colIndex => 
                            {
                                var colName = columnNames[colIndex];
                                if (colName.Equals("id", StringComparison.OrdinalIgnoreCase) || 
                                    colName.Equals("name", StringComparison.OrdinalIgnoreCase))
                                    return true;
                                if (colName.Equals("ver", StringComparison.OrdinalIgnoreCase) || 
                                    colName.Equals("envs", StringComparison.OrdinalIgnoreCase))
                                    return false;
                                return colIndex < clientNeededFlags.Count && clientNeededFlags[colIndex];
                            }).ToList();
                            
                            if (filteredArrayGroup.Count > 0)
                            {
                                // 配列データをオブジェクト配列として集計
                                var arrayObjects = CollectArrayDataAsObjects(allRows, rowIndex, filteredArrayGroup, columnNames, isArrayFlags, dataStartIndex2);
                                rowData[columnName] = arrayObjects;
                            }
                            
                            // グループ内の他のカラムも処理済みとしてマーク
                            foreach (var colIndex in arrayGroup)
                            {
                                processedArrayColumns.Add(colIndex);
                            }
                        }
                    }
                    else if (!processedArrayColumns.Contains(i))
                    {
                        var value = currentRow[i + dataStartIndex2];
                        rowData[columnName] = value;
                    }
                }
                
                dataRows.Add(rowData);
            }
        }
        
        return dataRows;
    }
    
    /// <summary>
    /// 配列グループのデータをオブジェクト配列として収集する
    /// </summary>
    /// <param name="allRows">全データ行</param>
    /// <param name="startRowIndex">開始行インデックス</param>
    /// <param name="arrayGroup">配列グループのカラムインデックスリスト</param>
    /// <param name="columnNames">カラム名リスト</param>
    /// <param name="isArrayFlags">配列フラグリスト</param>
    /// <param name="dataStartIndex">データ開始インデックス</param>
    /// <returns>オブジェクト配列のリスト</returns>
    static List<Dictionary<string, object?>> CollectArrayDataAsObjects(List<string[]> allRows, int startRowIndex, List<int> arrayGroup, List<string> columnNames, List<bool> isArrayFlags, int dataStartIndex)
    {
        var arrayObjects = new List<Dictionary<string, object?>>();
        
        if (arrayGroup.Count == 0) return arrayObjects;
        
        // メイン行のデータを取得
        var mainRow = allRows[startRowIndex];
        var hasMainRowData = false;
        
        // メイン行に配列データがあるかチェック
        foreach (var colIndex in arrayGroup)
        {
            if ((colIndex + dataStartIndex) < mainRow.Length && !string.IsNullOrWhiteSpace(mainRow[colIndex + dataStartIndex]))
            {
                hasMainRowData = true;
                break;
            }
        }
        
        if (hasMainRowData)
        {
            var mainRowObject = new Dictionary<string, object?>();
            foreach (var colIndex in arrayGroup)
            {
                if ((colIndex + dataStartIndex) < mainRow.Length)
                {
                    var value = mainRow[colIndex + dataStartIndex];
                    var columnName = columnNames[colIndex];
                    mainRowObject[columnName] = string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }
            arrayObjects.Add(mainRowObject);
        }
        
        // 続く行の配列データを収集
        for (int i = startRowIndex + 1; i < allRows.Count; i++)
        {
            var row = allRows[i];
            if (CSVParser.IsMainDataRow(row, columnNames, isArrayFlags)) break; // 次のメイン行に到達したら終了
            
            var hasRowData = false;
            foreach (var colIndex in arrayGroup)
            {
                if ((colIndex + dataStartIndex) < row.Length && !string.IsNullOrWhiteSpace(row[colIndex + dataStartIndex]))
                {
                    hasRowData = true;
                    break;
                }
            }
            
            if (hasRowData)
            {
                var rowObject = new Dictionary<string, object?>();
                foreach (var colIndex in arrayGroup)
                {
                    if ((colIndex + dataStartIndex) < row.Length)
                    {
                        var value = row[colIndex + dataStartIndex];
                        var columnName = columnNames[colIndex];
                        rowObject[columnName] = string.IsNullOrWhiteSpace(value) ? null : value;
                    }
                }
                arrayObjects.Add(rowObject);
            }
        }
        
        return arrayObjects;
    }
}