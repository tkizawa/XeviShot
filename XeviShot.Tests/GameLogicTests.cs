using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XeviShot.Audio;
using XeviShot.Entities;
using XeviShot.Input;
using XeviShot.Localization;
using XeviShot.Settings;

namespace XeviShot.Tests;

[TestClass]
public class GameLogicTests
{
    private static string _tempSettingsPath = "";
    private static string _originalPath = "";

    [ClassInitialize]
    public static void ClassSetup(TestContext context)
    {
        _originalPath = SettingsManager.SettingsFilePath;
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"XeviShot_Test_{Guid.NewGuid():N}.json");
        SettingsManager.SettingsFilePath = _tempSettingsPath;
    }

    [ClassCleanup]
    public static void ClassTeardown()
    {
        SettingsManager.SettingsFilePath = _originalPath;
        if (File.Exists(_tempSettingsPath))
        {
            try { File.Delete(_tempSettingsPath); } catch { }
        }
    }

    [TestInitialize]
    public void Setup()
    {
        SettingsManager.SettingsFilePath = _tempSettingsPath;
        SettingsManager.Load();
    }

    [TestMethod]
    public void TestSettingsSaveAndLoad()
    {
        // 1. 設定保存テスト
        SettingsManager.Current.WindowX = 150;
        SettingsManager.Current.WindowY = 200;
        SettingsManager.Current.WindowWidth = 500;
        SettingsManager.Current.WindowHeight = 700;
        SettingsManager.Current.HighScore = 99990;
        SettingsManager.Current.Language = "ja";

        SettingsManager.Save();

        Assert.IsTrue(File.Exists(_tempSettingsPath), "設定ファイルが存在すること");

        string jsonContent = File.ReadAllText(_tempSettingsPath);
        Assert.IsTrue(jsonContent.Contains("\"HighScore\": 99990"), "HighScoreが保存されていること");
        Assert.IsTrue(jsonContent.Contains("\"WindowX\": 150"), "WindowXが保存されていること");

        // 2. 読み込みテスト
        SettingsManager.Current.HighScore = 0;
        SettingsManager.Load();
        Assert.AreEqual(99990, SettingsManager.Current.HighScore, "保存したHighScoreが復元されること");
        Assert.AreEqual(150, SettingsManager.Current.WindowX, "保存したWindowXが復元されること");
    }

    [TestMethod]
    public void TestLocalization()
    {
        SettingsManager.Current.Language = "ja";
        LocalizationManager.Initialize();
        Assert.IsTrue(LocalizationManager.IsJapanese);
        Assert.AreEqual("RETRO SHOOTER", LocalizationManager.TitleGame);
        Assert.IsTrue(LocalizationManager.MoveInstructions.Contains("矢印キー"));

        SettingsManager.Current.Language = "en";
        LocalizationManager.Initialize();
        Assert.IsFalse(LocalizationManager.IsJapanese);
        Assert.IsTrue(LocalizationManager.MoveInstructions.Contains("ARROW KEYS"));
    }

    [TestMethod]
    public void TestPlayerWeaponAndShield()
    {
        var game = new Game(480, 640);
        var input = new InputManager();

        Assert.AreEqual(3, game.Lives);
        Assert.AreEqual(1, game.Player.WeaponLevel);
        Assert.IsFalse(game.Player.HasLaser);
        Assert.AreEqual(0, game.Player.ShieldCount);

        // シールドカプセル取得
        var shield = new ShieldCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(shield);
        game.Update(input);
        Assert.AreEqual(5, game.Player.ShieldCount, "シールドカプセル取得で耐久5になること");

        // ウェポンカプセル取得
        var weapon = new WeaponCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(weapon);
        game.Update(input);
        Assert.AreEqual(2, game.Player.WeaponLevel, "ウェポンカプセル取得でレベル2になること");

        // 通常弾モード (Lv2) → レーザーカプセル取得: レーザー有効化 & 通常弾レベルが1にリセット
        var laser = new LaserCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(laser);
        game.Update(input);
        Assert.IsTrue(game.Player.HasLaser, "レーザーカプセル取得でレーザー有効になること");
        Assert.AreEqual(1, game.Player.WeaponLevel, "レーザーカプセル取得で通常弾レベルが1にリセットされること");

        // レーザーモード → 通常弾カプセル取得: レーザー解除 & 通常弾レベル1にリセット
        var weapon2 = new WeaponCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(weapon2);
        game.Update(input);
        Assert.IsFalse(game.Player.HasLaser, "通常弾カプセル取得でレーザーが解除されること");
        Assert.AreEqual(1, game.Player.WeaponLevel, "レーザーモードから通常弾カプセル取得でレベル1になること");

        // 通常弾モード (Lv1) → 通常弾カプセル取得: レベル2へアップ
        var weapon3 = new WeaponCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(weapon3);
        game.Update(input);
        Assert.AreEqual(2, game.Player.WeaponLevel, "通常弾モードで取得した場合はレベル2へアップすること");

        // 通常弾モード → レーザーカプセル1つ目取得: レーザー有効化 & WEAPON LEVEL 1
        var laser1 = new LaserCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(laser1);
        game.Update(input);
        Assert.IsTrue(game.Player.HasLaser, "レーザー有効化");
        Assert.AreEqual(1, game.Player.WeaponLevel, "レーザー1つ目でLEVEL 1");

        // レーザーモード → レーザーカプセル2つ目取得: WEAPON LEVEL 2
        var laser2 = new LaserCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(laser2);
        game.Update(input);
        Assert.IsTrue(game.Player.HasLaser);
        Assert.AreEqual(2, game.Player.WeaponLevel, "レーザー2つ目でLEVEL 2");

        // レーザーモード → レーザーカプセル3つ目取得: WEAPON LEVEL 3
        var laser3 = new LaserCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(laser3);
        game.Update(input);
        Assert.IsTrue(game.Player.HasLaser);
        Assert.AreEqual(3, game.Player.WeaponLevel, "レーザー3つ目でLEVEL 3");

        // レーザーモード → レーザーカプセル4つ目取得: WEAPON LEVEL 3維持 (最大3)
        var laser4 = new LaserCapsule(game.Player.X, game.Player.Y);
        game.Items.Add(laser4);
        game.Update(input);
        Assert.IsTrue(game.Player.HasLaser);
        Assert.AreEqual(3, game.Player.WeaponLevel, "レーザー4つ目でも最大LEVEL 3維持");

        // レーザー弾の幅の検証
        var lb1 = new LaserBullet(0, 0, level: 1);
        var lb2 = new LaserBullet(0, 0, level: 2);
        var lb3 = new LaserBullet(0, 0, level: 3);
        Assert.IsTrue(lb2.Width > lb1.Width, "Lv2レーザーはLv1より幅が太いこと");
        Assert.IsTrue(lb3.Width > lb2.Width, "Lv3レーザーはLv2よりさらに幅が太いこと");
    }

    [TestMethod]
    public void TestWaveCannonEraseEnemyBullet()
    {
        var game = new Game(480, 640);
        var input = new InputManager();

        // 敵弾を配置
        var eb = new EnemyBullet(240, 200);
        game.EnemyBullets.Add(eb);

        // 波動砲を衝突位置に配置
        var wc = new WaveCannon(240, 200);
        game.WaveCannons.Add(wc);

        int initialScore = game.Score;
        game.Update(input);

        Assert.AreEqual(0, game.EnemyBullets.Count, "波動砲で敵弾がかき消されること");
        Assert.AreEqual(initialScore + 10, game.Score, "敵弾かき消しで10点加算されること");
    }

    [TestMethod]
    public void TestBombDestroysGroundEnemy()
    {
        var game = new Game(480, 640);
        var input = new InputManager();

        // 地上敵を配置
        var ground = new Enemy(200, 300, "ground");
        game.Enemies.Add(ground);

        // 爆発中のボムを同じ位置に配置
        var bomb = new Bomb(200, 300, 200, 300)
        {
            Exploded = true,
            ExplosionTimer = 1
        };
        game.Bombs.Add(bomb);

        int initialScore = game.Score;
        game.Update(input);

        Assert.AreEqual(0, game.Enemies.Count, "地上敵が破壊されること");
        Assert.AreEqual(initialScore + 300, game.Score, "地上敵撃破で300点加算されること");
    }

    [TestMethod]
    public void TestBossSpawnAndDefeat()
    {
        var game = new Game(480, 640);
        var input = new InputManager();

        // Tキー（デバッグスキップ）でフレーム数を進める
        input.OnKeyDown(System.Windows.Forms.Keys.T);
        game.Update(input);
        input.OnKeyUp(System.Windows.Forms.Keys.T);

        // 10800フレームまで進めてボス出現
        for (int i = 0; i < 301; i++)
        {
            game.Update(input);
        }

        Assert.IsTrue(game.BossActive, "10800フレーム到達でボスが出現すること");
        Assert.IsTrue(game.BossSpawned);

        // ボスにダメージを与えて撃破テスト
        Boss? boss = null;
        foreach (var e in game.Enemies)
        {
            if (e is Boss b)
            {
                boss = b;
                break;
            }
        }

        Assert.IsNotNull(boss);
        boss.State = "HOVER";
        boss.Y = 120f; // 画面内に配置

        // レーザー弾を当ててHPが削れることを確認
        int prevHp = boss.Hp;
        var lb = new LaserBullet(boss.X, boss.Y + 10f);
        game.LaserBullets.Add(lb);
        game.Update(input);

        Assert.IsTrue(boss.Hp < prevHp, "レーザー弾でボスのHPが削れること");

        // HPを1にしてとどめを刺す
        boss.Hp = 1;
        var lbFinish = new LaserBullet(boss.X, boss.Y + 10f);
        game.LaserBullets.Add(lbFinish);
        game.Update(input);

        Assert.AreEqual("DEFEATED", boss.State, "HP0でDEFEATEDになること");

        // 撃破演出タイマー進行
        boss.DeathTimer = 95;
        game.Update(input); // ボスがMarkedForDeletionになりリストから除去される
        game.Update(input); // 次フレームでBossActive=falseとなりStageClear=trueになる

        Assert.IsTrue(game.StageClear, "ボス撃破でステージクリアになること");
        Assert.IsFalse(game.BossActive);
    }

    [TestMethod]
    public void TestBackgroundProgression()
    {
        var bg = new Background(480, 640);

        // 1分経過まで: 森と川
        Assert.AreEqual(BackgroundTheme.ForestAndRiver, Background.GetTheme(0));
        Assert.AreEqual(BackgroundTheme.ForestAndRiver, Background.GetTheme(1800));
        Assert.AreEqual(BackgroundTheme.ForestAndRiver, Background.GetTheme(3599));

        // 2分経過まで: 街 (3600f〜7199f)
        Assert.AreEqual(BackgroundTheme.City, Background.GetTheme(3600));
        Assert.AreEqual(BackgroundTheme.City, Background.GetTheme(5400));
        Assert.AreEqual(BackgroundTheme.City, Background.GetTheme(7199));

        // 2分50秒まで: 要塞前哨基地 (7200f〜10199f)
        Assert.AreEqual(BackgroundTheme.Outpost, Background.GetTheme(7200));
        Assert.AreEqual(BackgroundTheme.Outpost, Background.GetTheme(9000));
        Assert.AreEqual(BackgroundTheme.Outpost, Background.GetTheme(10199));

        // 2分50秒以降: 敵要塞 (10200f〜)
        Assert.AreEqual(BackgroundTheme.Fortress, Background.GetTheme(10200));
        Assert.AreEqual(BackgroundTheme.Fortress, Background.GetTheme(10800));

        // UpdateとDrawが例外なく正常に実行できること
        using var bmp = new System.Drawing.Bitmap(480, 640);
        using var g = System.Drawing.Graphics.FromImage(bmp);

        int[] testFrames = { 0, 3600, 7200, 10200, 10800 };
        foreach (int frame in testFrames)
        {
            bg.Update(frame);
            bg.Draw(g);
        }
    }
}
