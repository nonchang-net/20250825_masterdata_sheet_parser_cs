# MasterDataSheetParser

ゲーム実装のためのGoogle Sheets管理による特定フォーマット※のマスターデータを、CSVダウンロードを経由してjsonに変換するCLIツールです。
※このリポジトリ上では、シートのフォーマットの説明は割愛します。後日別記事で用意する想定

Google SheetsからのCSV自動ダウンロード機能、デバッグのためのログダンプ機能、フォルダ中のCSVを一括変換する機能を用意しています。

## 概要

- MasterDataSheetParser.csproj: .NET 9.0ベースのコンソールアプリケーションプロジェクト
- Program.cs: CSVファイルのシステム処理フラグを解析し、構造化データとして出力するメイン実装
- GoogleSheetsDownloader.cs: Google Sheets APIを使用したCSV自動ダウンロード機能
- SheetsProcessor.cs: Google Sheetsからの一括処理とJSON変換の統合処理
- CSVParser.cs: CSV解析とシステムフラグ処理
- JsonOutputter.cs: JSON/JSON2出力処理
- DumpOutputter.cs: ダンプ出力処理
- BatchProcessor.cs: バッチ変換処理


## dotnet runによる使用方法

### 基本的な使用方法

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

### Google Sheetsダウンロード機能

#### HTTP経由でのダウンロード（従来方式）
```bash
# デフォルトシート（messages, commands, inventories, actors）をdownloads/フォルダにダウンロード
dotnet run sheetsDownload <Google SheetsURL>

# 指定したシートのみダウンロード
dotnet run sheetsDownload <Google SheetsURL> sheet1 sheet2

# カスタムフォルダにダウンロード
dotnet run sheetsDownload <Google SheetsURL> --folder=output

# 変換後にCSVファイルを削除（JSONファイルのみ残す）
dotnet run sheetsDownload <Google SheetsURL> --cleanup

# すべてのオプションを組み合わせ
dotnet run sheetsDownload <Google SheetsURL> --folder=data --cleanup sheet1 sheet2

# テスト中URLの例
dotnet run sheetsDownload "https://docs.google.com/spreadsheets/d/1aI3tU5cELbUwYWanWs8Stal6lEDVTEbCNZu32R0IazA/edit?usp=sharing" --folder=tempwork
```

#### Google Sheets API v4経由でのダウンロード（高信頼方式）🆕
**データ欠損問題を解決する推奨方式**

```bash
# Google Sheets API経由でのダウンロード（アプリケーションデフォルト認証）
dotnet run sheetsApi <Google SheetsURL>

# サービスアカウントキーファイルを使用
dotnet run sheetsApi <Google SheetsURL> --key=service-account.json

# 指定したシートのみAPI経由でダウンロード
dotnet run sheetsApi <Google SheetsURL> sheet1 sheet2

# カスタムフォルダ + サービスアカウント認証
dotnet run sheetsApi <Google SheetsURL> --folder=output --key=path/to/key.json

# 変換後クリーンアップ + API方式
dotnet run sheetsApi <Google SheetsURL> --cleanup --key=service-account.json

# すべてのオプションを組み合わせ
dotnet run sheetsApi <Google SheetsURL> --folder=data --cleanup --key=path/to/key.json sheet1 sheet2
```

**Google Sheets API認証設定方法：**
1. **アプリケーションデフォルト認証**: `gcloud auth application-default login` 実行後、認証情報が自動的に使用されます
2. **サービスアカウントキー**: GCPコンソールでサービスアカウントを作成し、JSONキーファイルを `--key` オプションで指定
3. **環境変数認証**: `GOOGLE_APPLICATION_CREDENTIALS_JSON` 環境変数にサービスアカウントJSONを設定

## 使用例

### 基本的な使用例

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

### Google Sheetsダウンロードの使用例

#### HTTP方式（従来）
```bash
# 基本的なダウンロード（デフォルトシートをdownloads/フォルダに）
dotnet run sheetsDownload "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit"

# 特定のシートのみダウンロード
dotnet run sheetsDownload "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit" commands actors

# production環境用（カスタムフォルダ + クリーンアップ）
dotnet run sheetsDownload "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit" --folder=assets/data --cleanup

# 開発時の部分更新（特定シートのみ、クリーンアップなし）
dotnet run sheetsDownload "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit" --folder=dev_data messages
```

#### Google Sheets API方式（推奨）🆕
```bash
# 高信頼ダウンロード（データ欠損なし）
dotnet run sheetsApi "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit"

# サービスアカウント認証での本番環境用
dotnet run sheetsApi "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit" --key=prod-service-account.json --folder=assets/data --cleanup

# 開発環境での部分更新（API方式）
dotnet run sheetsApi "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit" --folder=dev_data messages commands

# CI/CD パイプライン用（サービスアカウント + 自動クリーンアップ）
dotnet run sheetsApi "https://docs.google.com/spreadsheets/d/YOUR_SHEET_ID/edit" --key=$SERVICE_ACCOUNT_KEY_PATH --folder=build/data --cleanup
```

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
./bin/Debug/net9.0/MasterDataSheetParser batchConvert ./tempwork
```

## 機能

### Google Sheetsダウンロードモード
**2つのダウンロード方式を提供：**

#### HTTP方式（sheetsDownload）
- Google SheetsのパブリックCSVエクスポートURLを使用した従来方式
- 認証不要で手軽に使用可能
- 一部のデータで欠損が発生する場合があります

#### Google Sheets API v4方式（sheetsApi）🆕
- **データ欠損問題を解決する推奨方式**
- Google公式APIを使用した高信頼データ取得
- 3種類の認証方式をサポート（ADC、サービスアカウントキー、環境変数）
- CSVエスケープ処理の完全対応
- より安定したデータ整合性を保証

#### 共通機能
- デフォルトシート（messages, commands, inventories, actors）の一括ダウンロード
- カスタムシート名の指定による部分的なデータ取得
- `--folder=フォルダ名` による出力先フォルダのカスタマイズ
- `--cleanup` による変換後のCSVファイル自動削除（JSONファイルのみ残す）
- エラーハンドリングと詳細な進行状況表示
- **ワークフロー例**: Google Sheets更新 → 1コマンドでプロダクション用JSONファイル生成

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

## テスト

このプロジェクトには包括的なテストスイートが含まれています。

### テスト実行方法

```bash
# 全テストを実行
dotnet test MasterDataSheetParser.Tests/

