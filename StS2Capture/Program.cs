namespace StS2Capture;

internal static class Program
{
    /// <summary>
    /// 試験アプリのエントリポイント。ゲーム画面を監視し、カード提示画面で
    /// 表示中のカードを特定して一覧表示する。
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
