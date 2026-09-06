using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace XeviShot.Entities;

/// <summary>
/// 敵が発射する弾
/// </summary>
public class EnemyBullet : Entity
{
    public float Vx { get; set; }
    public float Vy { get; set; }
    public Color Color { get; set; } = Color.FromArgb(255, 100, 100);

    public EnemyBullet(float x, float y, float vx = 0f, float vy = 5f)
    {
        X = x;
        Y = y;
        Width = 6f;
        Height = 6f;
        Vx = vx;
        Vy = vy;
    }

    public override void Update()
    {
        X += Vx;
        Y += Vy;
        if (Y > 700f || Y < -50f || X < -50f || X > 530f)
        {
            MarkedForDeletion = true;
        }
    }

    public override void Draw(Graphics g)
    {
        float r = Width / 2f;
        // 外郭グロー
        using (var glowBrush = new SolidBrush(Color.FromArgb(90, 255, 50, 50)))
        {
            g.FillEllipse(glowBrush, X - r - 1.5f, Y - r - 1.5f, (r + 1.5f) * 2, (r + 1.5f) * 2);
        }
        // 立体エネルギー球（赤色コア）
        using (var coreBrush = new SolidBrush(Color))
        {
            g.FillEllipse(coreBrush, X - r, Y - r, Width, Height);
        }
        // 中心高熱ハイライト
        using (var hiBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(hiBrush, X - r * 0.4f, Y - r * 0.4f, r * 0.8f, r * 0.8f);
        }
    }
}

/// <summary>
/// 通常敵（空中敵 Zoldas、高速強敵 Phaeon、地上砲台）
/// </summary>
public class Enemy : Entity
{
    public string Type { get; set; } // "air", "phaeon", "ground"
    public int ShootTimer { get; set; }
    public bool ShootNow { get; set; } = false;
    public float Speed { get; set; }

    // 空中敵用
    public int MovementType { get; set; }

    // Phaeon用
    public string State { get; set; } = "APPROACH";
    public float TargetY { get; set; }
    public float RetreatVx { get; set; }
    public float RetreatVy { get; set; }

    private static readonly Random Rand = new();

    public Enemy(float x, float y, string type)
    {
        X = x;
        Y = y;
        Type = type;
        ShootTimer = Rand.Next(30, 120);

        if (Type == "air")
        {
            Width = 24f;
            Height = 24f;
            Speed = 2f;
            MovementType = Rand.Next(0, 3);
        }
        else if (Type == "phaeon")
        {
            Width = 24f;
            Height = 24f;
            Speed = 4f;
            State = "APPROACH";
            TargetY = (float)(Rand.NextDouble() * (320 - 224) + 224);
            float[] retreatOptions = { -2f, 0f, 2f };
            RetreatVx = retreatOptions[Rand.Next(retreatOptions.Length)];
            RetreatVy = -6f;
        }
        else // "ground"
        {
            Width = 32f;
            Height = 32f;
            Speed = 1f;
        }
    }

    public override void Update()
    {
        if (Type == "air")
        {
            ShootTimer--;
            if (ShootTimer <= 0)
            {
                ShootNow = true;
                ShootTimer = Rand.Next(60, 180);
            }

            if (MovementType == 0)
            {
                Y += Speed;
            }
            else if (MovementType == 1)
            {
                Y += Speed * 0.8f;
                X += (float)(Math.Sin(Y * 0.05) * 3.0);
            }
            else if (MovementType == 2)
            {
                Y += Speed;
                X += Speed * (X > 240f ? -0.5f : 0.5f);
            }

            if (Y > 700f) MarkedForDeletion = true;
        }
        else if (Type == "phaeon")
        {
            if (State == "APPROACH")
            {
                Y += Speed;
                if (Y >= TargetY)
                {
                    State = "ATTACK";
                }
            }
            else if (State == "ATTACK")
            {
                ShootNow = true;
                State = "RETREAT";
            }
            else if (State == "RETREAT")
            {
                X += RetreatVx;
                Y += RetreatVy;
            }

            if (Y < -60f || Y > 700f || X < -60f || X > 540f)
            {
                MarkedForDeletion = true;
            }
        }
        else // "ground"
        {
            Y += Speed;
            if (Y > 700f) MarkedForDeletion = true;
        }
    }

