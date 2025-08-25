using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace MasterDataSheetParser.Tests;

/// <summary>
/// JsonOutputterクラスのユニットテスト
/// </summary>
public class JsonOutputterTests
{
    private readonly string _testDataPath = Path.Combine("TestData", "sample.csv");

    /// <summary>
    /// OutputAsJsonメソッドが正常に実行されることをテスト
    /// </summary>
    [Fact]
    public void OutputAsJson_ValidInput_ExecutesWithoutException()
    {
        // Arrange
        var filePath = _testDataPath;
        var (serverNeeded, clientNeeded, isArray, columnNames) = CSVParser.ParseSystemFlags(filePath, suppressOutput: true);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act & Assert - Should not throw exception
            JsonOutputter.OutputAsJson(filePath, columnNames, serverNeeded, clientNeeded, isArray);
            
            // Verify output is valid JSON
            var output = stringWriter.ToString();
            Assert.False(string.IsNullOrEmpty(output));
            
            // Try to parse the output as JSON to ensure it's valid
            var parsedJson = JsonSerializer.Deserialize<JsonElement>(output);
            Assert.Equal(JsonValueKind.Array, parsedJson.ValueKind);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// OutputAsJson2メソッドが正常に実行されることをテスト
    /// </summary>
    [Fact]
    public void OutputAsJson2_ValidInput_ExecutesWithoutException()
    {
        // Arrange
        var filePath = _testDataPath;
        var (serverNeeded, clientNeeded, isArray, columnNames) = CSVParser.ParseSystemFlags(filePath, suppressOutput: true);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act & Assert - Should not throw exception
            JsonOutputter.OutputAsJson2(filePath, columnNames, serverNeeded, clientNeeded, isArray);
            
            // Verify output is valid JSON
            var output = stringWriter.ToString();
            Assert.False(string.IsNullOrEmpty(output));
            
            // Try to parse the output as JSON to ensure it's valid
            var parsedJson = JsonSerializer.Deserialize<JsonElement>(output);
            Assert.Equal(JsonValueKind.Object, parsedJson.ValueKind);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// JSON出力形式の基本的な構造をテスト
    /// </summary>
    [Fact]
    public void OutputAsJson_ValidInput_ProducesExpectedStructure()
    {
        // Arrange
        var filePath = _testDataPath;
        var (serverNeeded, clientNeeded, isArray, columnNames) = CSVParser.ParseSystemFlags(filePath, suppressOutput: true);

        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            JsonOutputter.OutputAsJson(filePath, columnNames, serverNeeded, clientNeeded, isArray);
            var output = stringWriter.ToString();

            // Assert
            var jsonDocument = JsonDocument.Parse(output);
            var root = jsonDocument.RootElement;

            // Should be an array
            Assert.Equal(JsonValueKind.Array, root.ValueKind);

            // Array should have elements
            Assert.True(root.GetArrayLength() > 0);

            // Each element should be an object
            foreach (var item in root.EnumerateArray())
            {
                Assert.Equal(JsonValueKind.Object, item.ValueKind);
            }
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// JSON2出力形式の基本的な構造をテスト（連想配列形式）
    /// </summary>
    [Fact]
    public void OutputAsJson2_ValidInput_ProducesExpectedDictionaryStructure()
    {
        // Arrange
        var filePath = _testDataPath;
        var (serverNeeded, clientNeeded, isArray, columnNames) = CSVParser.ParseSystemFlags(filePath, suppressOutput: true);

        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            JsonOutputter.OutputAsJson2(filePath, columnNames, serverNeeded, clientNeeded, isArray);
            var output = stringWriter.ToString();

            // Assert
            var jsonDocument = JsonDocument.Parse(output);
            var root = jsonDocument.RootElement;

            // Should be an object (dictionary)
            Assert.Equal(JsonValueKind.Object, root.ValueKind);

            // Should have properties (keys)
            var propertyCount = 0;
            foreach (var property in root.EnumerateObject())
            {
                propertyCount++;
                // Each value should be an object
                Assert.Equal(JsonValueKind.Object, property.Value.ValueKind);
            }

            Assert.True(propertyCount > 0);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}