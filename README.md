# StS2Toys

Slay the Spire 2 向けのツール集です。

## 必要環境

- .NET 10 SDK
- Visual Studio 2022 以降 または Rider
- Steam 版 Slay the Spire 2 がインストール済みであること

---

## セットアップ手順

### 1. リポジトリのクローンとブランチ作成

```powershell
git clone https://github.com/lichten/StS2Toys.git
cd StS2Toys
git checkout -b feature/your-branch-name
```

### 2. tools フォルダのセットアップ

`tools/` フォルダは Git 管理外です。初回セットアップ時のみ以下の手順を実行してください。

#### 2-1. GodotPCKExplorer を入手する

[GodotPCKExplorer の GitHub リリースページ](https://github.com/DmitriySalnikov/GodotPCKExplorer/releases) から最新版の zip をダウンロードし、以下の場所に展開します。

```
tools/
└── GodotPCKExplorer/
    └── GodotPCKExplorer.Console.exe  ← ここに配置
```

#### 2-2. ゲームデータを展開する

PowerShell で以下のコマンドを実行します。

```powershell
$pck = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
$out = ".\tools\extracted"

New-Item -ItemType Directory -Force -Path $out | Out-Null
.\tools\GodotPCKExplorer\GodotPCKExplorer.Console.exe -e $pck $out
```

展開が完了すると `tools/extracted/` 以下に約 1.8 GB のゲームアセットが生成されます。

> **補足**  
> PCK バージョンは `3.4.5.1`（Godot 4.5+ 形式）です。  
> Steam のゲームアップデート後にアセットが変わった場合は、`tools/extracted/` を削除してから再実行してください。

### 3. ビルド・実行

tools のセットアップが完了したら、ソリューション全体をビルドします。

```powershell
dotnet build
```

各アプリケーションは以下のコマンドで起動します。

| アプリ | コマンド | 説明 |
|--------|----------|------|
| セーブデータビューア | `dotnet run --project StS2Toys` | デッキ・レリックの閲覧 |
| カードブラウザ | `dotnet run --project StS2CardBrowser` | キャラクター・メカニクスでフィルタリング |
| 静的サイトジェネレータ | `dotnet run --project StS2SiteBuilder` | ゲーム情報サイトの生成 |