    public override void Draw(Graphics g)
    {
        long ticks = Environment.TickCount64;

        if (Type == "air")
        {
            DrawAirEnemyZoldas(g, ticks);
        }
        else if (Type == "phaeon")
        {
            DrawPhaeon(g, ticks);
        }
        else // "ground"
        {
            DrawGroundTurret(g, ticks);
        }
    }

    /// <summary>
    /// 空中敵 Zoldas: 浮遊影、球冠状ソーサー装甲、ガラス球体センタースフィア
    /// </summary>
    private void DrawAirEnemyZoldas(Graphics g, long ticks)
    {
        // 1. 浮遊ドロップシャドウ（地面への投影）
        using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, X - Width * 0.4f + 6f, Y - Height * 0.2f + 10f, Width * 0.8f, Height * 0.45f);
        }

        // 2. 外周メタリック装甲リング
        float saucerW = Width;
        float saucerH = Height * 0.6f;
        float saucerX = X - saucerW / 2f;
        float saucerY = Y - saucerH / 2f;

        using (var rimBrush = new LinearGradientBrush(
            new PointF(saucerX, saucerY),
            new PointF(saucerX, saucerY + saucerH),
            Color.FromArgb(120, 30, 30),
            Color.FromArgb(60, 10, 15)))
        {
            g.FillEllipse(rimBrush, saucerX, saucerY, saucerW, saucerH);
        }

        // 3. 上部ソーサー本体（真紅の球冠ドーム・3Dシェーディング）
        float domeW = Width * 0.88f;
        float domeH = Height * 0.5f;
        float domeX = X - domeW / 2f;
        float domeY = Y - domeH / 2f - 1.5f;

        using (var domeBrush = new LinearGradientBrush(
            new PointF(domeX + domeW * 0.2f, domeY),
            new PointF(domeX + domeW * 0.8f, domeY + domeH),
            Color.FromArgb(255, 75, 75),
            Color.FromArgb(140, 15, 25)))
        {
            g.FillEllipse(domeBrush, domeX, domeY, domeW, domeH);
        }

        // ソーサー上部エッジのハイライト
        using (var rimPen = new Pen(Color.FromArgb(200, 255, 140, 140), 1f))
        {
            g.DrawArc(rimPen, domeX + 2f, domeY + 1f, domeW - 4f, domeH * 0.6f, 190, 160);
        }

        // 4. 周囲の微細な発光スリット（回転アニメーション）
        for (int i = 0; i < 4; i++)
        {
            double angle = (ticks * 0.004) + (i * Math.PI / 2.0);
            float slitX = (float)(X + Math.Cos(angle) * (Width * 0.35f));
            float slitY = (float)(Y + Math.Sin(angle) * (Height * 0.18f));
            using var slitBrush = new SolidBrush(Color.FromArgb(255, 220, 100));
            g.FillEllipse(slitBrush, slitX - 1f, slitY - 1f, 2f, 2f);
        }

        // 5. センタースフィア（立体エメラルドガラス球）
        float cr = Width * 0.28f;
        float cx = X - cr;
        float cy = Y - cr - 2f;

        // スフィア外枠
        using (var sphereFramePen = new Pen(Color.FromArgb(20, 60, 20), 1f))
        {
            g.DrawEllipse(sphereFramePen, cx, cy, cr * 2, cr * 2);
        }

        // スフィアベースグラデーション（左上から右下への球体ライティング）
        using (var sphereBrush = new LinearGradientBrush(
            new PointF(cx + cr * 0.3f, cy + cr * 0.3f),
            new PointF(cx + cr * 1.8f, cy + cr * 1.8f),
            Color.FromArgb(60, 255, 90),
            Color.FromArgb(5, 80, 20)))
        {
            g.FillEllipse(sphereBrush, cx, cy, cr * 2, cr * 2);
        }

        // ガラス曲面スペキュラハイライト
        using (var hiBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
        {
            g.FillEllipse(hiBrush, cx + cr * 0.45f, cy + cr * 0.35f, cr * 0.6f, cr * 0.5f);
        }
    }

    /// <summary>
    /// 高速強敵 Phaeon: 浮遊影、鋭角ステルス多面体装甲、ルビーセンサードーム、バーニア炎
    /// </summary>
    private void DrawPhaeon(Graphics g, long ticks)
    {
        float vx = State == "APPROACH" ? 0f : RetreatVx;
        float vy = State == "APPROACH" ? Speed : RetreatVy;

        float speed = (float)Math.Sqrt(vx * vx + vy * vy);
        float dx = speed > 0f ? vx / speed : 0f;
        float dy = speed > 0f ? vy / speed : 1f;

        // 1. 浮遊ドロップシャドウ（進行方向に応じたオフセット影）
        float sOffX = dx * 3f + 6f;
        float sOffY = dy * 3f + 10f;
        PointF sp1 = new(X + sOffX + dx * 15f, Y + sOffY + dy * 15f);
        PointF sp2 = new(X + sOffX - dy * 13f + dx * 4f, Y + sOffY + dx * 13f + dy * 4f);
        PointF sp3 = new(X + sOffX - dx * 9f, Y + sOffY - dy * 9f);
        PointF sp4 = new(X + sOffX + dy * 13f + dx * 4f, Y + sOffY - dx * 13f + dy * 4f);

        using (var shadowBrush = new SolidBrush(Color.FromArgb(85, 0, 0, 0)))
        {
            g.FillPolygon(shadowBrush, new[] { sp1, sp2, sp3, sp4 });
        }

        // 2. 機体頂点
        PointF nose = new(X + dx * 16f, Y + dy * 16f);
        PointF leftTip = new(X - dy * 14f + dx * 4f, Y + dx * 14f + dy * 4f);
        PointF tail = new(X - dx * 10f, Y - dy * 10f);
        PointF rightTip = new(X + dy * 14f + dx * 4f, Y - dx * 14f + dy * 4f);
        PointF spineMid = new(X - dx * 2f, Y - dy * 2f);

        // 3. スラスター噴射炎（後退・高速飛行時）
        float flameLen = 6f + (float)(Math.Sin(ticks * 0.1) * 3.0);
        PointF flameTip = new(tail.X - dx * flameLen, tail.Y - dy * flameLen);
        using (var flameBrush = new SolidBrush(Color.FromArgb(240, 255, 120, 0)))
        {
            g.FillPolygon(flameBrush, new[] { tail, new(tail.X - dy * 3f, tail.Y + dx * 3f), flameTip, new(tail.X + dy * 3f, tail.Y - dx * 3f) });
        }

        // 4. 左主翼面（受光面: ゴールドイエローグラデーション）
        PointF[] leftWing = { nose, leftTip, tail, spineMid };
        using (var leftBrush = new LinearGradientBrush(
            leftTip, nose,
            Color.FromArgb(255, 230, 40),
            Color.FromArgb(255, 255, 150)))
        {
            g.FillPolygon(leftBrush, leftWing);
        }

        // 5. 右主翼面（陰影面: ディープアンバー/ブロンズ）
        PointF[] rightWing = { nose, spineMid, tail, rightTip };
        using (var rightBrush = new LinearGradientBrush(
            nose, rightTip,
            Color.FromArgb(210, 150, 20),
            Color.FromArgb(140, 90, 5)))
        {
            g.FillPolygon(rightBrush, rightWing);
        }

        // 中央稜線ハイライト
        using (var hiPen = new Pen(Color.FromArgb(220, 255, 255, 200), 1f))
        {
            g.DrawLine(hiPen, nose, spineMid);
            g.DrawLine(hiPen, nose, leftTip);
        }
        using (var darkPen = new Pen(Color.FromArgb(160, 90, 50, 0), 1f))
        {
            g.DrawLine(darkPen, nose, rightTip);
            g.DrawLine(darkPen, rightTip, tail);
        }

        // 6. ルビーセンサードーム（立体レンズ）
        float cx = X + dx * 5f;
        float cy = Y + dy * 5f;
        float sr = 3.5f;

        using (var rubyBrush = new LinearGradientBrush(
            new PointF(cx - sr, cy - sr),
            new PointF(cx + sr, cy + sr),
            Color.FromArgb(255, 80, 80),
            Color.FromArgb(120, 0, 0)))
        {
            g.FillEllipse(rubyBrush, cx - sr, cy - sr, sr * 2, sr * 2);
        }
        using (var specBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(specBrush, cx - sr * 0.5f, cy - sr * 0.5f, 2f, 2f);
        }
    }

    /// <summary>
    /// 地上砲台: 接地アンビエントシャドウ、重装甲ピラミッド台座、ターンテーブル、パルスセンサードーム
    /// </summary>
    private void DrawGroundTurret(Graphics g, long ticks)
    {
        // 1. 接地アンビエントシャドウ（周囲の落影）
        using (var groundShadow = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
        {
            g.FillRectangle(groundShadow, X - Width / 2f + 4f, Y - Height / 2f + 4f, Width, Height);
        }

        // 2. ピラミッド型立体台座（上面と傾斜側面）
        float hw = Width / 2f;
        float hh = Height / 2f;
        float tw = Width * 0.32f; // 上面天板の半幅
        float th = Height * 0.32f;

        PointF pTopLeftOuter = new(X - hw, Y - hh);
        PointF pTopRightOuter = new(X + hw, Y - hh);
        PointF pBottomRightOuter = new(X + hw, Y + hh);
        PointF pBottomLeftOuter = new(X - hw, Y + hh);

        PointF pTopLeftInner = new(X - tw, Y - th);
        PointF pTopRightInner = new(X + tw, Y - th);
        PointF pBottomRightInner = new(X + tw, Y + th);
        PointF pBottomLeftInner = new(X - tw, Y + th);

        // 左側面（受光傾斜面：明るいバイオレット）
        using (var leftBrush = new LinearGradientBrush(
            pTopLeftOuter, pTopLeftInner,
            Color.FromArgb(175, 55, 185),
            Color.FromArgb(215, 90, 225)))
        {
            g.FillPolygon(leftBrush, new[] { pTopLeftOuter, pTopLeftInner, pBottomLeftInner, pBottomLeftOuter });
        }

        // 上側面（受光傾斜面：最も明るい）
        using (var topBrush = new LinearGradientBrush(
            pTopLeftOuter, pTopLeftInner,
            Color.FromArgb(220, 100, 230),
            Color.FromArgb(180, 60, 190)))
        {
            g.FillPolygon(topBrush, new[] { pTopLeftOuter, pTopRightOuter, pTopRightInner, pTopLeftInner });
        }

        // 右側面（影側面：濃いパープル）
        using (var rightBrush = new LinearGradientBrush(
            pTopRightInner, pBottomRightOuter,
            Color.FromArgb(110, 25, 120),
            Color.FromArgb(60, 10, 70)))
        {
            g.FillPolygon(rightBrush, new[] { pTopRightInner, pTopRightOuter, pBottomRightOuter, pBottomRightInner });
        }

        // 下側面（影側面：ダークパープル）
        using (var bottomBrush = new LinearGradientBrush(
            pBottomLeftInner, pBottomRightOuter,
            Color.FromArgb(100, 20, 110),
            Color.FromArgb(50, 10, 60)))
        {
            g.FillPolygon(bottomBrush, new[] { pBottomLeftInner, pBottomRightInner, pBottomRightOuter, pBottomLeftOuter });
        }

        // 上面デッキ（中央平坦部）
        using (var topDeckBrush = new SolidBrush(Color.FromArgb(145, 35, 155)))
        {
            g.FillRectangle(topDeckBrush, X - tw, Y - th, tw * 2, th * 2);
        }

        // 装甲リベット（四隅の金属鋲）
        using (var rivetBrush = new SolidBrush(Color.FromArgb(220, 200, 240)))
        {
            g.FillEllipse(rivetBrush, pTopLeftInner.X + 1f, pTopLeftInner.Y + 1f, 2f, 2f);
            g.FillEllipse(rivetBrush, pTopRightInner.X - 3f, pTopRightInner.Y + 1f, 2f, 2f);
            g.FillEllipse(rivetBrush, pBottomLeftInner.X + 1f, pBottomLeftInner.Y - 3f, 2f, 2f);
            g.FillEllipse(rivetBrush, pBottomRightInner.X - 3f, pBottomRightInner.Y - 3f, 2f, 2f);
        }

        // 3. 円形金属ターンテーブル（スチールリング）
        float tr = Width * 0.28f;
        using (var ringBrush = new LinearGradientBrush(
            new PointF(X - tr, Y - tr),
            new PointF(X + tr, Y + tr),
            Color.FromArgb(160, 160, 175),
            Color.FromArgb(70, 70, 85)))
        {
            g.FillEllipse(ringBrush, X - tr, Y - tr, tr * 2, tr * 2);
        }
        using (var ringGroovePen = new Pen(Color.FromArgb(40, 40, 50), 1f))
        {
            g.DrawEllipse(ringGroovePen, X - tr, Y - tr, tr * 2, tr * 2);
        }

        // 4. パルス発光センサードーム（深紅の球体レンズ）
        float sr = Width * 0.20f;
        float sx = X - sr;
        float sy = Y - sr;

        using (var sphereBrush = new LinearGradientBrush(
            new PointF(sx + sr * 0.3f, sy + sr * 0.3f),
            new PointF(sx + sr * 1.7f, sy + sr * 1.7f),
            Color.FromArgb(255, 80, 80),
            Color.FromArgb(110, 5, 10)))
        {
            g.FillEllipse(sphereBrush, sx, sy, sr * 2, sr * 2);
        }

        // パルス発光中心
        float pulse = (float)(Math.Sin(ticks * 0.008) * 0.5 + 0.5);
        int alpha = (int)(180 + pulse * 75);
        using (var pulseHiBrush = new SolidBrush(Color.FromArgb(alpha, 255, 230, 230)))
        {
            g.FillEllipse(pulseHiBrush, X - sr * 0.35f, Y - sr * 0.45f, sr * 0.7f, sr * 0.6f);
        }
    }
}

