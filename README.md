# MasterDataSheetParser

CSVファイルを読み込んで、JSON形式またはダンプ形式で出力するCLIツールです。

## 概要

- MasterDataSheetParser.csproj: .NET 9.0ベースのコンソールアプリケーションプロジェクト
- Program.cs: CSVファイルのシステム処理フラグを解析し、構造化データとして出力するメイン実装

## ビルド方法

```bash
# プロジェクトをビルド
dotnet build MasterDataSheetParser.csproj

# リリースビルド
dotnet build MasterDataSheetParser.csproj --configuration Release
```

ビルドして生成されたバイナリの実行例

```bash
./bin/Debug/net9.0/MasterDataSheetParser ./sample/20250825_2_command.csv
```

## 使用方法

```bash
# JSON2出力（デフォルト・ID列をキーとした連想配列形式）
dotnet run <CSVファイルパス>

# JSON出力（配列形式）
dotnet run json <CSVファイルパス>

# JSON2出力（明示的）
dotnet run json2 <CSVファイルパス>

# バッチ変換（フォルダ内CSVを一括JSON2変換）
dotnet run batchConvert <フォルダパス>

# ダンプ出力（従来形式）
dotnet run dump <CSVファイルパス>
```

## 使用例

```bash
# JSON2形式で出力（デフォルト・連想配列）
dotnet run data.csv
dotnet run json2 data.csv

# JSON形式で出力（配列）
dotnet run json data.csv

# バッチ変換（フォルダ内すべてのCSVファイルをJSON2形式に一括変換）
dotnet run batchConvert ./csvフォルダ

# ダンプ形式で出力
dotnet run dump data.csv
```

## 機能

### JSON2出力モード（デフォルト）
- ID列をキーとした連想配列形式でJSON出力
- マスターデータ間のリレーション解決時の検索コストを削減
- 配列データの自動集計とグループ化
- client_needed=TRUEの列のみを出力（nameは常時出力、id、ver、envsは除外）
- **注意**: オブジェクト内容からidは除外（キーとして使用されるため）

### JSON出力モード
- 実データを配列形式のJSON構造で出力
- 配列データの自動集計とグループ化
- client_needed=TRUEの列のみを出力（id、nameは常時出力、ver、envsは除外）

### バッチ変換モード
- 指定フォルダ内のすべてのCSVファイルを一括でJSON2形式に変換
- 元のCSVファイルは自動的に削除
- エラーハンドリングと処理結果の詳細表示
- 大量のマスターデータファイルの効率的な一括処理に最適

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