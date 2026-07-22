namespace SpireScope
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 旧名 StS2Toys 時代のユーザーデータ（設定・抽出アセット）を引き継ぐ。設定読み込みや
            // アセット解決より前に済ませる必要があるため、Form 構築前に呼ぶ。
            StS2Shared.Services.LegacyDataMigration.MigrateIfNeeded();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}