using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using XeviShot.Audio;
using XeviShot.Entities;
using XeviShot.Input;
using XeviShot.Localization;
using XeviShot.Settings;

namespace XeviShot.Forms;

public enum GameState
{
    Title,
    Playing,
    GameOver,
    Clear
}

/// <summary>
/// ゲームのメインウィンドウフォーム
/// ウィンドウ位置・サイズの保存・復元、アスペクト比維持スケーリング描画、60FPSゲームループを実装
/// </summary>
public class GameForm : Form
{
    private const int VirtualWidth = 480;
    private const int VirtualHeight = 640;

    private readonly InputManager _input = new();
    private Game? _game;
    private GameState _currentState = GameState.Title;
    private int _blinkTimer = 0;

    private readonly System.Windows.Forms.Timer _gameLoopTimer;
    private readonly Bitmap _renderTarget;
    private readonly Graphics _targetGraphics;

    // フォントリソース (高DPI環境でも仮想キャンバス480x640内で固定サイズとなるようGraphicsUnit.Pixelを指定)
    private readonly Font _titleFont = new(FontFamily.GenericSansSerif, 32f, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _mainFont = new(FontFamily.GenericSansSerif, 16f, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _smallFont = new(FontFamily.GenericSansSerif, 12f, FontStyle.Regular, GraphicsUnit.Pixel);
    private readonly Font _uiFont = new(FontFamily.GenericMonospace, 14f, FontStyle.Bold, GraphicsUnit.Pixel);

    public GameForm()
    {
        // フォーム基本設定
        Text = "XeviShot";
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(VirtualWidth, VirtualHeight);
        MinimumSize = new Size(360, 480);
        BackColor = Color.Black;

        // フリッカー防止のダブルバッファリング
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true
        );
        UpdateStyles();

        // 仮想レンダリングターゲット初期化
        _renderTarget = new Bitmap(VirtualWidth, VirtualHeight);
        _targetGraphics = Graphics.FromImage(_renderTarget);
        _targetGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        _targetGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // キー入力イベント設定
        KeyPreview = true;
        KeyDown += OnFormKeyDown;
        KeyUp += OnFormKeyUp;

        // ゲームループタイマー (約60FPS: 16ms)
        _gameLoopTimer = new System.Windows.Forms.Timer
        {
            Interval = 16
        };
        _gameLoopTimer.Tick += GameLoopStep;

        // アイコン適用
        try
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
            if (System.IO.File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }
        }
        catch { }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // 規約: 保存されたウィンドウ位置・サイズを復元
        RestoreWindowBounds();

        // オーディオ開始
        AudioManager.Instance.Initialize();
        AudioManager.Instance.PlayOpeningBgm();

        _gameLoopTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        _gameLoopTimer.Stop();
        AudioManager.Instance.Dispose();

        // 規約: ウィンドウ位置・サイズ・最大化状態を設定に保存
        SaveWindowBounds();
    }

    #region ウィンドウ位置・サイズの保存と復元
    private void RestoreWindowBounds()
    {
        var s = SettingsManager.Current;
        if (s.WindowWidth >= 300 && s.WindowHeight >= 400 && s.WindowX != int.MinValue && s.WindowY != int.MinValue)
        {
            var targetBounds = new Rectangle(s.WindowX, s.WindowY, s.WindowWidth, s.WindowHeight);

            // マルチモニター環境（負の座標を含む）でもいずれかのスクリーンの作業領域と交差していれば有効
            bool isVisibleOnAnyScreen = false;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(targetBounds))
                {
                    isVisibleOnAnyScreen = true;
                    break;
                }
            }

