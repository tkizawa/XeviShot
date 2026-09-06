using System;
using System.Collections.Generic;
using System.Drawing;
using XeviShot.Audio;
using XeviShot.Input;
using XeviShot.Settings;

namespace XeviShot.Entities;

/// <summary>
/// ゲーム全体のロジック、エンティティ管理、衝突判定を司るクラス
/// </summary>
public class Game
{
    public float Width { get; }
    public float Height { get; }

    public Player Player { get; }
    public Background Background { get; }

    public List<Bullet> Bullets { get; } = new();
    public List<LaserBullet> LaserBullets { get; } = new();
    public List<WaveCannon> WaveCannons { get; } = new();
    public List<Bomb> Bombs { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public List<EnemyBullet> EnemyBullets { get; } = new();
    public List<ItemCapsule> Items { get; } = new();

    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int Lives { get; private set; } = 3;
    public bool GameOver { get; private set; } = false;
    public bool StageClear { get; private set; } = false;
    public bool BossActive { get; private set; } = false;
    public bool BossSpawned { get; private set; } = false;

    private float _enemySpawnTimer = 0f;
    private float _enemySpawnInterval = 60f;
    private float _groundSpawnTimer = 0f;
    private const float GroundSpawnInterval = 180f;

    private int _frameCount = 0;
    private float _phaeonSpawnTimer = 0f;
    private const float PhaeonSpawnInterval = 300f;

    private static readonly Random Rand = new();

    public Game(float width, float height, int initialScore = 0)
    {
        Width = width;
        Height = height;
        Score = initialScore;
        HighScore = Math.Max(Score, SettingsManager.Current.HighScore);

        Player = new Player(width / 2f, height - 100f);
        Background = new Background(width, height);
    }

    public void Update(InputManager input)
    {
        if (GameOver || StageClear) return;

        // デバッグ用: 1キーで街(1分)、2キーで敵要塞(2分50秒)、Tキーでボス直前(2分55秒)へスキップ
        if (input.CityTestKey && _frameCount < 3600)
        {
            _frameCount = 3600;
        }
        else if (input.FortressTestKey && _frameCount < 10200)
        {
            _frameCount = 10200;
        }
        else if (input.BossTestKey && _frameCount < 10500)
        {
            _frameCount = 10500;
        }

        _frameCount++;

        // 3分経過（10800フレーム）でボス出現
        if (_frameCount >= 10800 && !BossActive && !BossSpawned)
        {
            Enemies.Add(new Boss(Width / 2f, -50f));
            BossActive = true;
            BossSpawned = true;
            AudioManager.Instance.PlayBossBgm();
        }

        // ボス撃破確認
        if (BossActive)
        {
            bool hasBoss = false;
            foreach (var e in Enemies)
            {
                if (e is Boss)
                {
                    hasBoss = true;
                    break;
                }
            }

            if (!hasBoss)
            {
                BossActive = false;
                StageClear = true;
                Enemies.Clear();
                EnemyBullets.Clear();
                if (HighScore > SettingsManager.Current.HighScore)
                {
                    SettingsManager.Current.HighScore = HighScore;
                    SettingsManager.Save();
                }
            }
        }

        Background.Update(_frameCount);
        Player.UpdateInput(input, Width, Height);

        // 自機の振動トリガー処理
        if (Player.RumbleTrigger != null)
        {
            string trigger = Player.RumbleTrigger;
            Player.RumbleTrigger = null;
            if (trigger == "charge_complete")
            {
                input.TriggerRumble(0.0f, 0.6f, 120);
            }
            else if (trigger == "charge_complete2")
            {
                input.TriggerRumble(0.0f, 1.0f, 200);
            }
        }

        // 1. 対空弾通常発射
        if (Player.ShootNormal)
        {
            Player.ShootNormal = false;
            if (Player.HasLaser)
            {
                LaserBullets.Add(new LaserBullet(Player.X, Player.Y - Player.Height / 2f, level: Player.WeaponLevel));
                Player.CooldownAir = 12;
            }
            else
            {
                int level = Player.WeaponLevel;
                if (level >= 3)
                {
                    float[] angles = { -25f, -8f, 8f, 25f };
                    float[] offsets = { -15f, -5f, 5f, 15f };
                    for (int i = 0; i < angles.Length; i++)
                    {
                        double rad = angles[i] * Math.PI / 180.0;
                        float vx = (float)(10.0 * Math.Sin(rad));
                        float vy = (float)(-10.0 * Math.Cos(rad));
                        Bullets.Add(new Bullet(Player.X + offsets[i], Player.Y - Player.Height / 2f, vx, vy));
                    }
                }
                else if (level == 2)
                {
                    Bullets.Add(new Bullet(Player.X - 10f, Player.Y - Player.Height / 2f));
                    Bullets.Add(new Bullet(Player.X + 10f, Player.Y - Player.Height / 2f));
                }
                else
                {
                    Bullets.Add(new Bullet(Player.X, Player.Y - Player.Height / 2f));
                }
                Player.CooldownAir = 10;
            }
            AudioManager.Instance.Play("laser");
        }

        // 2. 波動砲発射 (通常チャージ)
        if (Player.ShootWave)
        {
            Player.ShootWave = false;
            WaveCannons.Add(new WaveCannon(Player.X, Player.Y - Player.Height / 2f));
            AudioManager.Instance.Play("wave_cannon");
            input.TriggerRumble(0.6f, 0.4f, 250);
        }

        // 3. 拡散波動砲発射 (最大チャージ)
        if (Player.ShootDiffusionWave)
        {
            Player.ShootDiffusionWave = false;
            float[] angles = { -30f, -15f, 0f, 15f, 30f };
            const float baseSpeed = 12f;
            foreach (float angle in angles)
            {
                double rad = angle * Math.PI / 180.0;
                float vx = (float)(baseSpeed * Math.Sin(rad));
                float vy = (float)(-baseSpeed * Math.Cos(rad));
                WaveCannons.Add(new WaveCannon(Player.X, Player.Y - Player.Height / 2f, vx, vy));
            }
            AudioManager.Instance.Play("wave_cannon");
            input.TriggerRumble(0.9f, 0.7f, 450);
        }

        // 4. 対地ボム発射 (X または C または Pad B)
        if (input.FireGround || input.FireBoth)
        {
            if (Player.CooldownGround <= 0)
            {
                float targetX = Player.X;
                float targetY = Player.Y - Player.ReticleDistance;
                Bombs.Add(new Bomb(Player.X, Player.Y, targetX, targetY));
                Player.CooldownGround = 60;
                AudioManager.Instance.Play("bomb_launch");
            }
        }

        // 5. 敵の出現管理（ボス未出現時）
        if (!BossSpawned)
        {
            // 空中通常敵 (Zoldas)
            _enemySpawnTimer++;
            if (_enemySpawnTimer > _enemySpawnInterval)
            {
                SpawnEnemy("air");
                _enemySpawnTimer = 0;
                if (_enemySpawnInterval > 20f)
                {
                    _enemySpawnInterval -= 0.5f;
                }
            }

            // 地上敵砲台
            _groundSpawnTimer++;
            if (_groundSpawnTimer > GroundSpawnInterval)
            {
                SpawnEnemy("ground");
                _groundSpawnTimer = 0;
            }

            // 1分経過（3600フレーム）後から出現する高速強敵 (Phaeon)
            if (_frameCount > 3600)
            {
                _phaeonSpawnTimer++;
                if (_phaeonSpawnTimer > PhaeonSpawnInterval)
                {
                    _phaeonSpawnTimer = 0;
                    int numPhaeons = Rand.Next(1, 4);
                    float baseX = (float)(Rand.NextDouble() * (Width - 160) + 80);
                    for (int i = 0; i < numPhaeons; i++)
                    {
                        float spawnX = Math.Clamp(baseX + (i - (numPhaeons - 1) / 2f) * 40f, 30f, Width - 30f);
                        float spawnY = -50f - i * 30f;
                        Enemies.Add(new Enemy(spawnX, spawnY, "phaeon"));
                    }
                }
            }
        }

        // 6. 各エンティティの更新
        foreach (var b in Bullets) b.Update();
        foreach (var lb in LaserBullets) lb.Update();
        foreach (var wc in WaveCannons) wc.Update();

        foreach (var bomb in Bombs)
        {
            bool wasExploded = bomb.Exploded;
            bomb.Update();
            if (bomb.Exploded && !wasExploded)
            {
                input.TriggerRumble(0.4f, 0.2f, 150);
            }
        }

        foreach (var enemy in Enemies)
        {
            enemy.Update();
            if (enemy.ShootNow)
            {
                enemy.ShootNow = false;
                if (enemy.Type == "phaeon")
                {
                    float dx = Player.X - enemy.X;
                    float dy = Player.Y - enemy.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    float vx = dist > 0 ? (dx / dist) * 6.0f : 0f;
                    float vy = dist > 0 ? (dy / dist) * 6.0f : 6.0f;
                    EnemyBullets.Add(new EnemyBullet(enemy.X, enemy.Y, vx, vy));
                }
                else if (enemy is Boss)
                {
                    float[] angles = { -40f, -20f, 0f, 20f, 40f };
                    const float bulletSpeed = 5.0f;
                    foreach (float angle in angles)
                    {
                        double rad = angle * Math.PI / 180.0;
                        float vx = (float)(bulletSpeed * Math.Sin(rad));
                        float vy = (float)(bulletSpeed * Math.Cos(rad));
                        EnemyBullets.Add(new EnemyBullet(enemy.X, enemy.Y + 20f, vx, vy));
                    }
                }
                else
                {
                    EnemyBullets.Add(new EnemyBullet(enemy.X, enemy.Y + enemy.Height / 2f));
                }
            }
        }

        foreach (var eb in EnemyBullets) eb.Update();
        foreach (var item in Items) item.Update();

        // 削除済みエンティティの一括除去
        Bullets.RemoveAll(b => b.MarkedForDeletion);
        LaserBullets.RemoveAll(lb => lb.MarkedForDeletion);
        WaveCannons.RemoveAll(wc => wc.MarkedForDeletion);
        EnemyBullets.RemoveAll(eb => eb.MarkedForDeletion);
        Bombs.RemoveAll(b => b.MarkedForDeletion);
        Enemies.RemoveAll(e => e.MarkedForDeletion);
        Items.RemoveAll(i => i.MarkedForDeletion);

        // 7. 衝突判定
        CheckCollisions(input);

        // 衝突により削除マークされたエンティティを除去
        Bullets.RemoveAll(b => b.MarkedForDeletion);
        LaserBullets.RemoveAll(lb => lb.MarkedForDeletion);
        WaveCannons.RemoveAll(wc => wc.MarkedForDeletion);
        EnemyBullets.RemoveAll(eb => eb.MarkedForDeletion);
        Bombs.RemoveAll(b => b.MarkedForDeletion);
        Enemies.RemoveAll(e => e.MarkedForDeletion);
        Items.RemoveAll(i => i.MarkedForDeletion);

        // ハイスコア更新チェック（現在のスコアが過去最高を超えたら更新）
        if (Score > HighScore)
        {
            HighScore = Score;
        }
    }

    private void SpawnEnemy(string type)
    {
        float x = (float)(Rand.NextDouble() * (Width - 60) + 30);
        float y = -50f;
        Enemies.Add(new Enemy(x, y, type));
    }

    private void CheckCollisions(InputManager input)
    {
        // 1. 自機通常弾 vs 空中敵 / ボス
        foreach (var bullet in Bullets)
        {
            if (bullet.MarkedForDeletion) continue;

            foreach (var enemy in Enemies)
            {
                if (enemy.Type is not ("air" or "phaeon") && enemy is not Boss) continue;
                if (enemy.MarkedForDeletion) continue;

                if (IsColliding(bullet, enemy))
                {
                    bullet.MarkedForDeletion = true;
                    HitEnemy(enemy);
                    break;
                }
            }
        }

        // 2. 自機レーザー弾 vs 空中敵 / ボス (貫通)
        foreach (var lb in LaserBullets)
        {
            if (lb.MarkedForDeletion) continue;

            foreach (var enemy in Enemies)
            {
                if (enemy.Type is not ("air" or "phaeon") && enemy is not Boss) continue;
                if (enemy.MarkedForDeletion) continue;

                if (IsColliding(lb, enemy))
                {
                    HitEnemy(enemy);
                }
            }
        }

        // 3. 波動砲 vs 空中敵 / ボス (貫通) & 敵弾かき消し
        foreach (var wc in WaveCannons)
        {
            if (wc.MarkedForDeletion) continue;

            // 敵への貫通攻撃
            foreach (var enemy in Enemies)
            {
                if (enemy.Type is not ("air" or "phaeon") && enemy is not Boss) continue;
                if (enemy.MarkedForDeletion) continue;

                if (IsColliding(wc, enemy))
                {
                    HitEnemy(enemy);
                }
            }

            // 敵弾のかき消し
            foreach (var eb in EnemyBullets)
            {
                if (eb.MarkedForDeletion) continue;

                if (IsColliding(wc, eb))
                {
                    eb.MarkedForDeletion = true;
                    Score += 10;
                }
            }
        }

        // 4. ボム爆発 vs 地上敵
        foreach (var bomb in Bombs)
        {
            if (!bomb.Exploded) continue;

            foreach (var enemy in Enemies)
            {
                if (enemy.Type != "ground" || enemy.MarkedForDeletion) continue;

                float dx = bomb.X - enemy.X;
                float dy = bomb.Y - enemy.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float radius = (20f + bomb.ExplosionTimer) + enemy.Width / 2f;

                if (dist < radius)
                {
                    enemy.MarkedForDeletion = true;
                    Score += 300;
                    DropItem(enemy.X, enemy.Y);
                }
            }
        }

        if (!GameOver)
        {
            // 5. 自機 vs アイテム
            foreach (var item in Items)
            {
                if (item.MarkedForDeletion) continue;

                if (IsColliding(Player, item))
                {
                    item.MarkedForDeletion = true;
                    if (item is ShieldCapsule)
                    {
                        Player.ShieldCount = 5;
                    }
                    else if (item is WeaponCapsule)
                    {
                        if (Player.HasLaser)
                        {
                            // レーザーモード → 通常弾カプセル: レーザー解除して WEAPON LEVEL 1 にリセット
                            Player.HasLaser = false;
                            Player.WeaponLevel = 1;
                        }
                        else
                        {
                            // 通常弾モード → 通常弾カプセル: レベルアップ (最大3)
                            Player.WeaponLevel = Math.Min(3, Player.WeaponLevel + 1);
                        }
                    }
                    else if (item is LaserCapsule)
                    {
                        if (!Player.HasLaser)
                        {
                            // 通常弾モード → レーザーカプセル: レーザー有効化して WEAPON LEVEL 1 に初期化
                            Player.HasLaser = true;
                            Player.WeaponLevel = 1;
                        }
                        else
                        {
                            // レーザーモード → レーザーカプセル: レベルアップ (最大3)
                            Player.WeaponLevel = Math.Min(3, Player.WeaponLevel + 1);
                        }
                    }
                }
            }

            // 6. 自機 vs 空中敵 / ボス
            foreach (var enemy in Enemies)
            {
                if (enemy.Type is not ("air" or "phaeon") && enemy is not Boss) continue;
                if (enemy.MarkedForDeletion) continue;

                if (IsColliding(Player, enemy))
                {
                    if (enemy is not Boss)
                    {
                        enemy.MarkedForDeletion = true;
                    }
                    HitPlayer(input);
                    break;
                }
            }

            // 7. 自機 vs 敵弾
            foreach (var eb in EnemyBullets)
            {
                if (eb.MarkedForDeletion) continue;

                if (IsColliding(Player, eb))
                {
                    eb.MarkedForDeletion = true;
                    HitPlayer(input);
                    break;
                }
            }
        }
    }

    private void HitEnemy(Entity entity)
    {
        if (entity is Boss boss)
        {
            if (boss.State is "ENTER" or "HOVER")
            {
                boss.Hp--;
                boss.FlashTimer = 5;
                if (boss.Hp <= 0)
                {
                    boss.State = "DEFEATED";
                    AudioManager.Instance.Play("explosion_ground");
                }
            }
        }
        else if (entity is Enemy enemy)
        {
            enemy.MarkedForDeletion = true;
            DropItem(enemy.X, enemy.Y);

            if (enemy.Type == "phaeon")
            {
                Score += 500;
            }
            else
            {
                Score += 100;
            }
            AudioManager.Instance.Play("explosion_air");
        }
    }

    private void DropItem(float x, float y)
    {
        double roll = Rand.NextDouble();
        if (roll <= 0.10)
        {
            Items.Add(new ShieldCapsule(x, y));
        }
        else if (roll <= 0.20)
        {
            Items.Add(new WeaponCapsule(x, y));
        }
        else if (roll <= 0.30)
        {
            Items.Add(new LaserCapsule(x, y));
        }
    }

    private void HitPlayer(InputManager input)
    {
        if (Player.ShieldCount > 0)
        {
            Player.ShieldCount--;
            AudioManager.Instance.Play("player_hit");
            input.TriggerRumble(0.6f, 0.6f, 250);
        }
        else
        {
            LoseLife(input);
        }
    }

    private void LoseLife(InputManager input)
    {
        Lives--;
        AudioManager.Instance.Play("player_hit");
        input.TriggerRumble(1.0f, 1.0f, 500);

        // 状態リセット
        Player.ChargeTimer = 0;
        Player.WasFireAirPressed = false;
        Player.PlayedComplete = false;
        Player.PlayedComplete2 = false;
        Player.ShootDiffusionWave = false;
        Player.WeaponLevel = 1;
        Player.HasLaser = false;
        AudioManager.Instance.StopCharge();

        // 画面内の敵弾と至近の敵をクリア
        Enemies.RemoveAll(e => e.Y < Player.Y - 100f);
        EnemyBullets.Clear();

        Player.X = Width / 2f;
        Player.Y = Height - 100f;

        if (Lives <= 0)
        {
            GameOver = true;
            if (HighScore > SettingsManager.Current.HighScore)
            {
                SettingsManager.Current.HighScore = HighScore;
                SettingsManager.Save();
            }
            AudioManager.Instance.Play("game_over");
        }
    }

    private static bool IsColliding(Entity r1, Entity r2)
    {
        return r1.X - r1.Width / 2f < r2.X + r2.Width / 2f &&
               r1.X + r1.Width / 2f > r2.X - r2.Width / 2f &&
               r1.Y - r1.Height / 2f < r2.Y + r2.Height / 2f &&
               r1.Y + r1.Height / 2f > r2.Y - r2.Height / 2f;
    }

    public void Draw(Graphics g)
    {
        // 1. 背景
        Background.Draw(g);

        // 2. 地上敵
        foreach (var e in Enemies)
        {
            if (e.Type == "ground") e.Draw(g);
        }

        // 3. ボム
        foreach (var b in Bombs) b.Draw(g);

        // 4. アイテムカプセル
        foreach (var item in Items) item.Draw(g);

        // 5. 自機
        if (!GameOver)
        {
            Player.Draw(g);
        }

        // 6. 空中敵・ボス
        foreach (var e in Enemies)
        {
            if (e.Type is "air" or "phaeon" || e is Boss) e.Draw(g);
        }

        // 7. 自機弾・敵弾
        foreach (var b in Bullets) b.Draw(g);
        foreach (var lb in LaserBullets) lb.Draw(g);
        foreach (var wc in WaveCannons) wc.Draw(g);
        foreach (var eb in EnemyBullets) eb.Draw(g);
    }
}
