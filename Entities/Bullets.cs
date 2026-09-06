using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using XeviShot.Audio;

namespace XeviShot.Entities;

/// <summary>
/// 通常の対空弾
/// </summary>
public class Bullet : Entity
{
    public float Vx { get; set; }
    public float Vy { get; set; }
    public Color Color { get; set; } = Color.Yellow;

    public Bullet(float x, float y, float vx = 0f, float vy = -10f)
    {
        X = x;
        Y = y;
        Width = 4f;
        Height = 12f;
        Vx = vx;
        Vy = vy;
    }

    public override void Update()
    {
        X += Vx;
        Y += Vy;
        if (Y < -50f || Y > 690f || X < -50f || X > 530f)
        {
            MarkedForDeletion = true;
        }
    }

    public override void Draw(Graphics g)
    {
        float speed = (float)Math.Sqrt(Vx * Vx + Vy * Vy);
        if (speed > 0f)
        {
            float dx = Vx / speed;
            float dy = Vy / speed;
            float x1 = X - dx * Height / 2f;
            float y1 = Y - dy * Height / 2f;
            float x2 = X + dx * Height / 2f;
            float y2 = Y + dy * Height / 2f;

            using var pen = new Pen(Color, Width);
            g.DrawLine(pen, x1, y1, x2, y2);
        }
        else
        {
            using var brush = new SolidBrush(Color);
            g.FillRectangle(brush, X - Width / 2f, Y - Height / 2f, Width, Height);
        }
    }
}

/// <summary>
/// 空中敵を一網打尽にする貫通レーザー弾
/// </summary>
public class LaserBullet : Entity
{
    public int Level { get; set; } = 1;
    public float Vx { get; set; }
    public float Vy { get; set; }

    public LaserBullet(float x, float y, float vx = 0f, float vy = -18f, int level = 1)
    {
        X = x;
        Y = y;
        Level = Math.Clamp(level, 1, 3);
        Width = Level switch
        {
            1 => 6f,
            2 => 14f,
            3 => 24f,
            _ => 6f
        };
        Height = 60f;
        Vx = vx;
        Vy = vy;
    }

    public override void Update()
    {
        X += Vx;
        Y += Vy;
        if (Y < -50f || Y > 690f || X < -50f || X > 530f)
        {
            MarkedForDeletion = true;
        }
    }

    public override void Draw(Graphics g)
    {
        // 外側の緑の発光
        using (var greenPen = new Pen(Color.FromArgb(0, 220, 0), Width))
        {
            greenPen.StartCap = LineCap.Round;
            greenPen.EndCap = LineCap.Round;
            g.DrawLine(greenPen, X, Y - Height / 2f, X, Y + Height / 2f);
        }

        // 内側の白色コア
        float coreWidth = Level switch
        {
            1 => 2f,
            2 => 5f,
            3 => 9f,
            _ => 2f
        };

        using (var whitePen = new Pen(Color.White, coreWidth))
        {
            whitePen.StartCap = LineCap.Round;
            whitePen.EndCap = LineCap.Round;
            g.DrawLine(whitePen, X, Y - Height / 2f + 3f, X, Y + Height / 2f - 3f);
        }
    }
}

/// <summary>
/// 敵弾をかき消す強力な波動砲・拡散波動砲
/// </summary>
public class WaveCannon : Entity
{
    public float Vx { get; set; }
    public float Vy { get; set; }

    public WaveCannon(float x, float y, float vx = 0f, float vy = -12f)
    {
        X = x;
        Y = y;
        Width = 32f;
        Height = 32f;
        Vx = vx;
        Vy = vy;
    }

    public override void Update()
    {
        X += Vx;
        Y += Vy;
        if (Y < -50f || X < -50f || X > 530f)
        {
            MarkedForDeletion = true;
        }
    }

