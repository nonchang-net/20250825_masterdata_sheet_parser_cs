using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MasterDataSheetParser;

/// <summary>
/// ダンプ形式での出力処理を担当するクラス
/// </summary>
public class DumpOutputter
{
    /// <summary>
    /// column_name行以降の実データをダンプする（システム処理フラグ情報付き）
    /// </summary>
    /// <param name="filePath">読み込むCSVファイルのパス</param>
    /// <param name="columnNames">カラム名のリスト</param>
    /// <param name="serverNeededFlags">サーバーAPIに必要なカラムフラグ</param>
    /// <param name="clientNeededFlags">クライアントAPIに必要なカラムフラグ</param>
    /// <param name="isArrayFlags">配列を示すカラムフラグ</param>
    public static void DumpActualData(string filePath, List<string> columnNames, List<bool> serverNeededFlags, List<bool> clientNeededFlags, List<bool> isArrayFlags)
    {
        Console.WriteLine();
        Console.WriteLine("=== 実データのダンプ ===");

        using var reader = new StreamReader(filePath);
        string[]? columns;
        
        // column_name行が見つかるまでスキップ
        while ((columns = CSVParser.ParseCsvLine(reader)) != null)
        {
            var firstColumn = columns[0].Trim();
            if (firstColumn == "column_name")
            {
                break;
            }
        }
        
        // 実データを読み込み・表示（配列データ集計付き）
        int dataRowNumber = 1;
        var allRows = new List<string[]>();
        
        // まず全行を読み込む
        while ((columns = CSVParser.ParseCsvLine(reader)) != null)
        {
            allRows.Add(columns);
        }
        
        // 配列データを集計しながら表示
        for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
        {
            var currentRow = allRows[rowIndex];
            
            // メイン行かどうかを判定（1列目が空白でない、または重要データが含まれる）
            bool isMainRow = CSVParser.IsMainDataRow(currentRow, columnNames, isArrayFlags);
            
            if (isMainRow)
            {
                Console.WriteLine($"データ行 {dataRowNumber}:");
                
                // 実データは1列目が空白なので、カラム1から開始してカラム名[0]から対応させる
                int dataStartIndex = 1;
                var processedData = new Dictionary<string, object>();
                
                for (int i = 0; i < columnNames.Count && (i + dataStartIndex) < currentRow.Length; i++)
                {
                    var columnName = columnNames[i];
                    var serverFlag = i < serverNeededFlags.Count && serverNeededFlags[i];
                    var clientFlag = i < clientNeededFlags.Count && clientNeededFlags[i];
                    var arrayFlag = i < isArrayFlags.Count && isArrayFlags[i];
                    
                    var flagIndicator = "";
                    if (serverFlag) flagIndicator += "[S]";
                    if (clientFlag) flagIndicator += "[C]";
                    if (arrayFlag) flagIndicator += "[A]";
                    
                    if (arrayFlag)
                    {
                        // 配列グループの最初のカラムかどうかを確認
                        var arrayGroup = CSVParser.GetArrayColumnGroup(i, isArrayFlags);
                        if (arrayGroup.Count > 0 && i == arrayGroup.Min())
                        {
                            // 配列データの集計（グループの最初のカラムのみ出力）
                            var arrayItems = CollectArrayData(allRows, rowIndex, i, columnNames, isArrayFlags, dataStartIndex);
                            var arrayDisplay = string.Join(":", arrayItems);
                            Console.WriteLine($"  {columnName}{flagIndicator}: {arrayDisplay}");
                            processedData[columnName] = arrayItems;
                        }
                        // グループの最初でない場合は出力しない
                    }
                    else
                    {
                        var value = currentRow[i + dataStartIndex];
                        Console.WriteLine($"  {columnName}{flagIndicator}: {value}");
                        processedData[columnName] = value;
                    }
                }
                Console.WriteLine();
                dataRowNumber++;
            }
        }
        
        Console.WriteLine($"=== 実データ合計 {dataRowNumber - 1} 行 ===");
    }

    /// <summary>
    /// 指定したカラムの配列データを収集する
    /// </summary>
    /// <param name="allRows">全データ行</param>
    /// <param name="startRowIndex">開始行インデックス</param>
    /// <param name="columnIndex">対象カラムのインデックス</param>
    /// <param name="columnNames">カラム名リスト</param>
    /// <param name="isArrayFlags">配列フラグリスト</param>
    /// <param name="dataStartIndex">データ開始インデックス</param>
    /// <returns>配列データのリスト</returns>
    static List<string> CollectArrayData(List<string[]> allRows, int startRowIndex, int columnIndex, List<string> columnNames, List<bool> isArrayFlags, int dataStartIndex)
    {
        var arrayItems = new List<string>();
        var columnName = columnNames[columnIndex];
        
        // is_arrayフラグがついている連続するカラムグループを特定
        var arrayGroup = CSVParser.GetArrayColumnGroup(columnIndex, isArrayFlags);
        
        if (arrayGroup.Count > 0)
        {
            // メイン行のデータを取得
            var mainRow = allRows[startRowIndex];
            var mainRowValues = new List<string>();
            
            foreach (var colIndex in arrayGroup)
            {
                if ((colIndex + dataStartIndex) < mainRow.Length)
                {
                    mainRowValues.Add(mainRow[colIndex + dataStartIndex]);
                }
            }
            
            if (mainRowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                arrayItems.Add(string.Join(",", mainRowValues));
            }
            
            // 続く行の配列データを収集
            for (int i = startRowIndex + 1; i < allRows.Count; i++)
            {
                var row = allRows[i];
                if (CSVParser.IsMainDataRow(row, columnNames, isArrayFlags)) break; // 次のメイン行に到達したら終了
                
                var rowValues = new List<string>();
                foreach (var colIndex in arrayGroup)
                {
                    if ((colIndex + dataStartIndex) < row.Length)
                    {
                        rowValues.Add(row[colIndex + dataStartIndex]);
                    }
                }
                
                if (rowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    arrayItems.Add(string.Join(",", rowValues));
                }
            }
            
            // グループの最初のカラムの場合のみ結果を返す（重複を避けるため）
            if (columnIndex == arrayGroup.Min())
            {
                return arrayItems;
            }
        }
        
        return new List<string>(); // グループの最初以外またはグループ外のカラムでは空を返す
    }
}