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
        using var brush = new SolidBrush(Color);
        g.FillEllipse(brush, X - Width / 2f, Y - Height / 2f, Width, Height);
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
        if (Type == "air")
        {
            // 赤い円盤形機体
            using (var brushRed = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brushRed, X - Width / 2f, Y - Height / 4f, Width, Height / 2f);
            }
            // 緑のセンタースフィア
            using (var brushGreen = new SolidBrush(Color.FromArgb(0, 255, 0)))
            {
                float cr = Width / 4f;
                g.FillEllipse(brushGreen, X - cr, Y - 4f - cr, cr * 2, cr * 2);
            }
        }
        else if (Type == "phaeon")
        {
            float vx = State == "APPROACH" ? 0f : RetreatVx;
            float vy = State == "APPROACH" ? Speed : RetreatVy;

            float speed = (float)Math.Sqrt(vx * vx + vy * vy);
            float dx = speed > 0f ? vx / speed : 0f;
            float dy = speed > 0f ? vy / speed : 1f;

            PointF p1 = new(X + dx * 16f, Y + dy * 16f);
            PointF p2 = new(X - dy * 14f + dx * 4f, Y + dx * 14f + dy * 4f);
            PointF p3 = new(X - dx * 10f, Y - dy * 10f);
            PointF p4 = new(X + dy * 14f + dx * 4f, Y - dx * 14f + dy * 4f);

            using (var brushYellow = new SolidBrush(Color.Yellow))
            {
                g.FillPolygon(brushYellow, new[] { p1, p2, p3, p4 });
            }

            float cx = X + dx * 6f;
            float cy = Y + dy * 6f;
            using (var brushRed = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brushRed, cx - 3f, cy - 3f, 6f, 6f);
            }
        }
        else // "ground"
        {
            using (var brushPurple = new SolidBrush(Color.FromArgb(170, 0, 170)))
            {
                g.FillRectangle(brushPurple, X - Width / 2f, Y - Height / 2f, Width, Height);
            }
            using (var brushRed = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brushRed, X - 8f, Y - 8f, 16f, 16f);
            }
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
        // 1. メタリック六角形装甲
        Color bodyColor = Color.FromArgb(100, 100, 110);
        if (FlashTimer > 0 && (FlashTimer / 2) % 2 == 0)
        {
            bodyColor = Color.White;
        }

        PointF[] hexPoints =
        {
            new(X, Y - 40f),
            new(X + 40f, Y - 20f),
            new(X + 40f, Y + 20f),
            new(X, Y + 40f),
            new(X - 40f, Y + 20f),
            new(X - 40f, Y - 20f)
        };

        using (var brushBody = new SolidBrush(bodyColor))
        {
            g.FillPolygon(brushBody, hexPoints);
        }

        using (var penOutline = new Pen(Color.FromArgb(50, 50, 60), 3f))
        {
            g.DrawPolygon(penOutline, hexPoints);
        }

        // 2. 左右エンジンモジュール
        using (var brushModule = new SolidBrush(Color.FromArgb(80, 0, 0)))
        {
            g.FillRectangle(brushModule, X - 35f, Y - 15f, 10f, 30f);
            g.FillRectangle(brushModule, X + 25f, Y - 15f, 10f, 30f);
        }

        // 3. 中央コア
        long ticks = Environment.TickCount64;
        Color coreColor = Color.Red;
        if (State == "DEFEATED")
        {
            coreColor = Color.FromArgb(120, 120, 120);
        }
        else if ((ticks / 200) % 2 == 0)
        {
            coreColor = Color.FromArgb(255, 100, 0);
        }

        using (var brushCore = new SolidBrush(coreColor))
        {
            g.FillEllipse(brushCore, X - 15f, Y - 15f, 30f, 30f);
        }

        using (var brushCoreCenter = new SolidBrush(Color.White))
        {
            g.FillEllipse(brushCoreCenter, X - 6f, Y - 6f, 12f, 12f);
        }

        // 4. ボスHPバー（画面上部に表示）
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

        // 5. 撃破時の連続爆発エフェクト
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
}