    public override void Draw(Graphics g)
    {
        long ticks = Environment.TickCount64;
        float pulse = (float)(Math.Sin(ticks * 0.05) * 4.0);

        // 外側の楕円オーラ
        using (var penEllipse = new Pen(Color.FromArgb(0, 255, 255), 2f))
        {
            g.DrawEllipse(penEllipse, X - 30f, Y - 8f, 60f, 16f);
        }

        // 外側の青い波紋円
        float r2 = 22f + pulse;
        using (var penCircle = new Pen(Color.FromArgb(0, 170, 255), 3f))
        {
            g.DrawEllipse(penCircle, X - r2, Y - r2, r2 * 2, r2 * 2);
        }

        // コア白色円
        float r1 = 12f + pulse;
        using (var brushCore = new SolidBrush(Color.White))
        {
            g.FillEllipse(brushCore, X - r1, Y - r1, r1 * 2, r1 * 2);
        }
    }
}

/// <summary>
/// 地上目標へ投下する対地ボム
/// </summary>
public class Bomb : Entity
{
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public float Progress { get; set; } = 0.0f;
    public float Speed { get; set; } = 0.03f;
    public float Size { get; set; } = 10f;
    public bool Exploded { get; set; } = false;
    public int ExplosionTimer { get; set; } = 0;

    public Bomb(float startX, float startY, float targetX, float targetY)
    {
        X = startX;
        Y = startY;
        TargetX = targetX;
        TargetY = targetY;
        Width = 10f;
        Height = 10f;
    }

    public override void Update()
    {
        if (Exploded)
        {
            ExplosionTimer++;
            if (ExplosionTimer > 15)
            {
                MarkedForDeletion = true;
            }
            return;
        }

        Progress += Speed;
        X += (TargetX - X) * 0.1f;
        Y += (TargetY - Y) * Progress;

        if (Progress >= 1.0f)
        {
            Progress = 1.0f;
            if (!Exploded)
            {
                Exploded = true;
                AudioManager.Instance.Play("explosion_ground");
            }
            X = TargetX;
            Y = TargetY;
        }
    }

    public override void Draw(Graphics g)
    {
        if (Exploded)
        {
            float rOuter = 20f + ExplosionTimer * 1.5f;
            using (var outerBrush = new SolidBrush(Color.FromArgb(220, 255, 100, 0)))
            {
                g.FillEllipse(outerBrush, X - rOuter, Y - rOuter, rOuter * 2, rOuter * 2);
            }

            float rMid = 12f + ExplosionTimer;
            using (var midBrush = new SolidBrush(Color.FromArgb(240, 255, 200, 0)))
            {
                g.FillEllipse(midBrush, X - rMid, Y - rMid, rMid * 2, rMid * 2);
            }

            float rInner = 6f + ExplosionTimer * 0.4f;
            using (var innerBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(innerBrush, X - rInner, Y - rInner, rInner * 2, rInner * 2);
            }
        }
        else
        {
            // 投下高度の立体表現: 空中高くから地面に向かって影が収束する3Dパララックス
            float altitude = 1.0f - Progress; // 1.0(高空) -> 0.0(着弾)
            float shadowDistX = 10f * altitude;
            float shadowDistY = 16f * altitude;
            float shadowScale = 0.5f + Progress * 0.5f;

            float currentSize = Math.Max(2f, Size * (1f - Progress * 0.4f));

            // 地面に落ちる影
            using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(110 * Progress + 30), 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush,
                    X + shadowDistX - currentSize * shadowScale,
                    Y + shadowDistY - currentSize * shadowScale * 0.6f,
                    currentSize * shadowScale * 2f,
                    currentSize * shadowScale * 1.2f);
            }

            // ボム本体（立体球体グラデーション）
            using (var bombBrush = new LinearGradientBrush(
                new PointF(X - currentSize * 0.5f, Y - currentSize * 0.5f),
                new PointF(X + currentSize, Y + currentSize),
                Color.FromArgb(255, 120, 255),
                Color.FromArgb(120, 0, 140)))
            {
                g.FillEllipse(bombBrush, X - currentSize, Y - currentSize, currentSize * 2, currentSize * 2);
            }

            // 球体ハイライト
            using (var hiBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(hiBrush, X - currentSize * 0.45f, Y - currentSize * 0.45f, currentSize * 0.6f, currentSize * 0.5f);
            }
        }
    }
}