/// <summary>
/// ステージボス（ガルドロワ母艦）
/// </summary>
public class Boss : Enemy
{
    public int MaxHp { get; } = 50;
    public int Hp { get; set; }
    public new int ShootTimer { get; set; } = 0;
    public new bool ShootNow { get; set; } = false;
    public new float Speed { get; set; } = 1.0f;
    public float Dx { get; set; } = 1.0f;
    public new string State { get; set; } = "ENTER"; // ENTER, HOVER, DEFEATED
    public float EnterTargetY { get; set; } = 120f;
    public int FlashTimer { get; set; } = 0;
    public int DeathTimer { get; set; } = 0;

    private static readonly Random BossRand = new();

    public Boss(float x, float y) : base(x, y, "boss")
    {
        Width = 80f;
        Height = 80f;
        Hp = MaxHp;
    }

    public override void Update()
    {
        if (State == "ENTER")
        {
            if (Y < EnterTargetY)
            {
                Y += Speed;
            }
            else
            {
                State = "HOVER";
            }
        }
        else if (State == "HOVER")
        {
            X += Dx * Speed;
            if (X < 60f || X > 420f)
            {
                Dx *= -1f;
            }

            ShootTimer++;
            if (ShootTimer >= 60) // 1秒間隔で弾幕発射
            {
                ShootTimer = 0;
                ShootNow = true;
            }
        }
        else if (State == "DEFEATED")
        {
            DeathTimer++;
            if (DeathTimer > 90) // 1.5秒間爆発して退場
            {
                MarkedForDeletion = true;
            }
        }

        if (FlashTimer > 0)
        {
            FlashTimer--;
        }
    }

