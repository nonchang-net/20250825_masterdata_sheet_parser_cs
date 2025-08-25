using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MasterDataSheetParser.Tests;

/// <summary>
/// CSVParserクラスのユニットテスト
/// </summary>
public class CSVParserTests
{
    private readonly string _testDataPath = Path.Combine("TestData", "sample.csv");

    /// <summary>
    /// ParseSystemFlagsメソッドが正しくシステムフラグを解析することをテスト
    /// </summary>
    [Fact]
    public void ParseSystemFlags_ValidCsvFile_ReturnsCorrectFlags()
    {
        // Arrange
        var filePath = _testDataPath;

        // Act
        var (serverNeeded, clientNeeded, isArray, columnNames) = CSVParser.ParseSystemFlags(filePath, suppressOutput: true);

        // Assert
        Assert.Equal(4, columnNames.Count);
        Assert.Equal("id", columnNames[0]);
        Assert.Equal("name", columnNames[1]);
        Assert.Equal("description", columnNames[2]);
        Assert.Equal("items", columnNames[3]);

        Assert.Equal(4, serverNeeded.Count);
        Assert.True(serverNeeded[0]);   // id: TRUE
        Assert.True(serverNeeded[1]);   // name: TRUE  
        Assert.False(serverNeeded[2]);  // description: FALSE
        Assert.True(serverNeeded[3]);   // items: TRUE

        Assert.Equal(4, clientNeeded.Count);
        Assert.True(clientNeeded[0]);   // id: TRUE
        Assert.False(clientNeeded[1]);  // name: FALSE
        Assert.True(clientNeeded[2]);   // description: TRUE
        Assert.True(clientNeeded[3]);   // items: TRUE

        Assert.Equal(4, isArray.Count);
        Assert.False(isArray[0]);  // id: FALSE
        Assert.False(isArray[1]);  // name: FALSE
        Assert.False(isArray[2]);  // description: FALSE
        Assert.True(isArray[3]);   // items: TRUE
    }

    /// <summary>
    /// ParseCsvLineメソッドが正しく行をパースすることをテスト
    /// </summary>
    [Fact]
    public void ParseCsvLine_ValidLine_ReturnsCorrectFields()
    {
        // Arrange
        var content = "field1,field2,\"field3 with spaces\",field4\n";
        using var reader = new StringReader(content);
        using var streamReader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

        // Act
        var fields = CSVParser.ParseCsvLine(streamReader);

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(4, fields.Length);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("field2", fields[1]);
        Assert.Equal("field3 with spaces", fields[2]);
        Assert.Equal("field4", fields[3]);
    }

    /// <summary>
    /// ParseCsvLineメソッドがダブルクォート内のカンマを正しく処理することをテスト
    /// </summary>
    [Fact]
    public void ParseCsvLine_QuotedFieldWithComma_HandlesCorrectly()
    {
        // Arrange
        var content = "field1,\"field2, with comma\",field3\n";
        using var streamReader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

        // Act
        var fields = CSVParser.ParseCsvLine(streamReader);

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(3, fields.Length);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("field2, with comma", fields[1]);
        Assert.Equal("field3", fields[2]);
    }

    /// <summary>
    /// ParseCsvLineメソッドが空のフィールドを正しく処理することをテスト
    /// </summary>
    [Fact]
    public void ParseCsvLine_EmptyFields_HandlesCorrectly()
    {
        // Arrange
        var content = "field1,,field3,\n";
        using var streamReader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

        // Act
        var fields = CSVParser.ParseCsvLine(streamReader);

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(4, fields.Length);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("", fields[1]);
        Assert.Equal("field3", fields[2]);
        Assert.Equal("", fields[3]);
    }

    /// <summary>
    /// IsMainDataRowメソッドがメイン行を正しく判定することをテスト
    /// </summary>
    [Fact]
    public void IsMainDataRow_MainRow_ReturnsTrue()
    {
        // Arrange
        var row = new string[] { "1", "value1", "value2", "value3" };
        var columnNames = new List<string> { "id", "name", "description", "items" };
        var isArrayFlags = new List<bool> { false, false, false, true };

        // Act
        var result = CSVParser.IsMainDataRow(row, columnNames, isArrayFlags);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// IsMainDataRowメソッドが配列継続行を正しく判定することをテスト
    /// </summary>
    [Fact]
    public void IsMainDataRow_ArrayContinuationRow_ReturnsFalse()
    {
        // Arrange - 1列目が空で、非配列カラム（indexが1から始まるので2列目以降）にデータがない行
        var row = new string[] { "", "", "", "", "arrayValue" };  // 5要素にして、データ開始インデックス1を考慮
        var columnNames = new List<string> { "id", "name", "description", "items" };
        var isArrayFlags = new List<bool> { false, false, false, true };

        // Act
        var result = CSVParser.IsMainDataRow(row, columnNames, isArrayFlags);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// GetArrayColumnGroupメソッドが連続する配列カラムのグループを正しく取得することをテスト
    /// </summary>
    [Fact]
    public void GetArrayColumnGroup_ArrayColumn_ReturnsCorrectGroup()
    {
        // Arrange
        var isArrayFlags = new List<bool> { false, true, true, true, false };
        int targetColumnIndex = 2;

        // Act
        var group = CSVParser.GetArrayColumnGroup(targetColumnIndex, isArrayFlags);

        // Assert
        Assert.Equal(3, group.Count);
        Assert.Contains(1, group);
        Assert.Contains(2, group);
        Assert.Contains(3, group);
    }

    /// <summary>
    /// GetArrayColumnGroupメソッドが非配列カラムに対して空のグループを返すことをテスト
    /// </summary>
    [Fact]
    public void GetArrayColumnGroup_NonArrayColumn_ReturnsEmptyGroup()
    {
        // Arrange
        var isArrayFlags = new List<bool> { false, true, true, true, false };
        int targetColumnIndex = 0;

        // Act
        var group = CSVParser.GetArrayColumnGroup(targetColumnIndex, isArrayFlags);

        // Assert
        Assert.Empty(group);
    }
}