            if (isVisibleOnAnyScreen)
            {
                StartPosition = FormStartPosition.Manual;
                DesktopBounds = targetBounds;
            }
            else
            {
                Size = new Size(s.WindowWidth, s.WindowHeight);
                CenterToScreen();
            }
        }
        else if (s.WindowWidth >= 300 && s.WindowHeight >= 400)
        {
            Size = new Size(s.WindowWidth, s.WindowHeight);
            CenterToScreen();
        }
        else
        {
            CenterToScreen();
        }

        if (s.IsMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    private void SaveWindowBounds()
    {
        var s = SettingsManager.Current;
        Rectangle normalBounds = WindowState == FormWindowState.Normal ? DesktopBounds : RestoreBounds;
        if (normalBounds.Width >= 300 && normalBounds.Height >= 400)
        {
            s.WindowX = normalBounds.X;
            s.WindowY = normalBounds.Y;
            s.WindowWidth = normalBounds.Width;
            s.WindowHeight = normalBounds.Height;
        }
        s.IsMaximized = (WindowState == FormWindowState.Maximized);

        SettingsManager.Save();
    }
    #endregion

    #region 入力イベント
    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        _input.OnKeyDown(e.KeyCode);

        if (_currentState is GameState.Title or GameState.GameOver or GameState.Clear)
        {
            StartNewGame();
        }
    }

    private void OnFormKeyUp(object? sender, KeyEventArgs e)
    {
        _input.OnKeyUp(e.KeyCode);
    }

    private void StartNewGame()
    {
        AudioManager.Instance.StopOpeningBgm();
        int initialScore = (_currentState == GameState.Clear && _game != null) ? _game.Score : 0;
        _game = new Game(VirtualWidth, VirtualHeight, initialScore);
        _currentState = GameState.Playing;
        AudioManager.Instance.Play("start_jingle");
        AudioManager.Instance.PlayBgm();
    }
    #endregion

    #region ゲームループ (更新 & 描画)
    private void GameLoopStep(object? sender, EventArgs e)
    {
        _input.Update();

        // ゲームパッドでのスタート受付
        if (_currentState is GameState.Title or GameState.GameOver or GameState.Clear)
        {
            if (_input.GamepadAnyButton)
            {
                StartNewGame();
            }
        }

        if (_currentState == GameState.Playing && _game != null)
        {
            _game.Update(_input);

            if (_game.GameOver)
            {
                _currentState = GameState.GameOver;
                AudioManager.Instance.StopBgm();
                AudioManager.Instance.StopCharge();
                AudioManager.Instance.PlayOpeningBgm();
                _blinkTimer = 0;
            }
            else if (_game.StageClear)
            {
                _currentState = GameState.Clear;
                AudioManager.Instance.StopBgm();
                AudioManager.Instance.StopCharge();
                AudioManager.Instance.PlayOpeningBgm();
                _blinkTimer = 0;
            }
        }

        // 仮想バッファに描画
        RenderToVirtualBuffer();

        // ウィンドウへ再描画を要求
        Invalidate();
    }

    private void RenderToVirtualBuffer()
    {
        _targetGraphics.Clear(Color.Black);

        switch (_currentState)
        {
            case GameState.Title:
                DrawTitleScreen(_targetGraphics);
                break;

            case GameState.Playing:
                _game?.Draw(_targetGraphics);
                DrawPlayingUI(_targetGraphics);
                break;

            case GameState.GameOver:
                _game?.Draw(_targetGraphics);
                DrawGameOverScreen(_targetGraphics);
                break;

            case GameState.Clear:
                _game?.Draw(_targetGraphics);
                DrawClearScreen(_targetGraphics);
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // クライアント領域内でアスペクト比 480:640 を保ち中央にスケーリング描画
        float scaleX = (float)ClientSize.Width / VirtualWidth;
        float scaleY = (float)ClientSize.Height / VirtualHeight;
        float scale = Math.Min(scaleX, scaleY);

        int renderW = (int)(VirtualWidth * scale);
        int renderH = (int)(VirtualHeight * scale);
        int offsetX = (ClientSize.Width - renderW) / 2;
        int offsetY = (ClientSize.Height - renderH) / 2;

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(_renderTarget, offsetX, offsetY, renderW, renderH);

        // レターボックス余白（黒）
        if (offsetX > 0)
        {
            using var brush = new SolidBrush(Color.Black);
            e.Graphics.FillRectangle(brush, 0, 0, offsetX, ClientSize.Height);
            e.Graphics.FillRectangle(brush, offsetX + renderW, 0, offsetX, ClientSize.Height);
        }
        if (offsetY > 0)
        {
            using var brush = new SolidBrush(Color.Black);
            e.Graphics.FillRectangle(brush, 0, 0, ClientSize.Width, offsetY);
            e.Graphics.FillRectangle(brush, 0, offsetY + renderH, ClientSize.Width, offsetY);
        }
    }
    #endregion

    #region UI描画メソッド (多言語対応)
    private void DrawTitleScreen(Graphics g)
    {
        // タイトル文字
        string title = LocalizationManager.TitleGame;
        using (var yellowBrush = new SolidBrush(Color.Yellow))
        {
            DrawCenteredString(g, title, _titleFont, yellowBrush, VirtualHeight / 4f - 15f);
        }

        // サブタイトル
        string subtitle = LocalizationManager.TitleSubtitle;
        using (var cyanBrush = new SolidBrush(Color.Cyan))
        {
            DrawCenteredString(g, subtitle, _smallFont, cyanBrush, VirtualHeight / 4f + 35f);
        }

        // 操作説明
        float startY = VirtualHeight / 2f - 30f;
        using var whiteBrush = new SolidBrush(Color.White);

        DrawCenteredString(g, LocalizationManager.MoveInstructions, _smallFont, whiteBrush, startY);
        DrawCenteredString(g, LocalizationManager.AttackInstructions, _smallFont, whiteBrush, startY + 28f);
        DrawCenteredString(g, LocalizationManager.BothAttackInstructions, _smallFont, whiteBrush, startY + 56f);

        if (_input.IsGamepadConnected)
        {
            using var padBrush = new SolidBrush(Color.FromArgb(0, 255, 255));
            DrawCenteredString(g, LocalizationManager.GamepadInstructions, _smallFont, padBrush, startY + 84f);
        }

        // 点滅スタート案内
        _blinkTimer++;
        if ((_blinkTimer % 60) < 30)
        {
            string startText = _input.IsGamepadConnected
                ? LocalizationManager.PressToStartPad
                : LocalizationManager.PressToStartKey;
            using var greenBrush = new SolidBrush(Color.FromArgb(0, 255, 0));
            DrawCenteredString(g, startText, _mainFont, greenBrush, startY + 130f);
        }

        // ハイスコア表示
        string hiText = $"{LocalizationManager.HighScoreText}: {SettingsManager.Current.HighScore}";
        using var goldBrush = new SolidBrush(Color.Gold);
        DrawCenteredString(g, hiText, _uiFont, goldBrush, VirtualHeight - 60f);
    }

    private void DrawPlayingUI(Graphics g)
    {
        if (_game == null) return;

        using var uiBrush = new SolidBrush(Color.Cyan);
        using var goldBrush = new SolidBrush(Color.Gold);

        // スコア表示
        string scoreText = $"{LocalizationManager.ScoreText}: {_game.Score}";
        g.DrawString(scoreText, _uiFont, uiBrush, 20f, 20f);

        // ハイスコア表示
        string hiText = $"{LocalizationManager.HighScoreText}: {_game.HighScore}";
        var hiSize = g.MeasureString(hiText, _uiFont);
        g.DrawString(hiText, _uiFont, goldBrush, (VirtualWidth - hiSize.Width) / 2f, 20f);

        // 残機表示
        string livesText = $"{LocalizationManager.LivesText}: {_game.Lives}";
        var livesSize = g.MeasureString(livesText, _uiFont);
        g.DrawString(livesText, _uiFont, uiBrush, VirtualWidth - livesSize.Width - 20f, 20f);

        // 装備・シールド状態表示
        if (_game.Player.HasLaser)
        {
            using var laserBrush = new SolidBrush(Color.LimeGreen);
            string laserText = _game.Player.WeaponLevel > 1 ? $"[LASER LV.{_game.Player.WeaponLevel}]" : "[LASER]";
            g.DrawString(laserText, _smallFont, laserBrush, 20f, VirtualHeight - 30f);
        }
        else if (_game.Player.WeaponLevel > 1)
        {
            using var wpBrush = new SolidBrush(Color.OrangeRed);
            g.DrawString($"[WEAPON LV.{_game.Player.WeaponLevel}]", _smallFont, wpBrush, 20f, VirtualHeight - 30f);
        }

        if (_game.Player.ShieldCount > 0)
        {
            using var shBrush = new SolidBrush(Color.DeepSkyBlue);
            string shText = $"{LocalizationManager.ShieldText}: {_game.Player.ShieldCount}";
            var shSize = g.MeasureString(shText, _smallFont);
            g.DrawString(shText, _smallFont, shBrush, VirtualWidth - shSize.Width - 20f, VirtualHeight - 30f);
        }

        // 画面下部中央のチャージ状況インジケーター
        DrawChargeStatusHUD(g, _game.Player);
    }

    private void DrawChargeStatusHUD(Graphics g, Player player)
    {
        long ticks = Environment.TickCount64;
        float centerX = VirtualWidth / 2f;
        float posY = VirtualHeight - 28f;

        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        if (player.ChargeTimer == 0)
        {
            // チャージ待機中ヒント
            using var tipBrush = new SolidBrush(Color.FromArgb(90, 120, 150));
            g.DrawString("[Z: HOLD TO CHARGE]", _smallFont, tipBrush, centerX, posY, sf);
        }
        else if (player.ChargeTimer < Player.ChargeMax)
        {
            // 第1段階: 通常波動砲チャージ中 (0〜1秒)
            int percent = (int)(player.ChargeTimer * 100f / Player.ChargeMax);
            string text = $"WAVE CHARGE {percent}%";
            using var chargeBrush = new SolidBrush(Color.FromArgb(0, 220, 255));
            g.DrawString(text, _smallFont, chargeBrush, centerX, posY, sf);

            // 小さなバー
            float barW = 100f;
            float barH = 3f;
            float barX = centerX - barW / 2f;
            float barY = posY + 10f;
            using (var bgBrush = new SolidBrush(Color.FromArgb(0, 40, 60)))
            {
                g.FillRectangle(bgBrush, barX, barY, barW, barH);
            }
            using (var fillBrush = new SolidBrush(Color.FromArgb(0, 220, 255)))
            {
                g.FillRectangle(fillBrush, barX, barY, barW * (percent / 100f), barH);
            }
        }
        else if (player.ChargeTimer < Player.ChargeMax2)
        {
            // 第2段階: 通常波動砲発射可能 ＆ 拡散波動砲チャージ中 (1〜10秒)
            float diffProgress = (float)(player.ChargeTimer - Player.ChargeMax) / (Player.ChargeMax2 - Player.ChargeMax);
            int percent = (int)(diffProgress * 100f);

            string text = $"[WAVE OK] DIFFUSION {percent}%";
            using var diffBrush = new SolidBrush(Color.FromArgb(255, 200, 0));
            g.DrawString(text, _smallFont, diffBrush, centerX, posY, sf);

            float barW = 120f;
            float barH = 4f;
            float barX = centerX - barW / 2f;
            float barY = posY + 10f;

            // 満タンのWAVEベース
            using (var waveBrush = new SolidBrush(Color.FromArgb(0, 180, 220)))
            {
                g.FillRectangle(waveBrush, barX, barY, barW, barH);
            }
            // 拡散チャージのオレンジ/ゴールド進捗
            using (var fillBrush = new SolidBrush(Color.FromArgb(255, 200, 0)))
            {
                g.FillRectangle(fillBrush, barX, barY, barW * diffProgress, barH);
            }
            using (var borderPen = new Pen(Color.White, 1f))
            {
                g.DrawRectangle(borderPen, barX, barY, barW, barH);
            }
        }
        else
        {
            // 最大段階: 拡散波動砲 MAX チャージ完了！
            Color maxColor = (ticks / 70) % 2 == 0 ? Color.Yellow : Color.FromArgb(255, 60, 60);
            using var maxBrush = new SolidBrush(maxColor);
            g.DrawString("★ MAX DIFFUSION WAVE READY ★", _mainFont, maxBrush, centerX, posY, sf);
        }
    }

    private void DrawGameOverScreen(Graphics g)
    {
        // 半透明暗転オーバーレイ
        using (var overlayBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
        {
            g.FillRectangle(overlayBrush, 0f, 0f, VirtualWidth, VirtualHeight);
        }

        // GAME OVER
        using (var yellowBrush = new SolidBrush(Color.Yellow))
        {
            DrawCenteredString(g, LocalizationManager.GameOver, _titleFont, yellowBrush, VirtualHeight / 3f);
        }

        // 最終スコア
        if (_game != null)
        {
            string scoreText = $"{LocalizationManager.FinalScore}{_game.Score}";
            using var whiteBrush = new SolidBrush(Color.White);
            DrawCenteredString(g, scoreText, _mainFont, whiteBrush, VirtualHeight / 2f);
        }

        // 再スタート案内
        _blinkTimer++;
        if ((_blinkTimer % 60) < 30)
        {
            string restartText = _input.IsGamepadConnected
                ? LocalizationManager.PressToRestartPad
                : LocalizationManager.PressToRestartKey;
            using var greenBrush = new SolidBrush(Color.FromArgb(0, 255, 0));
            DrawCenteredString(g, restartText, _smallFont, greenBrush, VirtualHeight / 2f + 60f);
        }
    }

    private void DrawClearScreen(Graphics g)
    {
        // 半透明暗転オーバーレイ
        using (var overlayBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
        {
            g.FillRectangle(overlayBrush, 0f, 0f, VirtualWidth, VirtualHeight);
        }

        // STAGE CLEAR
        using (var greenBrush = new SolidBrush(Color.FromArgb(0, 255, 0)))
        {
            DrawCenteredString(g, LocalizationManager.StageClear, _titleFont, greenBrush, VirtualHeight / 3f);
        }

        // 最終スコア
        if (_game != null)
        {
            string scoreText = $"{LocalizationManager.FinalScore}{_game.Score}";
            using var whiteBrush = new SolidBrush(Color.White);
            DrawCenteredString(g, scoreText, _mainFont, whiteBrush, VirtualHeight / 2f);
        }

        // 継続案内
        _blinkTimer++;
        if ((_blinkTimer % 60) < 30)
        {
            string continueText = _input.IsGamepadConnected
                ? LocalizationManager.PressToContinuePad
                : LocalizationManager.PressToContinueKey;
            using var greenBrush = new SolidBrush(Color.FromArgb(0, 255, 0));
            DrawCenteredString(g, continueText, _smallFont, greenBrush, VirtualHeight / 2f + 60f);
        }
    }

    private static void DrawCenteredString(Graphics g, string text, Font font, Brush brush, float y)
    {
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };
        g.DrawString(text, font, brush, VirtualWidth / 2f, y, sf);
    }
    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameLoopTimer.Dispose();
            _targetGraphics.Dispose();
            _renderTarget.Dispose();
            _titleFont.Dispose();
            _mainFont.Dispose();
            _smallFont.Dispose();
            _uiFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
