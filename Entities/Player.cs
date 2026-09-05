using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using XeviShot.Audio;
using XeviShot.Input;

namespace XeviShot.Entities;

/// <summary>
/// プレイヤー自機「Xenon-Ray (ゼノン・レイ)」
/// </summary>
public class Player : Entity
{
    public float Speed { get; set; } = 4f;
    public int CooldownAir { get; set; } = 0;
    public int CooldownGround { get; set; } = 0;
    public float ReticleDistance { get; set; } = 120f;

    // チャージ（波動砲・拡散波動砲）の状態
    public int ChargeTimer { get; set; } = 0;
    public const int ChargeMax = 60;     // 1秒で通常波動砲
    public const int ChargeMax2 = 600;   // 10秒で拡散波動砲
    public bool WasFireAirPressed { get; set; } = false;
    public bool PlayedComplete { get; set; } = false;
    public bool PlayedComplete2 { get; set; } = false;

    // 発射トリガー
    public bool ShootNormal { get; set; } = false;
    public bool ShootWave { get; set; } = false;
    public bool ShootDiffusionWave { get; set; } = false;
    public string? RumbleTrigger { get; set; } = null;

    // パワーアップ状態
    public int ShieldCount { get; set; } = 0;
    public int WeaponLevel { get; set; } = 1;
    public bool HasLaser { get; set; } = false;

    private readonly Color _shipColor = Color.FromArgb(0, 255, 255);

    public Player(float x, float y)
    {
        X = x;
        Y = y;
        Width = 30f;
        Height = 30f;
    }

    public void UpdateInput(InputManager input, float canvasWidth, float canvasHeight)
    {
        // 移動
        if (input.Up) Y -= Speed;
        if (input.Down) Y += Speed;
        if (input.Left) X -= Speed;
        if (input.Right) X += Speed;

        // 画面内クランプ
        X = Math.Clamp(X, Width / 2f, canvasWidth - Width / 2f);
        Y = Math.Clamp(Y, Height / 2f, canvasHeight - Height / 2f);

        // クールダウン減算
        if (CooldownAir > 0) CooldownAir--;
        if (CooldownGround > 0) CooldownGround--;

        // 対空攻撃・チャージ判定 (Z または C または Pad A)
        bool fireAirPressed = input.FireAir || input.FireBoth;

        if (fireAirPressed)
        {
            if (!WasFireAirPressed)
            {
                if (CooldownAir <= 0)
                {
                    ShootNormal = true;
                }
            }

            ChargeTimer = Math.Min(ChargeMax2, ChargeTimer + 1);

            if (ChargeTimer == 1)
            {
                AudioManager.Instance.PlayCharge();
            }
            else if (ChargeTimer == ChargeMax)
            {
                if (!PlayedComplete)
                {
                    AudioManager.Instance.Play("charge_complete");
                    PlayedComplete = true;
                    RumbleTrigger = "charge_complete";
                }
            }
            else if (ChargeTimer == ChargeMax2)
            {
                if (!PlayedComplete2)
                {
                    AudioManager.Instance.Play("charge_complete");
                    PlayedComplete2 = true;
                    RumbleTrigger = "charge_complete2";
                }
            }
        }
        else
        {
            if (WasFireAirPressed)
            {
                AudioManager.Instance.StopCharge();
                if (ChargeTimer >= ChargeMax2)
                {
                    ShootDiffusionWave = true;
                }
                else if (ChargeTimer >= ChargeMax)
                {
                    ShootWave = true;
                }

                ChargeTimer = 0;
                PlayedComplete = false;
                PlayedComplete2 = false;
            }
        }

        WasFireAirPressed = fireAirPressed;
    }

    public override void Update()
    {
        // 外部からUpdateInput経由で更新される
    }

    public override void Draw(Graphics g)
    {
        long ticks = Environment.TickCount64;

        // 1. チャージ中のオーラ描画
        if (ChargeTimer > 0)
        {
            float chargeRatio = Math.Min(1.0f, (float)ChargeTimer / ChargeMax);
            float chargeRatio2 = Math.Max(0.0f, (float)(ChargeTimer - ChargeMax) / (ChargeMax2 - ChargeMax));

            float pulse = (float)(Math.Sin(ticks * 0.02) * 5.0);
            float radius = 25f * chargeRatio + 20f * chargeRatio2 + 5f + pulse;

            // オーラ円
            if (ChargeTimer >= ChargeMax2)
            {
                Color color = (ticks / 50) % 2 == 0 ? Color.Yellow : Color.Red;
                using var pen1 = new Pen(color, 3f);
                using var pen2 = new Pen(Color.FromArgb(255, 150, 0), 2f);
                g.DrawEllipse(pen1, X - radius, Y - radius, radius * 2, radius * 2);
                g.DrawEllipse(pen2, X - (radius - 6), Y - (radius - 6), (radius - 6) * 2, (radius - 6) * 2);
            }
            else if (chargeRatio >= 1.0f)
            {
                Color color = (ticks / 50) % 2 == 0 ? Color.White : Color.Cyan;
                using var pen1 = new Pen(color, 2f);
                using var pen2 = new Pen(Color.FromArgb(0, 170, 255), 1f);
                g.DrawEllipse(pen1, X - radius, Y - radius, radius * 2, radius * 2);
                g.DrawEllipse(pen2, X - (radius - 4), Y - (radius - 4), (radius - 4) * 2, (radius - 4) * 2);
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(0, 100, 255), 1f);
                g.DrawEllipse(pen, X - radius, Y - radius, radius * 2, radius * 2);
            }

            // 自機へ吸い込まれるエネルギー収束パーティクル
            int particleCount = ChargeTimer >= ChargeMax ? 8 : 4;
            Color pColor = ChargeTimer >= ChargeMax2
                ? Color.Yellow
                : (ChargeTimer >= ChargeMax ? Color.Cyan : Color.FromArgb(100, 180, 255));
            using var pBrush = new SolidBrush(pColor);

            for (int i = 0; i < particleCount; i++)
            {
                double baseAngle = i * (2.0 * Math.PI / particleCount) + (ticks * 0.003);
                float progress = (float)((ticks * 0.004 + (i * 0.25)) % 1.0);
                float dist = radius * (1.0f - progress);
                float px = (float)(X + Math.Cos(baseAngle) * dist);
                float py = (float)(Y + Math.Sin(baseAngle) * dist);
                float pSize = 2f + progress * 2f;
                g.FillEllipse(pBrush, px - pSize / 2f, py - pSize / 2f, pSize, pSize);
            }

            // 自機直下のミニチャージゲージ
            DrawMiniChargeGauge(g, chargeRatio, chargeRatio2, ticks);
        }

