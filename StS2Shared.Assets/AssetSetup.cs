using StS2Shared.Services;

namespace StS2Shared.Assets;

/// <summary>
/// 初回セットアップ／再同期の頭脳（UI 非依存）。ユーザーの <c>.pck</c> からビューア用アセットを
/// 配布ディレクトリ（<see cref="AssetLocator.DistributionAssetsRoot"/>）配下へトランザクショナルに抽出する。
///
/// 抽出はまず <c>_staging</c> に対して行い、成功して初めて <c>v{version}</c> へ原子的にリネームする。
/// これにより、抽出の中断・失敗で中途半端な <c>v{version}</c> が残る（＝次回起動で「セットアップ済み」と
/// 誤判定され画像欠けのまま再プロンプトされない）事態を防ぐ。GUI ウィザードと CLI の双方から再利用できる。
/// </summary>
public static class AssetSetup
{
    /// <summary>ステージング用ディレクトリ名（先頭が <c>v</c> でないため <see cref="AssetLocator.LatestDistributionVersionDir"/> の <c>v*</c> 走査に載らない）。</summary>
    const string StagingDirName = "_staging";

    /// <summary>ゲームバージョンが不明な場合のフォールバック名（<c>v*</c> 形式かつ整数抽出で最小になり、実バージョンが常に優先される）。</summary>
    const string UnknownVersion = "v0.0.0-unknown";

    /// <summary>
    /// <paramref name="install"/> の <c>.pck</c> からビューア用アセットを抽出し、
    /// <c>%LocalAppData%\SpireScope\assets\v{version}</c> に配置する。生成した最終ディレクトリのパスを返す。
    /// 途中でキャンセル・失敗した場合はステージングを片付けて例外を送出する（最終ディレクトリは変更しない）。
    /// </summary>
    public static string RunSetup(Sts2Install install,
        IProgress<ExtractProgress>? progress = null, CancellationToken ct = default)
    {
        var distRoot = AssetLocator.DistributionAssetsRoot;
        Directory.CreateDirectory(distRoot);

        var version = NormalizeVersion(install.Version);
        var staging = Path.Combine(distRoot, StagingDirName);
        var final = Path.Combine(distRoot, version);

        // 前回の残骸を除去してからやり直す。
        DeleteIfExists(staging);

        try
        {
            using (var pck = new PckReader(install.PckPath))
            {
                var extractor = new AssetExtractor(pck, staging);
                extractor.ExtractViewerAssets(progress, ct);
            }
            ct.ThrowIfCancellationRequested();

            // 成功したので既存の同バージョンを置き換えて公開（同一ボリューム内の Move）。
            DeleteIfExists(final);
            Directory.Move(staging, final);
            return final;
        }
        catch
        {
            TryDelete(staging);
            throw;
        }
    }

    /// <summary>バージョン文字列を <c>v...</c> 形式に正規化。null/空はフォールバック名。</summary>
    static string NormalizeVersion(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return UnknownVersion;
        v = v.Trim();
        return v.StartsWith('v') || v.StartsWith('V') ? v : "v" + v;
    }

    static void DeleteIfExists(string dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    static void TryDelete(string dir)
    {
        try { DeleteIfExists(dir); } catch { /* クリーンアップ失敗は握りつぶす（本来の例外を優先） */ }
    }
}
