using System;
using System.Windows.Forms;
using XeviShot.Forms;
using XeviShot.Localization;
using XeviShot.Settings;

namespace XeviShot;

internal static class Program
{
    /// <summary>
    /// アプリケーションのメイン エントリ ポイントです。
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // 高DPI対応および視覚スタイル初期化
        ApplicationConfiguration.Initialize();

        // 設定の読み込み (AppData\Local\XeviShot\settings.json)
        SettingsManager.Load();

        // 表示言語の初期化 (Windowsの表示言語モード準拠)
        LocalizationManager.Initialize();

        // メインゲームウィンドウの起動
        Application.Run(new GameForm());
    }
}