    public override void Draw(Graphics g)
    {
        long ticks = Environment.TickCount64;
        bool isFlashing = FlashTimer > 0 && (FlashTimer / 2) % 2 == 0;

        // 1. 巨大母艦の浮遊ドロップシャドウ（広大な地上への投影影）
        if (State != "DEFEATED")
        {
            float sOffX = 14f;
            float sOffY = 24f;
            PointF[] shadowHex =
            {
                new(X + sOffX, Y + sOffY - 42f),
                new(X + sOffX + 44f, Y + sOffY - 21f),
                new(X + sOffX + 44f, Y + sOffY + 21f),
                new(X + sOffX, Y + sOffY + 42f),
                new(X + sOffX - 44f, Y + sOffY + 21f),
                new(X + sOffX - 44f, Y + sOffY - 21f)
            };
            using var giantShadowBrush = new SolidBrush(Color.FromArgb(95, 0, 0, 0));
            g.FillPolygon(giantShadowBrush, shadowHex);
        }

        // 2. 左右大型シリンダーエンジンポッド（ツインナセル）
        DrawEnginePods(g, ticks, isFlashing);

        // 3. メタリック多層重装甲六角形メインフレーム（3Dベベル装甲）
        DrawHeavyArmorHull(g, isFlashing);

        // 4. 超巨大リアクターコア（多重球体エネルギー炉）
        DrawReactorCore(g, ticks, isFlashing);

        // 5. ボスHPバー（画面上部に表示）
        if (State is "ENTER" or "HOVER")
        {
            // 背景バー
            using (var bgBrush = new SolidBrush(Color.FromArgb(50, 0, 0)))
            {
                g.FillRectangle(bgBrush, 80f, 50f, 320f, 10f);
            }

            // HPゲージ
            float fillWidth = 320f * (Math.Max(0, Hp) / (float)MaxHp);
            using (var hpBrush = new SolidBrush(Color.Red))
            {
                g.FillRectangle(hpBrush, 80f, 50f, fillWidth, 10f);
            }

            // 枠線
            using (var borderPen = new Pen(Color.White, 1f))
            {
                g.DrawRectangle(borderPen, 80f, 50f, 320f, 10f);
            }
        }

        // 6. 撃破時の連続爆発エフェクト
        if (State == "DEFEATED")
        {
            for (int i = 0; i < 3; i++)
            {
                float ex = X + (float)(BossRand.NextDouble() * 70 - 35);
                float ey = Y + (float)(BossRand.NextDouble() * 70 - 35);
                int r = BossRand.Next(10, 25);
                Color expColor = Color.FromArgb(255, BossRand.Next(100, 256), 0);
                using var expBrush = new SolidBrush(expColor);
                g.FillEllipse(expBrush, ex - r, ey - r, r * 2, r * 2);
            }
        }
    }

