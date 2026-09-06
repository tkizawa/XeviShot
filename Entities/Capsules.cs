using System.Drawing;
using System.Drawing.Drawing2D;

namespace XeviShot.Entities;

/// <summary>
/// アイテムカプセルの共通基底クラス
/// </summary>
public abstract class ItemCapsule : Entity
{
    public float Speed { get; set; } = 1.5f;
    public abstract string Letter { get; }
    public abstract Color CapsuleColor { get; }

    protected ItemCapsule(float x, float y)
    {
        X = x;
        Y = y;
        Width = 18f;
        Height = 18f;
    }

    public override void Update()
    {
        Y += Speed;
        if (Y > 700f)
        {
            MarkedForDeletion = true;
        }
    }

    public override void Draw(Graphics g)
    {
        float r = Width / 2f;

        // 1. 浮遊ドロップシャドウ
        using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, X - r + 3f, Y - r + 5f, Width, Height);
        }

        // 2. カプセル外郭枠（金属リング）
        using (var rimBrush = new LinearGradientBrush(
            new PointF(X - r, Y - r),
            new PointF(X + r, Y + r),
            Color.FromArgb(220, 220, 230),
            Color.FromArgb(80, 80, 95)))
        {
            g.FillEllipse(rimBrush, X - r, Y - r, Width, Height);
        }

        // 3. 球体内部（3Dライティング球体グラデーション）
        float ir = r - 1.5f;
        Color lightCol = Color.FromArgb(
            Math.Min(255, CapsuleColor.R + 60),
            Math.Min(255, CapsuleColor.G + 60),
            Math.Min(255, CapsuleColor.B + 60));
        Color darkCol = Color.FromArgb(
            CapsuleColor.R / 3,
            CapsuleColor.G / 3,
            CapsuleColor.B / 3);

        using (var sphereBrush = new LinearGradientBrush(
            new PointF(X - ir * 0.5f, Y - ir * 0.5f),
            new PointF(X + ir, Y + ir),
            lightCol,
            darkCol))
        {
            g.FillEllipse(sphereBrush, X - ir, Y - ir, ir * 2, ir * 2);
        }

        // 4. ガラス曲面スペキュラハイライト（三日月ハイライト）
        using (var highlightBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
        {
            g.FillEllipse(highlightBrush, X - ir * 0.6f, Y - ir * 0.65f, ir * 0.7f, ir * 0.5f);
        }

        // 5. カプセル中央の文字（ドロップシャドウ付きで視認性と立体感を両立）
        using var font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold);
        var size = g.MeasureString(Letter, font);
        float tx = X - size.Width / 2f + 0.5f;
        float ty = Y - size.Height / 2f + 0.5f;

        using (var textShadow = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
        {
            g.DrawString(Letter, font, textShadow, tx + 1f, ty + 1f);
        }
        using (var textBrush = new SolidBrush(Color.White))
        {
            g.DrawString(Letter, font, textBrush, tx, ty);
        }
    }
}

/// <summary>
/// シールドカプセル（S / 水色）: シールド5回展開
/// </summary>
public class ShieldCapsule : ItemCapsule
{
    public override string Letter => "S";
    public override Color CapsuleColor => Color.FromArgb(0, 200, 255);

    public ShieldCapsule(float x, float y) : base(x, y) { }
}

/// <summary>
/// ウェポンカプセル（W / 赤色）: 通常弾パワーアップ
/// </summary>
public class WeaponCapsule : ItemCapsule
{
    public override string Letter => "W";
    public override Color CapsuleColor => Color.FromArgb(255, 50, 50);

    public WeaponCapsule(float x, float y) : base(x, y) { }
}

/// <summary>
/// レーザーカプセル（L / 緑色）: 貫通レーザー弾へ換装
/// </summary>
public class LaserCapsule : ItemCapsule
{
    public override string Letter => "L";
    public override Color CapsuleColor => Color.FromArgb(50, 255, 50);

    public LaserCapsule(float x, float y) : base(x, y) { }
}
