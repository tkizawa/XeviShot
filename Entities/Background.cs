using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace XeviShot.Entities;

/// <summary>
/// 背景のステージテーマ
/// </summary>
public enum BackgroundTheme
{
    ForestAndRiver, // 0〜1分 (0〜3600フレーム): 森と川
    City,           // 1分〜2分 (3600〜7200フレーム): 街
    Outpost,        // 2分〜2分50秒 (7200〜10200フレーム): 要塞前哨基地・進入路
    Fortress        // 2分50秒以降 (10200フレーム〜): 敵要塞
}

/// <summary>
/// 背景オブジェクトの種別
/// </summary>
public enum BackgroundElementType
{
    // 森
    ForestCluster,      // 樹冠が重なり合うリアルな森林
    FarmlandPatch,      // 畑・草地のパッチワーク
    ForestRock,         // 露出した岩場・小山

    // 街
    CityBuilding3D,     // 俯瞰立体ビル（屋上設備、窓、壁面、影）
    CityRoadGrid,       // 道路網（車線、横断歩道、街路樹）
    CityWarehouse,      // 工業団地・低層倉庫

    // 前哨基地
    OutpostRunway,      // 軍用滑走路（誘導灯、標識）
    OutpostBunker,      // アーチ型耐爆格納庫（コーションストライプ）
    OutpostFuelTank,    // 球形燃料タンク・トラス構造

    // 敵要塞
    FortressHexArmor,   // ベベル装甲プレート・ハニカムパネル
    FortressPlasmaCore, // 脈動するエネルギー発電コア
    FortressHeatVent    // 排熱ルーバー・サイバーサーキット
}

/// <summary>
/// 経過時間に応じてリアルな世界が広がる縦スクロール背景
/// （0〜1分: 森林・河川・田園 / 1〜2分: リアル3D都市・道路網 / 2〜2分50秒: 軍事前哨基地・滑走路 / 2分50秒〜: 暗黒敵要塞・サイバープラズマ）
/// </summary>
public class Background
{
    public class BackgroundElement
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float WallHeight { get; set; } // ビルや構造物の側面の立体高さ
        public BackgroundTheme Theme { get; set; }
        public BackgroundElementType ElementType { get; set; }

        public Color BaseColor { get; set; }
        public Color ShadeColor { get; set; }
        public Color AccentColor { get; set; }

