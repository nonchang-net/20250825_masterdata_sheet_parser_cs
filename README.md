# MasterDataSheetParser

CSVファイルを読み込んで、JSON形式またはダンプ形式で出力するCLIツールです。

## 概要

- MasterDataSheetParser.csproj: .NET 9.0ベースのコンソールアプリケーションプロジェクト
- Program.cs: CSVファイルのシステム処理フラグを解析し、構造化データとして出力するメイン実装

## ビルド方法

```bash
# プロジェクトをビルド
dotnet build

# リリースビルド
dotnet build --configuration Release
```

## 使用方法

```bash
# JSON出力（デフォルト）
dotnet run <CSVファイルパス>

# JSON出力（明示的）
dotnet run json <CSVファイルパス>

# ダンプ出力（従来形式）
dotnet run dump <CSVファイルパス>
```

## 使用例

```bash
# JSON形式で出力
dotnet run data.csv
dotnet run json data.csv

# ダンプ形式で出力
dotnet run dump data.csv
```

## 機能

### JSON出力モード（デフォルト）
- システム処理フラグ（server_needed、client_needed、is_array）をメタデータとして出力
- 実データを構造化されたJSON形式で出力
- 配列データの自動集計とグループ化

### ダンプ出力モード
- システム処理フラグの詳細解析結果を表示
- 実データを読みやすいテキスト形式で出力
- 配列データの集計結果を「:」区切りで表示

### 共通機能
- ファイルの存在確認とエラーハンドリング
- CSV内のダブルクォート対応と改行文字のエスケープ処理
- is_arrayフラグによる動的な配列データグループ判定

## 既知の課題

- 現状では、is_arrayによるグループ集計の設定が一箇所でしか利用できない状態。