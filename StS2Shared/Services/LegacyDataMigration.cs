namespace StS2Shared.Services;

/// <summary>
/// 旧名 <c>StS2Toys</c> 時代のユーザーデータフォルダを、新名 <c>SpireScope</c> へ引き継ぐ。
///
/// 対象は 2 系統：
/// <list type="bullet">
///   <item><c>%AppData%\{名前}</c> — settings.json / url_templates.json（ウィンドウ位置・言語・リンク設定）</item>
///   <item><c>%LocalAppData%\{名前}</c> — assets\v{version}（配布モードの抽出アセット。1 GB 級）</item>
/// </list>
/// アセットが大きいためコピーではなく <see cref="Directory.Move"/> で移す（同一ボリューム前提）。
/// 失敗しても例外は投げない — 最悪「未セットアップ扱いで再抽出」に落ちるだけで、データは壊れない。
/// </summary>
public static class LegacyDataMigration
{
    /// <summary>現行のユーザーデータフォルダ名。<see cref="AssetLocator.DistributionAssetsRoot"/> も参照する。</summary>
    public const string AppFolderName = "SpireScope";

    /// <summary>暫定名だった頃のフォルダ名。移行元としてのみ使う。</summary>
    const string LegacyAppFolderName = "StS2Toys";

    static bool _done;

    /// <summary>
    /// 旧フォルダがあり新フォルダが無い場合にのみ、フォルダごと移動する。プロセス内で 1 回だけ実行される。
    /// アセット解決（<see cref="AssetLocator"/>）や設定読み込みより前に呼ぶこと。
    /// </summary>
    public static void MigrateIfNeeded()
    {
        if (_done) return;
        _done = true;

        MoveIfNeeded(Environment.SpecialFolder.ApplicationData);
        MoveIfNeeded(Environment.SpecialFolder.LocalApplicationData);
    }

    static void MoveIfNeeded(Environment.SpecialFolder root)
    {
        try
        {
            var baseDir = Environment.GetFolderPath(root);
            if (string.IsNullOrEmpty(baseDir)) return;

            var legacy = Path.Combine(baseDir, LegacyAppFolderName);
            var current = Path.Combine(baseDir, AppFolderName);

            // 新フォルダが既にあるなら何もしない（二重実行・新旧混在時の安全策）。
            if (!Directory.Exists(legacy) || Directory.Exists(current)) return;

            Directory.Move(legacy, current);
        }
        catch (Exception ex)
        {
            // 移行はベストエフォート。ロックや権限で失敗しても起動は継続する。
            System.Diagnostics.Debug.WriteLine($"[LegacyDataMigration] {root} の移行に失敗: {ex.Message}");
        }
    }
}
