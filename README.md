# 20250825_masterdata_sheet_parser_cs

  概要：
  - MasterDataSheetParser.csproj: .NET 9.0ベースのコンソールアプリケーションプロジェクト
  - Program.cs: CSVファイルを読み込んで行番号付きで標準出力にダンプするメイン実装

  使用方法：
  dotnet run <CSVファイルパス>

  例：
  dotnet run data.csv

  ツールは以下の機能を提供します：
  - コマンドライン引数でCSVファイルパスを受け取り
  - ファイルの存在確認とエラーハンドリング
  - 全行を行番号（4桁0埋め）付きで標準出力にダンプ
  - 読み込み行数の合計表示
