using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace XeviShot.Settings;

/// <summary>
/// アプリケーションの設定データモデル
/// </summary>
public class AppSettings
{
    // ウィンドウ位置・サイズ（初期値は未設定を表すint.MinValue）
    public int WindowX { get; set; } = int.MinValue;
    public int WindowY { get; set; } = int.MinValue;
    public int WindowWidth { get; set; } = 480;
    public int WindowHeight { get; set; } = 640;
    public bool IsMaximized { get; set; } = false;

    // ゲーム設定・ハイスコア（レトロアーケード標準の初期ハイスコア: 10,000）
    public int HighScore { get; set; } = 10000;
    public float MasterVolume { get; set; } = 1.0f;
    public float BgmVolume { get; set; } = 0.5f;
    public float SfxVolume { get; set; } = 0.8f;

    // 言語設定（"auto", "ja", "en"）
    public string Language { get; set; } = "auto";
}

/// <summary>
/// 設定の保存・読み込みを管理するマネージャー
/// 保存先: AppData\Local\XeviShot\settings.json
/// </summary>
public static class SettingsManager
{
    private static readonly string DefaultAppDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XeviShot"
    );

    public static string SettingsFilePath { get; set; } = Path.Combine(DefaultAppDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // 日本語がUnicodeエスケープ（\uXXXX）されず可視テキストとして保存されるように設定
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppSettings Current { get; private set; } = new();

    /// <summary>
    /// 設定ファイルを読み込みます。ファイルが存在しない場合は初期設定を作成します。
    /// </summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    Current = loaded;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"設定ファイルの読み込みに失敗しました: {ex.Message}");
        }

        Current = new AppSettings();
    }

    /// <summary>
    /// 現在の設定を AppData\Local\XeviShot\settings.json にUTF-8で保存します。
    /// </summary>
    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"設定ファイルの保存に失敗しました: {ex.Message}");
        }
    }
}