    private void DrawEnginePods(Graphics g, long ticks, bool isFlashing)
    {
        float[] podX = { X - 42f, X + 30f };
        float podY = Y - 20f;
        float podW = 12f;
        float podH = 40f;

        foreach (float px in podX)
        {
            // エンジン排気ジェット炎（赤橙色のアニメーション）
            if (State != "DEFEATED")
            {
                float flameLen = 8f + (float)(Math.Sin(ticks * 0.08 + px) * 4.0);
                PointF[] flame =
                {
                    new(px + 2f, podY + podH),
                    new(px + podW / 2f, podY + podH + flameLen),
                    new(px + podW - 2f, podY + podH)
                };
                using var flameBrush = new SolidBrush(Color.FromArgb(220, 255, 120, 0));
                g.FillPolygon(flameBrush, flame);
            }

            // シリンダー円筒金属グラデーション（左がハイライト、右がシャドウ）
            Color podLight = isFlashing ? Color.White : Color.FromArgb(140, 50, 50);
            Color podDark = isFlashing ? Color.LightGray : Color.FromArgb(60, 15, 15);

            using (var podBrush = new LinearGradientBrush(
                new PointF(px, podY),
                new PointF(px + podW, podY),
                podLight,
                podDark))
            {
                g.FillRectangle(podBrush, px, podY, podW, podH);
            }

            // 円筒の枠線・ディテールスリット
            using var podPen = new Pen(isFlashing ? Color.White : Color.FromArgb(40, 10, 10), 1f);
            g.DrawRectangle(podPen, px, podY, podW, podH);
            g.DrawLine(podPen, px, podY + 10f, px + podW, podY + 10f);
            g.DrawLine(podPen, px, podY + podH - 8f, px + podW, podY + podH - 8f);
        }
    }

