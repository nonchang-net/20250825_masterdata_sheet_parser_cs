using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace MasterDataSheetParser.Tests;

/// <summary>
/// SheetsProcessorクラスの単体テスト
/// </summary>
public class SheetsProcessorTests
{
    /// <summary>
    /// 結果表示機能のテスト - 存在しないフォルダ
    /// </summary>
    [Fact]
    public void ShowConversionResults_NonExistentFolder_DoesNotThrow()
    {
        // Arrange
        string nonExistentFolder = "non_existent_test_folder";

        // Act & Assert - 例外が発生しないことを確認
        var exception = Record.Exception(() => SheetsProcessor.ShowConversionResults(nonExistentFolder));
        Assert.Null(exception);
    }

    /// <summary>
    /// 結果表示機能のテスト - 空のフォルダ
    /// </summary>
    [Fact]
    public void ShowConversionResults_EmptyFolder_DoesNotThrow()
    {
        // Arrange
        string tempFolder = Path.Combine(Path.GetTempPath(), $"test_empty_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            // Act & Assert - 例外が発生しないことを確認
            var exception = Record.Exception(() => SheetsProcessor.ShowConversionResults(tempFolder));
            Assert.Null(exception);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }

    /// <summary>
    /// 結果表示機能のテスト - CSVとJSONファイルが混在するフォルダ
    /// </summary>
    [Fact]
    public void ShowConversionResults_FolderWithFiles_DoesNotThrow()
    {
        // Arrange
        string tempFolder = Path.Combine(Path.GetTempPath(), $"test_files_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            // テストファイルを作成
            File.WriteAllText(Path.Combine(tempFolder, "test1.csv"), "test,data\n1,2");
            File.WriteAllText(Path.Combine(tempFolder, "test1.json"), "{\"test\": \"data\"}");
            File.WriteAllText(Path.Combine(tempFolder, "test2.csv"), "test,data\n3,4");

            // Act & Assert - 例外が発生しないことを確認
            var exception = Record.Exception(() => SheetsProcessor.ShowConversionResults(tempFolder));
            Assert.Null(exception);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }

    /// <summary>
    /// Google Sheets URL処理のテスト - 無効なURL
    /// このテストは実際のHTTPリクエストを行わないため、
    /// URL検証ロジックのみをテスト
    /// </summary>
    [Fact]
    public async Task ProcessGoogleSheetsToJsonAsync_InvalidUrl_ReturnsFalse()
    {
        // Arrange
        string invalidUrl = "https://example.com/invalid";

        // Act
        bool result = await SheetsProcessor.ProcessGoogleSheetsToJsonAsync(invalidUrl, "test_downloads");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// カスタムシート名でのGoogle Sheets URL処理のテスト - 無効なURL
    /// </summary>
    [Fact]
    public async Task ProcessGoogleSheetsToJsonAsync_InvalidUrlWithCustomSheets_ReturnsFalse()
    {
        // Arrange
        string invalidUrl = "https://example.com/invalid";
        var customSheets = new List<string> { "custom1", "custom2" };

        // Act
        bool result = await SheetsProcessor.ProcessGoogleSheetsToJsonAsync(invalidUrl, "test_downloads", customSheets);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// フォルダ作成のテスト - 新しいフォルダが作成される
    /// </summary>
    [Fact]
    public async Task ProcessGoogleSheetsToJsonAsync_CreatesDownloadFolder()
    {
        // Arrange
        string testFolder = Path.Combine(Path.GetTempPath(), $"test_download_{Guid.NewGuid()}");
        string invalidUrl = "https://docs.google.com/spreadsheets/d/invalid/edit";

        try
        {
            // Actこのテストは無効なスプレッドシートIDでも実行される部分までテスト）
            await SheetsProcessor.ProcessGoogleSheetsToJsonAsync(invalidUrl, testFolder);

            // Assert - フォルダが作成されたことを確認
            Assert.True(Directory.Exists(testFolder));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testFolder))
            {
                Directory.Delete(testFolder, true);
            }
        }
    }
}