        // 2. 自機本体（デルタ戦闘機）
        PointF[] points =
        {
            new(X, Y - Height / 2f),
            new(X + Width / 2f, Y + Height / 2f),
            new(X, Y + Height / 4f),
            new(X - Width / 2f, Y + Height / 2f)
        };

        using (var brush = new SolidBrush(_shipColor))
        {
            g.FillPolygon(brush, points);
        }

        using (var pen = new Pen(Color.White, 1f))
        {
            g.DrawPolygon(pen, points);
        }

        // 3. シールド（周回オービットビット）
        if (ShieldCount > 0)
        {
            float shieldRadius = Width / 2f + 8f;
            using var shieldPen = new Pen(Color.Cyan, 2f);
            g.DrawEllipse(shieldPen, X - shieldRadius, Y - shieldRadius, shieldRadius * 2, shieldRadius * 2);

            using var bitBrush = new SolidBrush(Color.White);
            for (int i = 0; i < ShieldCount; i++)
            {
                double angle = i * (2.0 * Math.PI / ShieldCount) + ticks * 0.005;
                float hx = (float)(X + Math.Cos(angle) * shieldRadius);
                float hy = (float)(Y + Math.Sin(angle) * shieldRadius);
                g.FillEllipse(bitBrush, hx - 3f, hy - 3f, 6f, 6f);
            }
        }

        // 4. 前方照準レティクル（対地爆撃用）
        DrawReticle(g);
    }

    private void DrawReticle(Graphics g)
    {
        float rx = X;
        float ry = Y - ReticleDistance;

        using var redPen = new Pen(Color.Red, 2f);
        g.DrawLine(redPen, rx - 10f, ry, rx + 10f, ry);
        g.DrawLine(redPen, rx, ry - 10f, rx, ry + 10f);
        g.DrawRectangle(redPen, rx - 8f, ry - 8f, 16f, 16f);
    }

    private void DrawMiniChargeGauge(Graphics g, float chargeRatio, float chargeRatio2, long ticks)
    {
        float barWidth = 44f;
        float barHeight = 4f;
        float barX = X - barWidth / 2f;
        float barY = Y + Height / 2f + 8f;

        // ゲージ背景
        using (var bgBrush = new SolidBrush(Color.FromArgb(160, 0, 20, 40)))
        {
            g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);
        }

        // 第1段階: 通常波動砲ゲージ (シアン)
        float fill1 = barWidth * chargeRatio;
        using (var brush1 = new SolidBrush(Color.FromArgb(0, 220, 255)))
        {
            g.FillRectangle(brush1, barX, barY, fill1, barHeight);
        }

        // 第2段階: 拡散波動砲ゲージ (ゴールド〜オレンジのオーバーレイ)
        if (chargeRatio2 > 0f)
        {
            float fill2 = barWidth * chargeRatio2;
            Color gauge2Color = ChargeTimer >= ChargeMax2
                ? ((ticks / 60) % 2 == 0 ? Color.Gold : Color.Red)
                : Color.FromArgb(255, 180, 0);

            using var brush2 = new SolidBrush(gauge2Color);
            g.FillRectangle(brush2, barX, barY, fill2, barHeight);
        }

        // 枠線
        Color borderColor = ChargeTimer >= ChargeMax2
            ? Color.Yellow
            : (ChargeTimer >= ChargeMax ? Color.Cyan : Color.FromArgb(50, 100, 160));
        using (var pen = new Pen(borderColor, 1f))
        {
            g.DrawRectangle(pen, barX, barY, barWidth, barHeight);
        }

        // ゲージ下のテキスト表示
        using var miniFont = new Font(FontFamily.GenericMonospace, 8f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

        if (ChargeTimer >= ChargeMax2)
        {
            using var maxBrush = new SolidBrush((ticks / 60) % 2 == 0 ? Color.Yellow : Color.Red);
            g.DrawString("MAX", miniFont, maxBrush, X, barY + barHeight + 1f, sf);
        }
        else if (ChargeTimer >= ChargeMax)
        {
            using var waveBrush = new SolidBrush(Color.Cyan);
            g.DrawString("WAVE", miniFont, waveBrush, X, barY + barHeight + 1f, sf);
        }
    }
}
