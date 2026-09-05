using System.Globalization;
using XeviShot.Settings;

namespace XeviShot.Localization;

/// <summary>
/// 多言語（日本語・英語）表示を管理するクラス
/// Windowsの表示言語設定に応じて自動判定し、手動設定もサポートします。
/// </summary>
public static class LocalizationManager
{
    public static bool IsJapanese { get; private set; } = true;

    /// <summary>
    /// 設定およびWindowsの表示言語設定に基づいて言語を初期化します。
    /// </summary>
    public static void Initialize()
    {
        var langSetting = SettingsManager.Current.Language?.ToLowerInvariant();
        if (langSetting == "ja")
        {
            IsJapanese = true;
        }
        else if (langSetting == "en")
        {
            IsJapanese = false;
        }
        else
        {
            // Windowsの表示言語モード（CurrentUICulture）で自動判定
            var currentUiName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            IsJapanese = currentUiName == "ja";
        }
    }

    // 表示文字列プロパティ
    public static string TitleGame => "RETRO SHOOTER";
    public static string TitleSubtitle => "Xenon-Ray vs Galdora";

    public static string MoveInstructions => IsJapanese
        ? "矢印キー / [W][A][S][D] で移動"
        : "Press ARROW KEYS or [W][A][S][D] to Move";

    public static string AttackInstructions => IsJapanese
        ? "[Z] 対空 (長押しで波動砲) / [X] 対地爆撃"
        : "[Z] Air (Hold: Wave) / [X] Ground Bomb";

    public static string BothAttackInstructions => IsJapanese
        ? "[C] 対空・対地 同時発射"
        : "[C] Fire Both (Air & Ground)";

    public static string GamepadInstructions => IsJapanese
        ? "ゲームパッド: Lスティック / [A] 対空 / [B] 対地"
        : "Gamepad: L-Stick / [A] Air / [B] Ground";

    public static string PressToStartKey => IsJapanese
        ? "いずれかのキーを押してスタート"
        : "Press ANY KEY to Start";

    public static string PressToStartPad => IsJapanese
        ? "いずれかのボタンを押してスタート"
        : "Press ANY BUTTON to Start";

    public static string ScoreText => "SCORE";
    public static string HighScoreText => "HIGH";
    public static string LivesText => "LIVES";
    public static string ShieldText => "SHIELD";
    public static string WeaponText => "WEAPON";

    public static string GameOver => "GAME OVER";
    public static string StageClear => "STAGE CLEAR";
    public static string FinalScore => IsJapanese ? "最終スコア: " : "FINAL SCORE: ";

    public static string PressToRestartKey => IsJapanese
        ? "いずれかのキーを押して再スタート"
        : "Press ANY KEY to Restart";

    public static string PressToRestartPad => IsJapanese
        ? "いずれかのボタンを押して再スタート"
        : "Press ANY BUTTON to Restart";

    public static string PressToContinueKey => IsJapanese
        ? "いずれかのキーを押して次のステージへ"
        : "Press ANY KEY to Play Next Stage";

    public static string PressToContinuePad => IsJapanese
        ? "いずれかのボタンを押して次のステージへ"
        : "Press ANY BUTTON to Play Next Stage";
}