    private void DrawHeavyArmorHull(Graphics g, bool isFlashing)
    {
        PointF top = new(X, Y - 42f);
        PointF topRight = new(X + 42f, Y - 21f);
        PointF bottomRight = new(X + 42f, Y + 21f);
        PointF bottom = new(X, Y + 42f);
        PointF bottomLeft = new(X - 42f, Y + 21f);
        PointF topLeft = new(X - 42f, Y - 21f);
        PointF center = new(X, Y);

        // A. 面分割による立体ベベル装甲（左上からの光源）
        // 1) 左上傾斜面（最高輝度面）
        Color cTopLeft = isFlashing ? Color.White : Color.FromArgb(170, 175, 190);
        using (var brushTopLeft = new SolidBrush(cTopLeft))
        {
            g.FillPolygon(brushTopLeft, new[] { top, center, topLeft });
        }

        // 2) 右上傾斜面（中間輝度）
        Color cTopRight = isFlashing ? Color.White : Color.FromArgb(135, 140, 155);
        using (var brushTopRight = new SolidBrush(cTopRight))
        {
            g.FillPolygon(brushTopRight, new[] { top, topRight, center });
        }

        // 3) 左下傾斜面（中間輝度）
        Color cBottomLeft = isFlashing ? Color.White : Color.FromArgb(120, 125, 140);
        using (var brushBottomLeft = new SolidBrush(cBottomLeft))
        {
            g.FillPolygon(brushBottomLeft, new[] { topLeft, center, bottomLeft });
        }

        // 4) 右側面（シャドウ面）
        Color cRight = isFlashing ? Color.White : Color.FromArgb(90, 95, 105);
        using (var brushRight = new SolidBrush(cRight))
        {
            g.FillPolygon(brushRight, new[] { topRight, bottomRight, center });
        }

        // 5) 底面傾斜面（最暗部シャドウ）
        Color cBottom = isFlashing ? Color.White : Color.FromArgb(70, 75, 85);
        using (var brushBottom = new SolidBrush(cBottom))
        {
            g.FillPolygon(brushBottom, new[] { bottomLeft, center, bottom });
            g.FillPolygon(brushBottom, new[] { center, bottomRight, bottom });
        }

        // 装甲板の継ぎ目ライン（パネルライン）
        using (var panelPen = new Pen(isFlashing ? Color.White : Color.FromArgb(45, 48, 55), 1.2f))
        {
            g.DrawPolygon(panelPen, new[] { top, topRight, bottomRight, bottom, bottomLeft, topLeft });
            g.DrawLine(panelPen, top, center);
            g.DrawLine(panelPen, topLeft, center);
            g.DrawLine(panelPen, bottomLeft, center);
            g.DrawLine(panelPen, bottom, center);
            g.DrawLine(panelPen, bottomRight, center);
            g.DrawLine(panelPen, topRight, center);
        }

        // メタリック外郭エッジハイライト
        using (var edgeHiPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
        {
            g.DrawLine(edgeHiPen, topLeft, top);
            g.DrawLine(edgeHiPen, topLeft, bottomLeft);
        }
    }

    private void DrawReactorCore(Graphics g, long ticks, bool isFlashing)
    {
        // 外郭装甲リング
        float ringR = 21f;
        using (var ringBrush = new LinearGradientBrush(
            new PointF(X - ringR, Y - ringR),
            new PointF(X + ringR, Y + ringR),
            Color.FromArgb(90, 95, 110),
            Color.FromArgb(40, 42, 50)))
        {
            g.FillEllipse(ringBrush, X - ringR, Y - ringR, ringR * 2, ringR * 2);
        }
        using (var ringPen = new Pen(Color.FromArgb(30, 30, 35), 1.5f))
        {
            g.DrawEllipse(ringPen, X - ringR, Y - ringR, ringR * 2, ringR * 2);
        }

        // コア内部球体
        float coreR = 15f;
        float cx = X - coreR;
        float cy = Y - coreR;

        if (State == "DEFEATED")
        {
            using var deadBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
            g.FillEllipse(deadBrush, cx, cy, coreR * 2, coreR * 2);
            return;
        }

        // 脈動エネルギー球（深紅からオレンジ・白への立体グロー）
        Color coreOuter = ((ticks / 200) % 2 == 0) ? Color.FromArgb(200, 20, 0) : Color.FromArgb(255, 60, 0);
        Color coreInner = ((ticks / 200) % 2 == 0) ? Color.FromArgb(255, 160, 20) : Color.FromArgb(255, 220, 50);

        using (var coreBrush = new LinearGradientBrush(
            new PointF(cx + coreR * 0.4f, cy + coreR * 0.4f),
            new PointF(cx + coreR * 1.8f, cy + coreR * 1.8f),
            coreInner,
            coreOuter))
        {
            g.FillEllipse(coreBrush, cx, cy, coreR * 2, coreR * 2);
        }

        // コア中心の高熱特異点ハイライト
        using (var centerHiBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(centerHiBrush, X - 4f, Y - 4f, 8f, 8f);
        }

        // コア外周の遮蔽パネル（4つの爪）
        for (int i = 0; i < 4; i++)
        {
            double angle = (i * Math.PI / 2.0) + Math.PI / 4.0;
            float clawX = (float)(X + Math.Cos(angle) * (coreR + 2f));
            float clawY = (float)(Y + Math.Sin(angle) * (coreR + 2f));
            using var clawBrush = new SolidBrush(Color.FromArgb(50, 55, 65));
            g.FillEllipse(clawBrush, clawX - 2.5f, clawY - 2.5f, 5f, 5f);
        }
    }
}
