using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace MasterDataSheetParser.Tests;

/// <summary>
/// Programクラスのユニットテスト
/// </summary>
public class ProgramTests
{
    private readonly string _testDataPath = Path.Combine("TestData", "sample.csv");

    /// <summary>
    /// 引数が不正な場合のテスト（引数なし）
    /// </summary>
    [Fact]
    public void Main_NoArguments_ReturnsErrorCode()
    {
        // Arrange
        var args = new string[0];

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            var output = stringWriter.ToString();
            Assert.Contains("使用方法", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 引数が多すぎる場合のテスト
    /// </summary>
    [Fact]
    public void Main_TooManyArguments_ReturnsErrorCode()
    {
        // Arrange
        var args = new string[] { "json", "file.csv", "extra_arg" };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            var output = stringWriter.ToString();
            Assert.Contains("引数が多すぎます", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 無効な出力モードのテスト
    /// </summary>
    [Fact]
    public void Main_InvalidOutputMode_ReturnsErrorCode()
    {
        // Arrange
        var args = new string[] { "invalid_mode", "file.csv" };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            var output = stringWriter.ToString();
            Assert.Contains("無効な出力モード", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 存在しないファイルの場合のテスト
    /// </summary>
    [Fact]
    public void Main_NonExistentFile_ReturnsErrorCode()
    {
        // Arrange
        var args = new string[] { "json", "nonexistent_file.csv" };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            var output = stringWriter.ToString();
            Assert.Contains("ファイル", output);
            Assert.Contains("見つかりません", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 正常なファイル指定（出力モード省略）のテスト
    /// </summary>
    [Fact]
    public void Main_ValidFileWithDefaultMode_ReturnsSuccess()
    {
        // Arrange
        var args = new string[] { _testDataPath };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            
            // Verify JSON output is produced
            var output = stringWriter.ToString();
            Assert.False(string.IsNullOrEmpty(output));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 正常なファイル指定（JSON出力モード）のテスト
    /// </summary>
    [Fact]
    public void Main_ValidFileWithJsonMode_ReturnsSuccess()
    {
        // Arrange
        var args = new string[] { "json", _testDataPath };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            
            // Verify JSON output is produced
            var output = stringWriter.ToString();
            Assert.False(string.IsNullOrEmpty(output));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 正常なファイル指定（JSON2出力モード）のテスト
    /// </summary>
    [Fact]
    public void Main_ValidFileWithJson2Mode_ReturnsSuccess()
    {
        // Arrange
        var args = new string[] { "json2", _testDataPath };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            
            // Verify JSON output is produced
            var output = stringWriter.ToString();
            Assert.False(string.IsNullOrEmpty(output));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 正常なファイル指定（ダンプ出力モード）のテスト
    /// </summary>
    [Fact]
    public void Main_ValidFileWithDumpMode_ReturnsSuccess()
    {
        // Arrange
        var args = new string[] { "dump", _testDataPath };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            
            // Verify dump output is produced (should contain debug information)
            var output = stringWriter.ToString();
            Assert.False(string.IsNullOrEmpty(output));
            Assert.Contains("システム処理フラグの解析", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// BatchConvertモードで存在しないディレクトリを指定した場合のテスト
    /// </summary>
    [Fact]
    public void Main_BatchConvertWithNonExistentDirectory_ReturnsErrorCode()
    {
        // Arrange
        var args = new string[] { "batchConvert", "nonexistent_directory" };

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            var output = stringWriter.ToString();
            Assert.Contains("フォルダ", output);
            Assert.Contains("見つかりません", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}