# より詳細な出力で実行
dotnet test MasterDataSheetParser.Tests/ --verbosity normal

# テストプロジェクトディレクトリから実行
cd MasterDataSheetParser.Tests
dotnet test
```

### テスト内容

#### CSVParserTests（8テスト）
- **システムフラグ解析テスト**: server_needed、client_needed、is_array、column_nameの正しい解析
- **CSV行パース機能**: 基本的なフィールド分割、ダブルクォート内のカンマ処理、空フィールドの処理
- **行判定ロジック**: メイン行と配列継続行の正しい判定
- **配列グループ機能**: 連続する配列カラムのグループ化と取得

#### JsonOutputterTests（4テスト）
- **JSON出力機能**: 配列形式のJSON出力とエラーハンドリング
- **JSON2出力機能**: 連想配列形式のJSON出力とエラーハンドリング
- **出力構造検証**: 生成されるJSONの基本的な構造とデータ型の検証

#### ProgramTests（12テスト）
- **引数検証**: 引数なし、引数過多、無効な出力モードのエラーハンドリング
- **ファイル存在確認**: 存在しないファイルやディレクトリに対するエラーハンドリング
- **各出力モード**: json、json2、dump、batchConvert、sheetsDownloadモードの基本動作確認
- **Google Sheetsオプション**: フォルダ指定、クリーンアップオプション、カスタムシート名の引数解析

#### GoogleSheetsDownloaderTests（5テスト）
- **URL解析**: Google SheetsのURLからスプレッドシートIDを抽出
- **エラーハンドリング**: 無効なURLや空文字列の処理
- **CSV URL生成**: 正しいCSVエクスポートURL形式の検証

#### SheetsProcessorTests（4テスト）
- **統合処理**: Google SheetsからJSON変換までの統合処理テスト
- **フォルダ操作**: 存在しないフォルダや空フォルダでのエラーハンドリング
- **カスタムオプション**: カスタムシート名やフォルダ指定の処理

### テスト結果
- **総テスト数**: 35個
- **成功率**: 100%（35/35個が成功）
- **カバレッジ**: 主要なビジネスロジック、Google Sheets連携、エラーハンドリングをカバー

テストはxUnitフレームワークを使用しており、継続的な開発とリファクタリングの安全性を担保します。

## 新機能のワークフロー例

### 開発フェーズ（Quick & Easy）
1. Google Sheetsでマスターデータを編集
2. `dotnet run sheetsDownload "URL" --folder=dev_data` でローカルに開発用データを取得（認証不要）
3. ゲーム内でテスト・調整
4. Google Sheetsを再編集して手順2からリピート

### 開発フェーズ（High Reliability）
1. Google Sheetsでマスターデータを編集
2. `dotnet run sheetsApi "URL" --folder=dev_data` でAPIによる高信頼データ取得
3. ゲーム内でテスト・調整（データ欠損なし）
4. Google Sheetsを再編集して手順2からリピート

### プロダクション配布
1. Google Sheetsで最終調整完了
2. `dotnet run sheetsApi "URL" --folder=assets/data --cleanup --key=prod-service-account.json` でクリーンな本番用JSONファイル生成
3. 生成されたJSONファイルをゲームビルドに含めて配布

### CI/CDパイプライン統合
1. Google Sheets更新をトリガーに自動ビルド開始
2. `dotnet run sheetsApi "URL" --folder=build/data --cleanup --key=$SERVICE_ACCOUNT_KEY` で自動データ取得
3. テスト実行後、自動デプロイ

### 部分更新
1. 特定のシート（例：commands）のみ更新したい場合
2. `dotnet run sheetsApi "URL" --folder=assets/data --cleanup commands` でAPI経由の必要な分のみ取得
3. 他のマスターデータはそのまま維持

## 既知の課題と制限

### 一般的な制限
- 現状では、is_arrayによるグループ集計の設定が一箇所でしか利用できない状態

### HTTP方式（sheetsDownload）
- Google Sheetsの公開権限が必要（エクスポート用URLにアクセスするため）
- **データ欠損が発生する場合がある**（特定の文字エンコードや特殊文字を含むセルで問題が生じる可能性）

### Google Sheets API方式（sheetsApi）
- GCP プロジェクトでGoogle Sheets APIの有効化が必要
- 認証設定（サービスアカウント作成またはADC設定）が必要
- API レート制限の適用（通常の使用では問題になりませんが、大量リクエスト時は注意）

### 推奨事項
- **本番環境やデータ整合性が重要な用途では `sheetsApi` モードの使用を強く推奨**
- 開発・テスト用途では手軽さ重視で `sheetsDownload` モードも利用可能
- CI/CDパイプラインではサービスアカウント認証での `sheetsApi` モードが最適