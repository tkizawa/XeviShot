using System;
using System.Collections.Generic;
using System.Drawing;

namespace XeviShot.Entities;

/// <summary>
/// 縦スクロール背景（深緑の地上、森林ブロック、蛇行する青い川）
/// </summary>
public class Background
{
    private class ForestBlock
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
    }

    public float Width { get; }
    public float Height { get; }
    public float ScrollY { get; private set; } = 0f;
    public float Speed { get; set; } = 1f;

    private readonly List<ForestBlock> _blocks = new();
    private readonly float[] _riverPoints = new float[20];
    private static readonly Random Rand = new(12345);

    public Background(float width, float height)
    {
        Width = width;
        Height = height;

        // 森林・地形ブロック生成
        for (int i = 0; i < 20; i++)
        {
            _blocks.Add(new ForestBlock
            {
                X = (float)(Rand.NextDouble() * width),
                Y = (float)(Rand.NextDouble() * (height * 2) - height),
                Size = (float)(Rand.NextDouble() * 40 + 20),
                Color = Rand.NextDouble() > 0.5 ? Color.FromArgb(0, 51, 0) : Color.FromArgb(0, 68, 0)
            });
        }

        // 川の蛇行カーブポイント
        for (int i = 0; i < 20; i++)
        {
            _riverPoints[i] = (float)(Math.Sin(i * 0.5) * 40.0 + width / 2f);
        }
    }

    public void Update()
    {
        ScrollY += Speed;
        if (ScrollY > Height)
        {
            ScrollY = 0f;
        }

        foreach (var block in _blocks)
        {
            block.Y += Speed;
            if (block.Y > Height)
            {
                block.Y = -block.Size;
                block.X = (float)(Rand.NextDouble() * Width);
            }
        }
    }

    public void Draw(Graphics g)
    {
        // 1. 地上の基底色（深い緑）
        using (var baseBrush = new SolidBrush(Color.FromArgb(0, 34, 0)))
        {
            g.FillRectangle(baseBrush, 0f, 0f, Width, Height);
        }

        // 2. 森林ブロック描画
        foreach (var block in _blocks)
        {
            using var brush = new SolidBrush(block.Color);
            g.FillRectangle(brush, block.X, block.Y, block.Size, block.Size);
            g.FillRectangle(brush, block.X, block.Y - Height, block.Size, block.Size);
        }

        // 3. 蛇行する川
        var points = new PointF[20];
        float stepH = Height / 10f;
        for (int i = 0; i < 20; i++)
        {
            float ry = i * stepH + (ScrollY % stepH);
            points[i] = new PointF(_riverPoints[i], ry);
        }

        using var riverPen = new Pen(Color.FromArgb(0, 0, 170), 30f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        g.DrawCurve(riverPen, points);
    }
}
