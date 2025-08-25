using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
            Console.WriteLine("使用方法: dotnet run [出力モード] <CSVファイルパス>");
            Console.WriteLine("出力モード: json (デフォルト) または dump");
            Console.WriteLine("例: dotnet run data.csv          (JSON出力)");
            Console.WriteLine("例: dotnet run json data.csv     (JSON出力)");
            Console.WriteLine("例: dotnet run dump data.csv     (ダンプ出力)");
            return 1;
        }

        // 引数を解析
        string outputMode = "json"; // デフォルトはJSON出力
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
            if (firstArg == "json" || firstArg == "dump")
            {
                outputMode = firstArg;
                csvFilePath = args[1];
            }
            else
            {
                Console.WriteLine("エラー: 無効な出力モードです。'json' または 'dump' を指定してください。");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("エラー: 引数が多すぎます。");
            return 1;
        }

        // ファイルの存在確認
        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"エラー: ファイル '{csvFilePath}' が見つかりません。");
            return 1;
        }

        try
        {
            // 出力モードに応じた処理
            if (outputMode == "json")
            {
                // JSON出力時はログ出力を抑制
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = ParseSystemFlags(csvFilePath, suppressOutput: true);
                OutputAsJson(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
            }
            else
            {
                // ダンプ出力時は詳細ログを出力
                var (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames) = ParseSystemFlags(csvFilePath, suppressOutput: false);
                DumpActualData(csvFilePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
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
    /// CSVファイルからシステム処理フラグ（server_needed、client_needed、is_array）を解析してList型変数に格納する
    /// </summary>
    /// <param name="filePath">解析するCSVファイルのパス</param>
    /// <param name="suppressOutput">出力を抑制するかどうか（JSON出力時はtrue）</param>
    /// <returns>システム処理フラグとカラム名のタプル</returns>
    static (List<bool> ServerNeeded, List<bool> ClientNeeded, List<bool> IsArray, List<string> ColumnNames) ParseSystemFlags(string filePath, bool suppressOutput = false)
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
            bool foundColumnNameRow = false;
            
            while ((columns = ParseCsvLine(reader)) != null && !foundColumnNameRow)
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
                    foundColumnNameRow = true;
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
        
        return (serverNeededFlags, clientNeededFlags, isArrayFlags, columnNames);
    }

    /// <summary>
    /// CSV行を適切にパースし、ダブルクォート内の改行やカンマを考慮してフィールドに分割する
    /// </summary>
    /// <param name="reader">StreamReader</param>
    /// <returns>パースされたフィールド配列、またはnull（EOF）</returns>
    static string[]? ParseCsvLine(StreamReader reader)
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
            string[]? columns;
            bool foundColumnNameRow = false;
            
            // column_name行が見つかるまでスキップ
            while ((columns = ParseCsvLine(reader)) != null && !foundColumnNameRow)
            {
                var firstColumn = columns[0].Trim();
                if (firstColumn == "column_name")
                {
                    foundColumnNameRow = true;
                }
            }
            
            // 実データを読み込み・表示（配列データ集計付き）
            int dataRowNumber = 1;
            var allRows = new List<string[]>();
            
            // まず全行を読み込む
            while ((columns = ParseCsvLine(reader)) != null)
            {
                allRows.Add(columns);
            }
            
            // 配列データを集計しながら表示
            for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
            {
                var currentRow = allRows[rowIndex];
                
                // メイン行かどうかを判定（1列目が空白でない、または重要データが含まれる）
                bool isMainRow = IsMainDataRow(currentRow, columnNames, isArrayFlags);
                
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
                            var arrayGroup = GetArrayColumnGroup(i, isArrayFlags);
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
    }

    /// <summary>
    /// メイン行かどうかを判定する（配列データの続きの行でないかを確認）
    /// </summary>
    /// <param name="row">チェックする行</param>
    /// <param name="columnNames">カラム名リスト</param>
    /// <param name="isArrayFlags">配列フラグリスト</param>
    /// <returns>メイン行の場合true</returns>
    static bool IsMainDataRow(string[] row, List<string> columnNames, List<bool> isArrayFlags)
    {
        if (row.Length < 2) return false;
        
        // 1列目が空でない場合はメイン行
        if (!string.IsNullOrWhiteSpace(row[0])) return true;
        
        // 配列行の判定：is_arrayフラグがfalseのカラムにデータがあるかチェック
        int dataStartIndex = 1;
        
        for (int i = 0; i < columnNames.Count && (i + dataStartIndex) < row.Length; i++)
        {
            var isArrayColumn = i < isArrayFlags.Count && isArrayFlags[i];
            
            if (!isArrayColumn) // is_arrayフラグがfalseのカラム
            {
                if (!string.IsNullOrWhiteSpace(row[i + dataStartIndex]))
                {
                    return true; // 非配列カラムにデータがある場合はメイン行
                }
            }
        }
        
        return false; // is_arrayカラムのみにデータがある場合は配列データ行
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
        var arrayGroup = GetArrayColumnGroup(columnIndex, isArrayFlags);
        
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
                if (IsMainDataRow(row, columnNames, isArrayFlags)) break; // 次のメイン行に到達したら終了
                
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

    /// <summary>
    /// 指定されたカラムが属するis_arrayフラグのグループを取得する
    /// </summary>
    /// <param name="columnIndex">対象カラムのインデックス</param>
    /// <param name="isArrayFlags">配列フラグリスト</param>
    /// <returns>連続する配列カラムのインデックスリスト</returns>
    static List<int> GetArrayColumnGroup(int columnIndex, List<bool> isArrayFlags)
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
    
    /// <summary>
    /// データをJSON形式で出力する（ゲーム用データのみ、metadataは除外）
    /// </summary>
    /// <param name="filePath">読み込むCSVファイルのパス</param>
    /// <param name="columnNames">カラム名のリスト</param>
    /// <param name="serverNeededFlags">サーバーAPIに必要なカラムフラグ</param>
    /// <param name="clientNeededFlags">クライアントAPIに必要なカラムフラグ</param>
    /// <param name="isArrayFlags">配列を示すカラムフラグ</param>
    static void OutputAsJson(string filePath, List<string> columnNames, List<bool> serverNeededFlags, List<bool> clientNeededFlags, List<bool> isArrayFlags)
    {
        var resultData = ParseDataRows(filePath, columnNames, serverNeededFlags, clientNeededFlags, isArrayFlags);
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        string jsonOutput = JsonSerializer.Serialize(resultData, jsonOptions);
        Console.WriteLine(jsonOutput);
    }
    
    /// <summary>
    /// CSVファイルから実データを解析してJSON用の構造化データを作成する
    /// </summary>
    /// <param name="filePath">読み込むCSVファイルのパス</param>
    /// <param name="columnNames">カラム名のリスト</param>
    /// <param name="serverNeededFlags">サーバーAPIに必要なカラムフラグ</param>
    /// <param name="clientNeededFlags">クライアントAPIに必要なカラムフラグ</param>
    /// <param name="isArrayFlags">配列を示すカラムフラグ</param>
    /// <returns>JSON用の構造化データ</returns>
    static List<Dictionary<string, object>> ParseDataRows(string filePath, List<string> columnNames, List<bool> serverNeededFlags, List<bool> clientNeededFlags, List<bool> isArrayFlags)
    {
        var dataRows = new List<Dictionary<string, object>>();
        
        // envsカラムのインデックスを取得
        int envsColumnIndex = columnNames.FindIndex(name => name.Equals("envs", StringComparison.OrdinalIgnoreCase));
        
        using var reader = new StreamReader(filePath);
        string[]? columns;
        bool foundColumnNameRow = false;
        
        // column_name行が見つかるまでスキップ
        while ((columns = ParseCsvLine(reader)) != null && !foundColumnNameRow)
        {
            var firstColumn = columns[0].Trim();
            if (firstColumn == "column_name")
            {
                foundColumnNameRow = true;
            }
        }
        
        // 実データを読み込み
        var allRows = new List<string[]>();
        while ((columns = ParseCsvLine(reader)) != null)
        {
            allRows.Add(columns);
        }
        
        // 配列データを集計しながらJSON形式に変換
        for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
        {
            var currentRow = allRows[rowIndex];
            
            // メイン行かどうかを判定
            bool isMainRow = IsMainDataRow(currentRow, columnNames, isArrayFlags);
            
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
                    
                    // 特殊列の判定
                    bool isSpecialColumn = columnName.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                                         columnName.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                                         columnName.Equals("ver", StringComparison.OrdinalIgnoreCase) ||
                                         columnName.Equals("envs", StringComparison.OrdinalIgnoreCase);
                    
                    // 出力対象の判定
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
                        var arrayGroup = GetArrayColumnGroup(i, isArrayFlags);
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
            if (IsMainDataRow(row, columnNames, isArrayFlags)) break; // 次のメイン行に到達したら終了
            
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