class Game {
    constructor(canvasWidth, canvasHeight) {
        this.width = canvasWidth;
        this.height = canvasHeight;
        this.player = new Player(this.width / 2, this.height - 100);
        this.background = new Background(this.width, this.height);
        this.keys = {};
        
        this.bullets = [];
        this.bombs = [];
        this.enemies = [];
        this.particles = []; // Optional for explosion effects
        
        this.score = 0;
        this.lives = 3;
        this.gameOver = false;
        
        this.enemySpawnTimer = 0;
        this.enemySpawnInterval = 60; // Frames

        this.groundSpawnTimer = 0;
        this.groundSpawnInterval = 180;
    }

    update() {
        if (this.gameOver) return;

        this.background.update();
        this.player.update(this.keys, this.width, this.height);

        // Handle Shooting (X - Air Attack)
        if (this.keys['x'] || this.keys['X']) {
            if (this.player.cooldownAir <= 0) {
                this.bullets.push(new Bullet(this.player.x, this.player.y - this.player.height / 2));
                this.player.cooldownAir = 10; // Frames between shots
                audioManager.playLaser();
            }
        }

        // Handle Bombing (Z - Ground Attack)
        if (this.keys['z'] || this.keys['Z']) {
            if (this.player.cooldownGround <= 0) {
                const targetX = this.player.x;
                const targetY = this.player.y - this.player.reticleDistance;
                this.bombs.push(new Bomb(this.player.x, this.player.y, targetX, targetY));
                this.player.cooldownGround = 60; // Slower fire rate for bombs
                audioManager.playBombLaunch();
            }
        }

        // Spawners
        this.enemySpawnTimer++;
        if (this.enemySpawnTimer > this.enemySpawnInterval) {
            this.spawnEnemy('air');
            this.enemySpawnTimer = 0;
            // Slightly increase difficulty over time
            if (this.enemySpawnInterval > 20) this.enemySpawnInterval -= 0.5;
        }

        this.groundSpawnTimer++;
        if (this.groundSpawnTimer > this.groundSpawnInterval) {
            this.spawnEnemy('ground');
            this.groundSpawnTimer = 0;
        }

        // Update arrays
        this.bullets.forEach(b => b.update());
        this.bombs.forEach(b => b.update());
        this.enemies.forEach(e => e.update());

        // Remove deleted entities
        this.bullets = this.bullets.filter(b => !b.markedForDeletion);
        this.bombs = this.bombs.filter(b => !b.markedForDeletion);
        this.enemies = this.enemies.filter(e => !e.markedForDeletion);

        this.checkCollisions();
    }

    draw(ctx) {
        this.background.draw(ctx);
        
        // Draw in correct z-order: Ground Enemies -> Bombs -> Player -> Air Enemies -> Bullets
        
        this.enemies.filter(e => e.type === 'ground').forEach(e => e.draw(ctx));
        this.bombs.forEach(b => b.draw(ctx));
        
        if (!this.gameOver) {
            this.player.draw(ctx);
        }
        
        this.enemies.filter(e => e.type === 'air').forEach(e => e.draw(ctx));
        this.bullets.forEach(b => b.draw(ctx));
    }

    spawnEnemy(type) {
        let x = Math.random() * (this.width - 60) + 30;
        let y = type === 'air' ? -50 : -50;
        this.enemies.push(new Enemy(x, y, type));
    }

    checkCollisions() {
        // Bullet vs Air Enemies
        this.bullets.forEach(bullet => {
            this.enemies.filter(e => e.type === 'air').forEach(enemy => {
                if (this.isColliding(bullet, enemy)) {
                    bullet.markedForDeletion = true;
                    enemy.markedForDeletion = true;
                    this.score += 100;
                    this.updateScoreDisplay();
                    audioManager.playExplosionAir();
                }
            });
        });

        // Bomb Explosion vs Ground Enemies
        this.bombs.filter(b => b.exploded).forEach(bomb => {
            this.enemies.filter(e => e.type === 'ground').forEach(enemy => {
                // Circular collision for explosion
                let dx = bomb.x - enemy.x;
                let dy = bomb.y - enemy.y;
                let distance = Math.sqrt(dx * dx + dy * dy);
                if (distance < (20 + bomb.explosionTimer) + enemy.width/2) {
                    enemy.markedForDeletion = true;
                    this.score += 300;
                    this.updateScoreDisplay();
                }
            });
        });

        // Player vs Air Enemies (Crash)
        if (!this.gameOver) {
            this.enemies.filter(e => e.type === 'air').forEach(enemy => {
                if (this.isColliding(this.player, enemy)) {
                    enemy.markedForDeletion = true;
                    this.loseLife();
                }
            });
        }
    }

    isColliding(rect1, rect2) {
        return (rect1.x - rect1.width/2 < rect2.x + rect2.width/2 &&
                rect1.x + rect1.width/2 > rect2.x - rect2.width/2 &&
                rect1.y - rect1.height/2 < rect2.y + rect2.height/2 &&
                rect1.y + rect1.height/2 > rect2.y - rect2.height/2);
    }

    loseLife() {
        this.lives--;
        this.updateLivesDisplay();
        
        // Play player hit sound
        audioManager.playPlayerHit();
        
        // Clear enemies nearby or reset player pos
        this.enemies = this.enemies.filter(e => e.y < this.player.y - 100);
        this.player.x = this.width / 2;
        this.player.y = this.height - 100;

        if (this.lives <= 0) {
            this.gameOver = true;
        }
    }

    updateScoreDisplay() {
        const scoreEl = document.getElementById('score');
        if (scoreEl) scoreEl.innerText = this.score;
    }

    updateLivesDisplay() {
        const livesEl = document.getElementById('lives');
        if (livesEl) livesEl.innerText = this.lives;
    }
}