        public int Seed { get; set; }
        public int RoofFeatureType { get; set; } // 0: ヘリポート, 1: 給水塔・室外機, 2: ソーラー・通信塔
        public float[] SubOffsets { get; set; } = Array.Empty<float>();
    }

    public float Width { get; }
    public float Height { get; }
    public float ScrollY { get; private set; } = 0f;
    public float Speed { get; set; } = 1.0f;

    public int CurrentFrameCount { get; private set; } = 0;
    public BackgroundTheme CurrentTheme => GetTheme(CurrentFrameCount);

    private readonly List<BackgroundElement> _elements = new();
    private readonly float[] _riverPoints = new float[20];
    private static readonly Random Rand = new(42135);

    public const int ThemeCityStartFrame = 3600;       // 1分 (60秒)
    public const int ThemeOutpostStartFrame = 7200;    // 2分 (120秒)
    public const int ThemeFortressStartFrame = 10200;  // 2分50秒 (170秒)

    public Background(float width, float height)
    {
        Width = width;
        Height = height;

        // 川・中央ルートの滑らかな蛇行スプライン
        for (int i = 0; i < 20; i++)
        {
            _riverPoints[i] = (float)(Math.Sin(i * 0.45) * 55.0 + width / 2f);
        }

        // 初期オブジェクト配置（開始時は森エリア）
        for (int i = 0; i < 32; i++)
        {
            var elem = new BackgroundElement();
            InitElement(elem, BackgroundTheme.ForestAndRiver);
            elem.X = (float)(Rand.NextDouble() * (width - elem.Width));
            elem.Y = (float)(Rand.NextDouble() * (height * 1.6f) - height * 0.4f);
            _elements.Add(elem);
        }
    }

    /// <summary>
    /// フレーム数から現在の背景テーマを取得
    /// </summary>
    public static BackgroundTheme GetTheme(int frameCount)
    {
        if (frameCount < ThemeCityStartFrame) return BackgroundTheme.ForestAndRiver;
        if (frameCount < ThemeOutpostStartFrame) return BackgroundTheme.City;
        if (frameCount < ThemeFortressStartFrame) return BackgroundTheme.Outpost;
        return BackgroundTheme.Fortress;
    }

    /// <summary>
    /// テーマに応じた要素の初期化（パラメータ・ディテール生成）
    /// </summary>
    private static void InitElement(BackgroundElement elem, BackgroundTheme theme)
    {
        elem.Theme = theme;
        elem.Seed = Rand.Next(100000);

        switch (theme)
        {
            case BackgroundTheme.ForestAndRiver:
                int fType = Rand.Next(10);
                if (fType < 6)
                {
                    // 森林クラスタ（複数の木々が密集した有機的キャノピー）
                    elem.ElementType = BackgroundElementType.ForestCluster;
                    elem.Width = (float)(Rand.NextDouble() * 50 + 40);
                    elem.Height = (float)(Rand.NextDouble() * 50 + 40);
                    elem.BaseColor = Color.FromArgb(16, 56, 20);
                    elem.ShadeColor = Color.FromArgb(8, 36, 12);
                    elem.AccentColor = Color.FromArgb(32, 90, 36);

                    // クラスタ内の小樹木オフセット
                    int subCount = Rand.Next(5, 9);
                    elem.SubOffsets = new float[subCount * 3]; // dx, dy, radius
                    for (int i = 0; i < subCount; i++)
                    {
                        elem.SubOffsets[i * 3 + 0] = (float)(Rand.NextDouble() * (elem.Width * 0.7f) + elem.Width * 0.15f);
                        elem.SubOffsets[i * 3 + 1] = (float)(Rand.NextDouble() * (elem.Height * 0.7f) + elem.Height * 0.15f);
                        elem.SubOffsets[i * 3 + 2] = (float)(Rand.NextDouble() * 12 + 10);
                    }
                }
                else if (fType < 9)
                {
                    // 田園・畑・草原のパッチワーク
                    elem.ElementType = BackgroundElementType.FarmlandPatch;
                    elem.Width = (float)(Rand.NextDouble() * 40 + 50);
                    elem.Height = (float)(Rand.NextDouble() * 30 + 40);
                    int greenTone = Rand.Next(45, 80);
                    elem.BaseColor = Color.FromArgb(greenTone - 15, greenTone, greenTone - 20);
                    elem.AccentColor = Color.FromArgb(greenTone - 5, greenTone + 15, greenTone - 10);
                }
                else
                {
                    // 露出した岩場
                    elem.ElementType = BackgroundElementType.ForestRock;
                    elem.Width = (float)(Rand.NextDouble() * 25 + 25);
                    elem.Height = (float)(Rand.NextDouble() * 20 + 20);
                    elem.BaseColor = Color.FromArgb(55, 60, 52);
                    elem.AccentColor = Color.FromArgb(85, 90, 80);
                }
                break;

            case BackgroundTheme.City:
                int cType = Rand.Next(10);
                if (cType < 7)
                {
                    // リアルな立体俯瞰ビル（屋上設備、窓、壁面、ドロップシャドウ）
                    elem.ElementType = BackgroundElementType.CityBuilding3D;
                    elem.Width = (float)(Rand.NextDouble() * 40 + 45);
                    elem.Height = (float)(Rand.NextDouble() * 50 + 45);
                    elem.WallHeight = (float)(Rand.NextDouble() * 18 + 14); // 手前に見えるビルの壁面の高さ
                    elem.RoofFeatureType = Rand.Next(3); // 0: ヘリポート, 1: 給水塔・室外機, 2: ソーラーパネル・機械室

                    int tone = Rand.Next(46, 78);
                    elem.BaseColor = Color.FromArgb(tone, tone + 4, tone + 10); // 屋上面
                    elem.ShadeColor = Color.FromArgb((int)(tone * 0.65f), (int)((tone + 4) * 0.65f), (int)((tone + 10) * 0.65f)); // 壁面（影側）
                    elem.AccentColor = Color.FromArgb(tone + 35, tone + 40, tone + 55); // パラペット（縁）
                }
                else if (cType < 9)
                {
                    // 道路・交差点グリッド（アスファルト、車線、横断歩道、並木）
                    elem.ElementType = BackgroundElementType.CityRoadGrid;
                    elem.Width = (float)(Rand.NextDouble() * 30 + 70);
                    elem.Height = (float)(Rand.NextDouble() * 25 + 50);
                    elem.BaseColor = Color.FromArgb(32, 34, 40);
                    elem.AccentColor = Color.FromArgb(240, 210, 60); // センターライン
                }
                else
                {
                    // 低層倉庫・商業コンプレックス
                    elem.ElementType = BackgroundElementType.CityWarehouse;
                    elem.Width = (float)(Rand.NextDouble() * 40 + 60);
                    elem.Height = (float)(Rand.NextDouble() * 25 + 35);
                    elem.WallHeight = 10f;
                    elem.BaseColor = Color.FromArgb(70, 75, 85);
                    elem.ShadeColor = Color.FromArgb(45, 50, 58);
                    elem.AccentColor = Color.FromArgb(100, 110, 125);
                }
                break;

            case BackgroundTheme.Outpost:
                int oType = Rand.Next(10);
                if (oType < 5)
                {
                    // 軍用滑走路・誘導路
                    elem.ElementType = BackgroundElementType.OutpostRunway;
                    elem.Width = (float)(Rand.NextDouble() * 25 + 45);
                    elem.Height = (float)(Rand.NextDouble() * 40 + 80);
                    elem.BaseColor = Color.FromArgb(26, 28, 34);
                    elem.AccentColor = Color.FromArgb(235, 235, 245);
                }
                else if (oType < 8)
                {
                    // アーチ型耐爆格納庫・バンカー
                    elem.ElementType = BackgroundElementType.OutpostBunker;
                    elem.Width = (float)(Rand.NextDouble() * 35 + 45);
                    elem.Height = (float)(Rand.NextDouble() * 30 + 35);
                    elem.WallHeight = 14f;
                    elem.BaseColor = Color.FromArgb(42, 46, 54);
                    elem.ShadeColor = Color.FromArgb(25, 28, 35);
                    elem.AccentColor = Color.FromArgb(240, 180, 0); // 警戒黄
                }
                else
                {
                    // 球形燃料タンク・パイプライン集合部
                    elem.ElementType = BackgroundElementType.OutpostFuelTank;
                    elem.Width = (float)(Rand.NextDouble() * 30 + 40);
                    elem.Height = (float)(Rand.NextDouble() * 30 + 40);
                    elem.BaseColor = Color.FromArgb(60, 65, 75);
                    elem.AccentColor = Color.FromArgb(130, 140, 155);
                }
                break;

            case BackgroundTheme.Fortress:
            default:
                int foType = Rand.Next(10);
                if (foType < 5)
                {
                    // ベベル装甲プレート・六角形ハニカム構造
                    elem.ElementType = BackgroundElementType.FortressHexArmor;
                    elem.Width = (float)(Rand.NextDouble() * 40 + 45);
                    elem.Height = (float)(Rand.NextDouble() * 40 + 45);
                    elem.WallHeight = 12f;
                    elem.BaseColor = Color.FromArgb(18, 20, 28);
                    elem.ShadeColor = Color.FromArgb(10, 11, 16);
                    elem.AccentColor = Color.FromArgb(50, 58, 75);
                }
                else if (foType < 8)
                {
                    // 脈動するエネルギー発電コア
                    elem.ElementType = BackgroundElementType.FortressPlasmaCore;
                    elem.Width = (float)(Rand.NextDouble() * 30 + 40);
                    elem.Height = (float)(Rand.NextDouble() * 30 + 40);
                    elem.BaseColor = Color.FromArgb(14, 15, 22);
                    elem.AccentColor = Rand.NextDouble() > 0.5
                        ? Color.FromArgb(0, 240, 255)   // ネオンシアン
                        : Color.FromArgb(255, 40, 130); // ネオンマゼンタ
                }
                else
                {
                    // 排熱ルーバー・高エネルギー回路
                    elem.ElementType = BackgroundElementType.FortressHeatVent;
                    elem.Width = (float)(Rand.NextDouble() * 35 + 40);
                    elem.Height = (float)(Rand.NextDouble() * 30 + 35);
                    elem.BaseColor = Color.FromArgb(22, 24, 32);
                    elem.AccentColor = Color.FromArgb(255, 120, 20); // 灼熱オレンジ
                }
                break;
        }
    }

    /// <summary>
    /// 背景スクロールとオブジェクトの更新
    /// </summary>
    public void Update(int frameCount)
    {
        CurrentFrameCount = frameCount;

        ScrollY += Speed;
        if (ScrollY > Height)
        {
            ScrollY = 0f;
        }

        var currentTheme = CurrentTheme;

        foreach (var elem in _elements)
        {
            elem.Y += Speed;
            if (elem.Y > Height + 80f)
            {
                // 画面下部に消えたら上部からリスポーン
                InitElement(elem, currentTheme);
                elem.Y = -elem.Height - elem.WallHeight - (float)(Rand.NextDouble() * 70 + 10);
                elem.X = (float)(Rand.NextDouble() * (Width - elem.Width));
            }
        }
    }

    /// <summary>
    /// 経過時間に応じた地表ベースカラーの滑らかな補間
    /// </summary>
    public static Color GetGroundBaseColor(int frameCount)
    {
        Color forestCol = Color.FromArgb(14, 34, 16);   // 深い大地・苔緑
        Color cityCol = Color.FromArgb(40, 42, 48);     // 都市アスファルト
        Color outpostCol = Color.FromArgb(26, 28, 34);  // 基地コンクリートスラブ
        Color fortressCol = Color.FromArgb(10, 11, 16); // 巨大要塞ダークスチール

        const int transitionDuration = 180; // 3秒間かけて色を滑らかにフェード

        if (frameCount < ThemeCityStartFrame - transitionDuration) return forestCol;
        if (frameCount < ThemeCityStartFrame)
        {
            float t = (frameCount - (ThemeCityStartFrame - transitionDuration)) / (float)transitionDuration;
            return LerpColor(forestCol, cityCol, t);
        }
        if (frameCount < ThemeOutpostStartFrame - transitionDuration) return cityCol;
        if (frameCount < ThemeOutpostStartFrame)
        {
            float t = (frameCount - (ThemeOutpostStartFrame - transitionDuration)) / (float)transitionDuration;
            return LerpColor(cityCol, outpostCol, t);
        }
        if (frameCount < ThemeFortressStartFrame - transitionDuration) return outpostCol;
        if (frameCount < ThemeFortressStartFrame)
        {
            float t = (frameCount - (ThemeFortressStartFrame - transitionDuration)) / (float)transitionDuration;
            return LerpColor(outpostCol, fortressCol, t);
        }
        return fortressCol;
    }

    private static Color LerpColor(Color c1, Color c2, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        int r = (int)(c1.R + (c2.R - c1.R) * t);
        int g = (int)(c1.G + (c2.G - c1.G) * t);
        int b = (int)(c1.B + (c2.B - c1.B) * t);
        return Color.FromArgb(r, g, b);
    }

    /// <summary>
    /// 背景全体の描画
    /// </summary>
    public void Draw(Graphics g)
    {
        // 1. 地上の基底色
        Color groundCol = GetGroundBaseColor(CurrentFrameCount);
        using (var baseBrush = new SolidBrush(groundCol))
        {
            g.FillRectangle(baseBrush, 0f, 0f, Width, Height);
        }

        // 2. 川・水路・プラズマ導管（下層として描画）
        DrawRiverOrConduit(g);

        // 3. 背景オブジェクト群（立体ドロップシャドウ、建築物、森林）
        foreach (var elem in _elements)
        {
            DrawElement(g, elem);
        }

        // 4. 水上にかかる橋・交差路（上層として水路の上に架橋）
        DrawOverpasses(g);
    }

    /// <summary>
    /// 各要素の描画ディスパッチ
    /// </summary>
    private static void DrawElement(Graphics g, BackgroundElement elem)
    {
        switch (elem.ElementType)
        {
            // 森エリア
            case BackgroundElementType.ForestCluster:
                DrawForestCluster(g, elem);
                break;
            case BackgroundElementType.FarmlandPatch:
                DrawFarmlandPatch(g, elem);
                break;
            case BackgroundElementType.ForestRock:
                DrawForestRock(g, elem);
                break;

            // 街エリア
            case BackgroundElementType.CityBuilding3D:
                DrawCityBuilding3D(g, elem);
                break;
            case BackgroundElementType.CityRoadGrid:
                DrawCityRoadGrid(g, elem);
                break;
            case BackgroundElementType.CityWarehouse:
                DrawCityWarehouse(g, elem);
                break;

            // 前哨基地エリア
            case BackgroundElementType.OutpostRunway:
                DrawOutpostRunway(g, elem);
                break;
            case BackgroundElementType.OutpostBunker:
                DrawOutpostBunker(g, elem);
                break;
            case BackgroundElementType.OutpostFuelTank:
                DrawOutpostFuelTank(g, elem);
                break;

            // 敵要塞エリア
            case BackgroundElementType.FortressHexArmor:
                DrawFortressHexArmor(g, elem);
                break;
            case BackgroundElementType.FortressPlasmaCore:
                DrawFortressPlasmaCore(g, elem);
                break;
            case BackgroundElementType.FortressHeatVent:
            default:
                DrawFortressHeatVent(g, elem);
                break;
        }
    }

    #region 森エリアのリアル描画

    /// <summary>
    /// 森林クラスタ: 樹冠が重なり合う自然な木々の集合体（立体ライティングと木漏れ日）
    /// </summary>
    private static void DrawForestCluster(Graphics g, BackgroundElement elem)
    {
        if (elem.SubOffsets.Length < 3) return;

        // 1. 地面に落ちる柔らかな森林シャドウ（右下方向）
        using (var shadowBrush = new SolidBrush(Color.FromArgb(65, 0, 0, 0)))
        {
            for (int i = 0; i < elem.SubOffsets.Length / 3; i++)
            {
                float cx = elem.X + elem.SubOffsets[i * 3 + 0];
                float cy = elem.Y + elem.SubOffsets[i * 3 + 1];
                float r = elem.SubOffsets[i * 3 + 2];
                g.FillEllipse(shadowBrush, cx - r + 5f, cy - r + 8f, r * 2f, r * 2f);
            }
        }

        // 2. 樹木の下層（陰影ベース・ディープグリーン）
        using (var shadeBrush = new SolidBrush(elem.ShadeColor))
        {
            for (int i = 0; i < elem.SubOffsets.Length / 3; i++)
            {
                float cx = elem.X + elem.SubOffsets[i * 3 + 0];
                float cy = elem.Y + elem.SubOffsets[i * 3 + 1];
                float r = elem.SubOffsets[i * 3 + 2];
                g.FillEllipse(shadeBrush, cx - r, cy - r, r * 2f, r * 2f);
            }
        }

        // 3. 樹冠の中層（本体グリーン）
        using (var bodyBrush = new SolidBrush(elem.BaseColor))
        {
            for (int i = 0; i < elem.SubOffsets.Length / 3; i++)
            {
                float cx = elem.X + elem.SubOffsets[i * 3 + 0];
                float cy = elem.Y + elem.SubOffsets[i * 3 + 1];
                float r = elem.SubOffsets[i * 3 + 2] - 2f;
                if (r > 2f)
                {
                    g.FillEllipse(bodyBrush, cx - r - 1f, cy - r - 2f, r * 2f, r * 2f);
                }
            }
        }

        // 4. 木漏れ日ハイライト（左上からのライティング）
        using (var hiBrush = new SolidBrush(elem.AccentColor))
        {
            for (int i = 0; i < elem.SubOffsets.Length / 3; i++)
            {
                float cx = elem.X + elem.SubOffsets[i * 3 + 0];
                float cy = elem.Y + elem.SubOffsets[i * 3 + 1];
                float r = elem.SubOffsets[i * 3 + 2] * 0.55f;
                if (r > 2f)
                {
                    g.FillEllipse(hiBrush, cx - r - 3f, cy - r - 4f, r * 2f, r * 1.8f);
                }
            }
        }
    }

    /// <summary>
    /// 田園・畑のパッチワーク（あぜ道・畝テクスチャ）
    /// </summary>
    private static void DrawFarmlandPatch(Graphics g, BackgroundElement elem)
    {
        // 畑のベース
        using (var patchBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(patchBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }

        // あぜ道・区画線
        using var ridgePen = new Pen(elem.AccentColor, 1.5f);
        g.DrawRectangle(ridgePen, elem.X, elem.Y, elem.Width, elem.Height);

        // 畝（うね）の規則的なライン
        float rowH = 7f;
        for (float ry = elem.Y + rowH; ry < elem.Y + elem.Height - 3f; ry += rowH)
        {
            g.DrawLine(ridgePen, elem.X + 3f, ry, elem.X + elem.Width - 3f, ry);
        }
    }

    /// <summary>
    /// 露出した岩場（立体陰影）
    /// </summary>
    private static void DrawForestRock(Graphics g, BackgroundElement elem)
    {
        using (var shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, elem.X + 3f, elem.Y + 4f, elem.Width, elem.Height);
        }
        using (var rockBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillEllipse(rockBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }
        using (var hiBrush = new SolidBrush(elem.AccentColor))
        {
            g.FillEllipse(hiBrush, elem.X + 3f, elem.Y + 2f, elem.Width * 0.5f, elem.Height * 0.45f);
        }
    }

    #endregion

    #region 街エリアのリアル3D描画

    /// <summary>
    /// リアルな立体俯瞰ビルディング
    /// （右下へ伸びる長いドロップシャドウ、手前の南側壁面＋窓、屋上のリアルな設備群）
    /// </summary>
    private static void DrawCityBuilding3D(Graphics g, BackgroundElement elem)
    {
        float x = elem.X;
        float y = elem.Y;
        float w = elem.Width;
        float h = elem.Height;
        float wallH = elem.WallHeight;

        // 1. 地面に落ちるリアルな立体ドロップシャドウ（光が左上から差すため右下へ）
        PointF[] shadowPoly =
        {
            new(x + w, y + 10f),
            new(x + w + 16f, y + 10f + wallH + 12f),
            new(x + 16f, y + h + wallH + 12f),
            new(x, y + h + wallH),
            new(x + w, y + h + wallH)
        };
        using (var shadowBrush = new SolidBrush(Color.FromArgb(85, 0, 0, 0)))
        {
            g.FillPolygon(shadowBrush, shadowPoly);
        }

        // 2. 南側の壁面（手前を向いたファサード）
        using (var wallBrush = new SolidBrush(elem.ShadeColor))
        {
            g.FillRectangle(wallBrush, x, y + h, w, wallH);
        }

        // 南壁面の階層窓（整然と並ぶオフィス窓）
        int wallFloors = Math.Max(1, (int)(wallH / 6f));
        int wallCols = Math.Max(2, (int)(w / 8f));
        int seed = elem.Seed;

        for (int f = 0; f < wallFloors; f++)
        {
            for (int c = 0; c < wallCols; c++)
            {
                seed = (seed * 1103515245 + 12345) & 0x7fffffff;
                bool isLit = (seed % 10) < 4;
                if (isLit)
                {
                    Color winCol = (seed % 2 == 0)
                        ? Color.FromArgb(255, 230, 130)  // 温かい白熱窓
                        : Color.FromArgb(130, 210, 255); // オフィス蛍光灯
                    using var winBrush = new SolidBrush(winCol);
                    g.FillRectangle(winBrush, x + c * 8f + 2f, y + h + f * 6f + 1.5f, 4.5f, 3f);
                }
            }
        }

        // 壁面底部の接地シャドウライン
        using (var groundPen = new Pen(Color.FromArgb(120, 0, 0, 0), 1.5f))
        {
            g.DrawLine(groundPen, x, y + h + wallH, x + w, y + h + wallH);
        }

        // 3. 屋上面（天面）
        using (var roofBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(roofBrush, x, y, w, h);
        }

        // 屋上周囲のパラペット（手すり・立ち上がり縁石）
        using (var parapetPen = new Pen(elem.AccentColor, 1.5f))
        {
            g.DrawRectangle(parapetPen, x, y, w, h);
        }

        // 4. 屋上の超リアルな設備ディテール
        float cx = x + w / 2f;
        float cy = y + h / 2f;

        if (elem.RoofFeatureType == 0 && w >= 40f && h >= 40f)
        {
            // タイプA: ヘリポート（着陸サークル、H文字、4隅マーカー）
            using var heliPen = new Pen(Color.FromArgb(240, 215, 50), 2f);
            g.DrawEllipse(heliPen, cx - 11f, cy - 11f, 22f, 22f);

            using var font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.FromArgb(240, 215, 50));
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("H", font, textBrush, cx, cy, sf);

            // 四隅の誘導マーカー
            using var markBrush = new SolidBrush(Color.FromArgb(220, 50, 50));
            g.FillRectangle(markBrush, x + 3f, y + 3f, 3f, 3f);
            g.FillRectangle(markBrush, x + w - 6f, y + 3f, 3f, 3f);
            g.FillRectangle(markBrush, x + 3f, y + h - 6f, 3f, 3f);
            g.FillRectangle(markBrush, x + w - 6f, y + h - 6f, 3f, 3f);
        }
        else if (elem.RoofFeatureType == 1)
        {
            // タイプB: 給水塔（立体円筒＋脚）＆ 大型室外機コンプレッサー
            // 給水塔シャドウ
            using (var sBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillEllipse(sBrush, x + 8f, y + 8f, 13f, 13f);
            }
            // 給水塔本体
            using (var tankBrush = new LinearGradientBrush(
                new PointF(x + 5f, y + 5f),
                new PointF(x + 16f, y + 16f),
                Color.FromArgb(210, 215, 225),
                Color.FromArgb(100, 105, 115)))
            {
                g.FillEllipse(tankBrush, x + 5f, y + 5f, 11f, 11f);
            }
            using (var topBrush = new SolidBrush(Color.FromArgb(240, 245, 255)))
            {
                g.FillEllipse(topBrush, x + 7f, y + 6f, 7f, 7f);
            }

            // エアコン室外機ユニット
            using (var acBrush = new SolidBrush(Color.FromArgb(90, 95, 105)))
            {
                g.FillRectangle(acBrush, x + w - 18f, y + 8f, 12f, 8f);
                g.FillRectangle(acBrush, x + w - 18f, y + 20f, 12f, 8f);
            }
            using (var acPen = new Pen(Color.FromArgb(130, 135, 145), 1f))
            {
                g.DrawRectangle(acPen, x + w - 18f, y + 8f, 12f, 8f);
                g.DrawRectangle(acPen, x + w - 18f, y + 20f, 12f, 8f);
            }
        }
        else
        {
            // タイプC: エレベーター機械室（ペントハウス）＆ ソーラーパネル群
            using (var phShadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillRectangle(phShadow, x + 8f, y + 8f, 16f, 14f);
            }
            using (var phBrush = new SolidBrush(Color.FromArgb(85, 90, 100)))
            {
                g.FillRectangle(phBrush, x + 6f, y + 6f, 16f, 14f);
            }
            using (var phPen = new Pen(Color.FromArgb(130, 135, 145), 1f))
            {
                g.DrawRectangle(phPen, x + 6f, y + 6f, 16f, 14f);
            }

            // ソーラーパネル（ダークネイビーブルーの反射面）
            using var solarBrush = new SolidBrush(Color.FromArgb(25, 45, 80));
            using var solarPen = new Pen(Color.FromArgb(60, 90, 140), 1f);
            float sx = x + w - 20f;
            float sy = y + 8f;
            for (int r = 0; r < 2; r++)
            {
                g.FillRectangle(solarBrush, sx, sy + r * 10f, 14f, 7f);
                g.DrawRectangle(solarPen, sx, sy + r * 10f, 14f, 7f);
            }
        }
    }

    /// <summary>
    /// 道路網・交差点グリッド（車線、横断歩道ゼブラ、並木）
    /// </summary>
    private static void DrawCityRoadGrid(Graphics g, BackgroundElement elem)
    {
        // 道路アスファルト
        using (var roadBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(roadBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }

        // 歩道ブロック（両脇の境界線）
        using var curbPen = new Pen(Color.FromArgb(70, 75, 85), 2f);
        g.DrawLine(curbPen, elem.X, elem.Y, elem.X, elem.Y + elem.Height);
        g.DrawLine(curbPen, elem.X + elem.Width, elem.Y, elem.X + elem.Width, elem.Y + elem.Height);

        // 中央車線（黄色センターライン）
        using var yellowPen = new Pen(elem.AccentColor, 2f);
        float midX = elem.X + elem.Width / 2f;
        g.DrawLine(yellowPen, midX, elem.Y, midX, elem.Y + elem.Height);

        // 横断歩道（ゼブラゾーン）
        using var zebraBrush = new SolidBrush(Color.FromArgb(230, 230, 240));
        float zy = elem.Y + elem.Height * 0.4f;
        for (float zx = elem.X + 4f; zx < elem.X + elem.Width - 6f; zx += 8f)
        {
            g.FillRectangle(zebraBrush, zx, zy, 4.5f, 10f);
        }

        // 街路樹（道路沿いに並ぶ並木）
        using var treeShadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
        using var treeBrush = new SolidBrush(Color.FromArgb(30, 95, 40));
        using var treeHi = new SolidBrush(Color.FromArgb(50, 140, 60));

        for (float ty = elem.Y + 6f; ty < elem.Y + elem.Height - 6f; ty += 18f)
        {
            // 左側並木
            g.FillEllipse(treeShadow, elem.X + 2f, ty + 2f, 7f, 7f);
            g.FillEllipse(treeBrush, elem.X + 1f, ty, 7f, 7f);
            g.FillEllipse(treeHi, elem.X + 2f, ty, 3.5f, 3.5f);

            // 右側並木
            g.FillEllipse(treeShadow, elem.X + elem.Width - 9f, ty + 2f, 7f, 7f);
            g.FillEllipse(treeBrush, elem.X + elem.Width - 10f, ty, 7f, 7f);
            g.FillEllipse(treeHi, elem.X + elem.Width - 9f, ty, 3.5f, 3.5f);
        }
    }

    /// <summary>
    /// 低層倉庫・工業施設
    /// </summary>
    private static void DrawCityWarehouse(Graphics g, BackgroundElement elem)
    {
        // 倉庫本体
        using (var bldgBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(bldgBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }
        // 屋根のリブ（波板スレート屋根の規則的ライン）
        using var ribPen = new Pen(elem.AccentColor, 1f);
        for (float rx = elem.X + 6f; rx < elem.X + elem.Width - 3f; rx += 6f)
        {
            g.DrawLine(ribPen, rx, elem.Y + 2f, rx, elem.Y + elem.Height - 2f);
        }
        using var framePen = new Pen(Color.FromArgb(35, 40, 48), 1.5f);
        g.DrawRectangle(framePen, elem.X, elem.Y, elem.Width, elem.Height);
    }

    #endregion

    #region 要塞前哨基地エリアのリアル描画

    /// <summary>
    /// 軍用滑走路（ピアノキー・スレッショルド、タッチダウンゾーン、サイド誘導灯）
    /// </summary>
    private static void DrawOutpostRunway(Graphics g, BackgroundElement elem)
    {
        // 滑走路コンクリートスラブ
        using (var rwBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(rwBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }

        // 目地グリッド（コンクリート版の継ぎ目）
        using (var jointPen = new Pen(Color.FromArgb(40, 44, 52), 1f))
        {
            for (float jy = elem.Y; jy < elem.Y + elem.Height; jy += 20f)
            {
                g.DrawLine(jointPen, elem.X, jy, elem.X + elem.Width, jy);
            }
        }

        // 滑走路端のピアノキー標識（スレッショルド）
        using (var stripeBrush = new SolidBrush(elem.AccentColor))
        {
            float barW = 3f;
            float barH = 14f;
            for (float sx = elem.X + 4f; sx < elem.X + elem.Width - 4f; sx += 6f)
            {
                g.FillRectangle(stripeBrush, sx, elem.Y + 3f, barW, barH);
            }
        }

        // 滑走路中心線（太い白破線）
        using var centerPen = new Pen(elem.AccentColor, 2.5f) { DashStyle = DashStyle.Dash };
        float midX = elem.X + elem.Width / 2f;
        g.DrawLine(centerPen, midX, elem.Y + 20f, midX, elem.Y + elem.Height - 10f);

        // サイド誘導灯（両端の緑色・赤色マーカー）
        using var greenLight = new SolidBrush(Color.FromArgb(40, 255, 120));
        using var redLight = new SolidBrush(Color.FromArgb(255, 60, 60));
        for (float ly = elem.Y + 6f; ly < elem.Y + elem.Height - 6f; ly += 16f)
        {
            g.FillEllipse(greenLight, elem.X + 1.5f, ly, 3f, 3f);
            g.FillEllipse(greenLight, elem.X + elem.Width - 4.5f, ly, 3f, 3f);
        }
    }

    /// <summary>
    /// アーチ型耐爆格納庫・バンカー（コーションストライプ、重装甲ハッチ）
    /// </summary>
    private static void DrawOutpostBunker(Graphics g, BackgroundElement elem)
    {
        // 影
        using (var sBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
        {
            g.FillRectangle(sBrush, elem.X + 4f, elem.Y + 5f, elem.Width, elem.Height);
        }

        // バンカー本体（アーチ金属屋根グラデーション）
        using (var bunkerBrush = new LinearGradientBrush(
            new PointF(elem.X, elem.Y),
            new PointF(elem.X + elem.Width, elem.Y),
            elem.ShadeColor,
            elem.BaseColor))
        {
            bunkerBrush.SetBlendTriangularShape(0.5f);
            g.FillRectangle(bunkerBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }

        // 警告コーションストライプ（黄・黒の45度斜めストライプバー）
        float barH = 5f;
        using (var yelBrush = new SolidBrush(elem.AccentColor))
        using (var blkBrush = new SolidBrush(Color.FromArgb(20, 20, 25)))
        {
            g.FillRectangle(yelBrush, elem.X, elem.Y, elem.Width, barH);
            for (float sx = elem.X; sx < elem.X + elem.Width; sx += 10f)
            {
                g.FillRectangle(blkBrush, sx, elem.Y, 5f, barH);
            }
        }

        // 外枠金属フレーム
        using var framePen = new Pen(Color.FromArgb(80, 85, 98), 1.5f);
        g.DrawRectangle(framePen, elem.X, elem.Y, elem.Width, elem.Height);
    }

    /// <summary>
    /// 球形燃料タンク・トラス構造
    /// </summary>
    private static void DrawOutpostFuelTank(Graphics g, BackgroundElement elem)
    {
        float r = elem.Width * 0.38f;
        float cx = elem.X + elem.Width / 2f;
        float cy = elem.Y + elem.Height / 2f;

        // ドロップシャドウ
        using (var sBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
        {
            g.FillEllipse(sBrush, cx - r + 4f, cy - r + 6f, r * 2f, r * 2f);
        }

        // 球体ライティング（左上ハイライト、右下ディープシャドウ）
        using (var sphereBrush = new LinearGradientBrush(
            new PointF(cx - r * 0.5f, cy - r * 0.5f),
            new PointF(cx + r, cy + r),
            Color.FromArgb(210, 220, 235),
            Color.FromArgb(40, 45, 55)))
        {
            g.FillEllipse(sphereBrush, cx - r, cy - r, r * 2f, r * 2f);
        }

        // スペキュラハイライト
        using (var specBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
        {
            g.FillEllipse(specBrush, cx - r * 0.5f, cy - r * 0.55f, r * 0.6f, r * 0.45f);
        }

        // 赤道リング・支柱脚
        using var ringPen = new Pen(Color.FromArgb(90, 95, 105), 1.5f);
        g.DrawArc(ringPen, cx - r, cy - r * 0.3f, r * 2f, r * 0.6f, 0, 180);
    }

    #endregion

    #region 敵要塞エリアのリアルSF描画

    /// <summary>
    /// ベベル装甲プレート・六角形ハニカムアーマー
    /// </summary>
    private static void DrawFortressHexArmor(Graphics g, BackgroundElement elem)
    {
        // 装甲プレート本体
        using (var panelBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(panelBrush, elem.X, elem.Y, elem.Width, elem.Height);
        }

        // ベベルエッジ（硬質な立体切削ハイライトとシャドウ）
        using (var hiPen = new Pen(elem.AccentColor, 1.5f))
        {
            g.DrawLine(hiPen, elem.X, elem.Y, elem.X + elem.Width, elem.Y);
            g.DrawLine(hiPen, elem.X, elem.Y, elem.X, elem.Y + elem.Height);
        }
        using (var darkPen = new Pen(Color.FromArgb(6, 7, 10), 1.5f))
        {
            g.DrawLine(darkPen, elem.X + elem.Width, elem.Y, elem.X + elem.Width, elem.Y + elem.Height);
            g.DrawLine(darkPen, elem.X, elem.Y + elem.Height, elem.X + elem.Width, elem.Y + elem.Height);
        }

        // ハニカム装甲グリッド（内部の幾何学スリット）
        using var slitPen = new Pen(Color.FromArgb(28, 32, 44), 1f);
        float step = 12f;
        for (float py = elem.Y + 6f; py < elem.Y + elem.Height - 6f; py += step)
        {
            g.DrawLine(slitPen, elem.X + 4f, py, elem.X + elem.Width - 4f, py);
        }
    }

    /// <summary>
    /// 脈動するエネルギー発電コア（多重グローリング、白熱コア、サイバーバス）
    /// </summary>
    private static void DrawFortressPlasmaCore(Graphics g, BackgroundElement elem)
    {
        long ticks = Environment.TickCount64;
        float cx = elem.X + elem.Width / 2f;
        float cy = elem.Y + elem.Height / 2f;
        float pulse = (float)(Math.Sin(ticks * 0.008) * 0.25 + 0.75);
        float r = (elem.Width * 0.35f) * pulse;

        // 金属ハッチフレーム
        using (var frameBrush = new SolidBrush(elem.BaseColor))
        {
            g.FillEllipse(frameBrush, cx - r - 6f, cy - r - 6f, (r + 6f) * 2, (r + 6f) * 2);
        }
        using (var rimPen = new Pen(Color.FromArgb(60, 68, 85), 2f))
        {
            g.DrawEllipse(rimPen, cx - r - 6f, cy - r - 6f, (r + 6f) * 2, (r + 6f) * 2);
        }

        // プラズマグローオーラ
        using (var auraBrush = new SolidBrush(Color.FromArgb((int)(110 * pulse), elem.AccentColor)))
        {
            g.FillEllipse(auraBrush, cx - r - 3f, cy - r - 3f, (r + 3f) * 2, (r + 3f) * 2);
        }

        // コア本体
        using (var coreBrush = new SolidBrush(elem.AccentColor))
        {
            g.FillEllipse(coreBrush, cx - r, cy - r, r * 2f, r * 2f);
        }

        // 白熱中心核
        using (var whiteCore = new SolidBrush(Color.White))
        {
            g.FillEllipse(whiteCore, cx - r * 0.4f, cy - r * 0.4f, r * 0.8f, r * 0.8f);
        }

        // 放射状サイバーサーキット
        using var circuitPen = new Pen(elem.AccentColor, 1.5f);
        g.DrawLine(circuitPen, cx, cy - r - 6f, cx, elem.Y);
        g.DrawLine(circuitPen, cx, cy + r + 6f, cx, elem.Y + elem.Height);
        g.DrawLine(circuitPen, cx - r - 6f, cy, elem.X, cy);
        g.DrawLine(circuitPen, cx + r + 6f, cy, elem.X + elem.Width, cy);
    }

    /// <summary>
    /// 排熱ルーバー・高熱スリット（内部から赤熱する通気口）
    /// </summary>
    private static void DrawFortressHeatVent(Graphics g, BackgroundElement elem)
    {
        // 装甲枠
        using (var ventFrame = new SolidBrush(elem.BaseColor))
        {
            g.FillRectangle(ventFrame, elem.X, elem.Y, elem.Width, elem.Height);
        }

        using var framePen = new Pen(Color.FromArgb(50, 55, 70), 1.5f);
        g.DrawRectangle(framePen, elem.X, elem.Y, elem.Width, elem.Height);

        // 排熱ルーバースリット（スリットから漏れ出す灼熱オレンジ光）
        using var heatBrush = new SolidBrush(elem.AccentColor);
        using var louverPen = new Pen(Color.FromArgb(12, 13, 18), 2f);

        float sy = elem.Y + 5f;
        while (sy < elem.Y + elem.Height - 5f)
        {
            g.FillRectangle(heatBrush, elem.X + 4f, sy, elem.Width - 8f, 2.5f);
            g.DrawLine(louverPen, elem.X + 4f, sy + 3f, elem.X + elem.Width - 4f, sy + 3f);
            sy += 6f;
        }
    }

    #endregion

    #region 川・運河・プラズマ導管・架橋のリアル描画

    /// <summary>
    /// 川（森）/ 護岸運河（街）/ 補給パイプ（前哨基地）/ プラズマ導管（敵要塞）の多層リアル描画
    /// </summary>
    private void DrawRiverOrConduit(Graphics g)
    {
        var points = new PointF[20];
        float stepH = Height / 10f;
        for (int i = 0; i < 20; i++)
        {
            float ry = i * stepH + (ScrollY % stepH);
            points[i] = new PointF(_riverPoints[i], ry);
        }

        var theme = CurrentTheme;
        long ticks = Environment.TickCount64;

        switch (theme)
        {
            case BackgroundTheme.ForestAndRiver:
                // 1. 川岸（湿地・アースブラウンの土手）
                using (var bankPen = new Pen(Color.FromArgb(18, 28, 12), 36f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawCurve(bankPen, points);
                }

                // 2. 浅瀬（エメラルドターコイズの水底）
                using (var shallowPen = new Pen(Color.FromArgb(10, 100, 150), 30f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawCurve(shallowPen, points);
                }

                // 3. 深水部（ディープサファイアブルー）
                using (var deepPen = new Pen(Color.FromArgb(4, 55, 140), 20f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawCurve(deepPen, points);
                }

                // 4. 水面の波紋・光の煌めき
                using (var shinePen = new Pen(Color.FromArgb(80, 180, 255), 3f)
                {
                    DashStyle = DashStyle.Dash,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawCurve(shinePen, points);
                }
                break;

            case BackgroundTheme.City:
                // 近代的な護岸運河（コンクリート垂直護岸＋ディープネイビー水路）
                // 1. 護岸外壁
                using (var canalBankPen = new Pen(Color.FromArgb(60, 64, 72), 36f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawCurve(canalBankPen, points);
                }

                // 2. 護岸コーピング（縁石ハイライト）
                using (var copingPen = new Pen(Color.FromArgb(110, 115, 125), 30f))
                {
                    g.DrawCurve(copingPen, points);
                }

                // 3. 運河の水面
                using (var canalWater = new Pen(Color.FromArgb(18, 48, 90), 22f))
                {
                    g.DrawCurve(canalWater, points);
                }
                break;

            case BackgroundTheme.Outpost:
                // 重厚な軍用補給パイプライン
                // 1. パイプ接地ドロップシャドウ
                using (var pipeShadow = new Pen(Color.FromArgb(65, 0, 0, 0), 28f))
                {
                    g.DrawCurve(pipeShadow, points);
                }

                // 2. パイプ鋼管本体（スチールメタルグラデーション調）
                using (var pipeSteel = new Pen(Color.FromArgb(65, 70, 80), 20f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawCurve(pipeSteel, points);
                }

                // 3. パイプ上部ハイライト
                using (var pipeHi = new Pen(Color.FromArgb(150, 160, 175), 4f))
                {
                    g.DrawCurve(pipeHi, points);
                }
                break;

            case BackgroundTheme.Fortress:
            default:
                // 激しく脈動する巨大プラズマエネルギー導管
                float pulse = (float)(Math.Sin(ticks * 0.01) * 0.25 + 0.75);

                // 1. 導管外殻コンジットフレーム
                using (var conduitFrame = new Pen(Color.FromArgb(24, 26, 36), 32f))
                {
                    g.DrawCurve(conduitFrame, points);
                }

                // 2. プラズマグローオーラ（広域発光）
                using (var auraPen = new Pen(Color.FromArgb((int)(120 * pulse), 0, 230, 255), 24f))
                {
                    g.DrawCurve(auraPen, points);
                }

                // 3. ネオンエネルギービーム
                using (var beamPen = new Pen(Color.FromArgb(0, 240, 255), 12f * pulse))
                {
                    g.DrawCurve(beamPen, points);
                }

                // 4. ホワイトホットなエネルギーコア
                using (var coreLaser = new Pen(Color.White, 4f * pulse))
                {
                    g.DrawCurve(coreLaser, points);
                }
                break;
        }
    }

    /// <summary>
    /// 川や水路を跨ぐ上層の橋（石橋・高速道路高架橋・パイプ支持トラス）
    /// </summary>
    private void DrawOverpasses(Graphics g)
    {
        var theme = CurrentTheme;
        float bridgeY1 = (ScrollY + Height * 0.3f) % Height;
        float bridgeY2 = (ScrollY + Height * 0.8f) % Height;

        float[] bridgeYPositions = { bridgeY1, bridgeY2 };

        foreach (float by in bridgeYPositions)
        {
            // 川の現在位置を算出
            int idx = Math.Clamp((int)(by / (Height / 10f)), 0, _riverPoints.Length - 1);
            float rx = _riverPoints[idx];

            switch (theme)
            {
                case BackgroundTheme.ForestAndRiver:
                    // 森の石橋（アーチ型欄干・石畳）
                    using (var bShadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    {
                        g.FillRectangle(bShadow, rx - 28f, by + 4f, 56f, 14f);
                    }
                    using (var stoneBrush = new SolidBrush(Color.FromArgb(95, 90, 80)))
                    {
                        g.FillRectangle(stoneBrush, rx - 28f, by, 56f, 14f);
                    }
                    using (var railPen = new Pen(Color.FromArgb(135, 130, 120), 2f))
                    {
                        g.DrawLine(railPen, rx - 28f, by, rx + 28f, by);
                        g.DrawLine(railPen, rx - 28f, by + 14f, rx + 28f, by + 14f);
                    }
                    break;

                case BackgroundTheme.City:
                    // 高速道路の高架橋（ハイウェイブリッジ）
                    using (var bShadow = new SolidBrush(Color.FromArgb(95, 0, 0, 0)))
                    {
                        g.FillRectangle(bShadow, rx - 35f, by + 6f, 70f, 18f);
                    }
                    using (var roadBrush = new SolidBrush(Color.FromArgb(32, 34, 40)))
                    {
                        g.FillRectangle(roadBrush, rx - 35f, by, 70f, 18f);
                    }
                    // ガードレール
                    using (var guardPen = new Pen(Color.FromArgb(110, 115, 125), 2f))
                    {
                        g.DrawLine(guardPen, rx - 35f, by, rx + 35f, by);
                        g.DrawLine(guardPen, rx - 35f, by + 18f, rx + 35f, by + 18f);
                    }
                    // センター白線
                    using (var whitePen = new Pen(Color.FromArgb(235, 235, 240), 1.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(whitePen, rx - 35f, by + 9f, rx + 35f, by + 9f);
                    }
                    break;

                case BackgroundTheme.Outpost:
                case BackgroundTheme.Fortress:
                    // トラス支柱・重装甲クロスゲート
                    using (var bShadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                    {
                        g.FillRectangle(bShadow, rx - 32f, by + 5f, 64f, 12f);
                    }
                    using (var trussBrush = new SolidBrush(Color.FromArgb(40, 44, 55)))
                    {
                        g.FillRectangle(trussBrush, rx - 32f, by, 64f, 12f);
                    }
                    using (var trussPen = new Pen(Color.FromArgb(80, 90, 110), 1.5f))
                    {
                        g.DrawRectangle(trussPen, rx - 32f, by, 64f, 12f);
                        g.DrawLine(trussPen, rx - 32f, by, rx + 32f, by + 12f);
                        g.DrawLine(trussPen, rx - 32f, by + 12f, rx + 32f, by);
                    }
                    break;
            }
        }
    }

    #endregion
}
