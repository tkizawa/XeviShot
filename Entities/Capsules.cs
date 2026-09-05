using System.Drawing;

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
        Width = 16f;
        Height = 16f;
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
        // カプセル外枠
        using (var brush = new SolidBrush(CapsuleColor))
        {
            g.FillEllipse(brush, X - Width / 2f, Y - Height / 2f, Width, Height);
        }

        // 光沢ハイライト
        using (var highlightBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(highlightBrush, X - 3f, Y - 3f, 4f, 4f);
        }

        // カプセル中央の文字
        using var font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.Black);
        var size = g.MeasureString(Letter, font);
        g.DrawString(Letter, font, textBrush, X - size.Width / 2f + 1f, Y - size.Height / 2f + 1f);
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
