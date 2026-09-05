using System;
using System.Drawing;

namespace XeviShot.Entities;

/// <summary>
/// ゲームエンティティの基本クラス
/// </summary>
public abstract class Entity
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public bool MarkedForDeletion { get; set; } = false;

    public virtual RectangleF Bounds => new(X - Width / 2f, Y - Height / 2f, Width, Height);

    public abstract void Update();
    public abstract void Draw(Graphics g);
}
