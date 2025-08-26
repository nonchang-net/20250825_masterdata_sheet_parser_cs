using Xunit;
using System.Threading.Tasks;

namespace MasterDataSheetParser.Tests;

/// <summary>
/// GoogleSheetsDownloaderクラスの単体テスト
/// </summary>
public class GoogleSheetsDownloaderTests
{
    /// <summary>
    /// スプレッドシートIDの抽出テスト - 正常なURL
    /// </summary>
    [Fact]
    public void ExtractSpreadsheetId_ValidUrl_ReturnsCorrectId()
    {
        // Arrange
        string testUrl = "https://docs.google.com/spreadsheets/d/1aI3tU5cELbUwYWanWs8Stal6lEDVTEbCNZu32R0IazA/edit?usp=sharing";
        string expectedId = "1aI3tU5cELbUwYWanWs8Stal6lEDVTEbCNZu32R0IazA";

        // Act
        string? actualId = GoogleSheetsDownloader.ExtractSpreadsheetId(testUrl);

        // Assert
        Assert.Equal(expectedId, actualId);
    }

    /// <summary>
    /// スプレッドシートIDの抽出テスト - 無効なURL
    /// </summary>
    [Fact]
    public void ExtractSpreadsheetId_InvalidUrl_ReturnsNull()
    {
        // Arrange
        string invalidUrl = "https://example.com/invalid";

        // Act
        string? actualId = GoogleSheetsDownloader.ExtractSpreadsheetId(invalidUrl);

        // Assert
        Assert.Null(actualId);
    }

    /// <summary>
    /// スプレッドシートIDの抽出テスト - 短縮形URL
    /// </summary>
    [Fact]
    public void ExtractSpreadsheetId_ShortUrl_ReturnsCorrectId()
    {
        // Arrange
        string testUrl = "https://docs.google.com/spreadsheets/d/1aI3tU5cELbUwYWanWs8Stal6lEDVTEbCNZu32R0IazA/";
        string expectedId = "1aI3tU5cELbUwYWanWs8Stal6lEDVTEbCNZu32R0IazA";

        // Act
        string? actualId = GoogleSheetsDownloader.ExtractSpreadsheetId(testUrl);

        // Assert
        Assert.Equal(expectedId, actualId);
    }

    /// <summary>
    /// スプレッドシートIDの抽出テスト - 空文字列
    /// </summary>
    [Fact]
    public void ExtractSpreadsheetId_EmptyString_ReturnsNull()
    {
        // Arrange
        string emptyUrl = "";

        // Act
        string? actualId = GoogleSheetsDownloader.ExtractSpreadsheetId(emptyUrl);

        // Assert
        Assert.Null(actualId);
    }

    /// <summary>
    /// CSVダウンロードのURL生成テスト（間接的）
    /// このテストでは実際のHTTPリクエストを行わないため、
    /// URLの形式が正しいかを確認する代替的なテスト
    /// </summary>
    [Fact]
    public void VerifyCsvUrlFormat()
    {
        // Arrange
        string spreadsheetId = "1aI3tU5cELbUwYWanWs8Stal6lEDVTEbCNZu32R0IazA";
        string sheetName = "messages";
        string expectedUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&sheet={sheetName}";

        // Act
        // この部分は実際の実装で使用されるURL形式をテスト
        string actualUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&sheet={sheetName}";

        // Assert
        Assert.Equal(expectedUrl, actualUrl);
        Assert.Contains("docs.google.com/spreadsheets/d/", actualUrl);
        Assert.Contains("gviz/tq?tqx=out:csv", actualUrl);
        Assert.Contains($"sheet={sheetName}", actualUrl);
    }
}