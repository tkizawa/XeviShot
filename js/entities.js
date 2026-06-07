class Player {
    constructor(x, y) {
        this.x = x;
        this.y = y;
        this.width = 30;
        this.height = 30;
        this.speed = 4;
        this.color = '#0ff';
        this.cooldownAir = 0;
        this.cooldownGround = 0;
        this.reticleDistance = 120;
    }

    update(keys, canvasWidth, canvasHeight) {
        // Movement
        if (keys['ArrowUp'] || keys['ArrowUp']) this.y -= this.speed;
        if (keys['ArrowDown'] || keys['ArrowDown']) this.y += this.speed;
        if (keys['ArrowLeft'] || keys['ArrowLeft']) this.x -= this.speed;
        if (keys['ArrowRight'] || keys['ArrowRight']) this.x += this.speed;

        // Boundaries
        this.x = Math.max(this.width/2, Math.min(canvasWidth - this.width/2, this.x));
        this.y = Math.max(this.height/2, Math.min(canvasHeight - this.height/2, this.y));

        // Cooldowns
        if (this.cooldownAir > 0) this.cooldownAir--;
        if (this.cooldownGround > 0) this.cooldownGround--;
    }

    draw(ctx) {
        // Draw Player Ship (Geometric shape: triangle/polygon)
        ctx.fillStyle = this.color;
        ctx.beginPath();
        ctx.moveTo(this.x, this.y - this.height / 2); // Nose
        ctx.lineTo(this.x + this.width / 2, this.y + this.height / 2); // Right wing
        ctx.lineTo(this.x, this.y + this.height / 4); // Engine indent
        ctx.lineTo(this.x - this.width / 2, this.y + this.height / 2); // Left wing
        ctx.closePath();
        ctx.fill();

        // Draw Reticle
        this.drawReticle(ctx);
    }

    drawReticle(ctx) {
        const reticleX = this.x;
        const reticleY = this.y - this.reticleDistance;
        
        ctx.strokeStyle = 'rgba(255, 0, 0, 0.7)';
        ctx.lineWidth = 2;
        ctx.beginPath();
        // Crosshair
        ctx.moveTo(reticleX - 10, reticleY);
        ctx.lineTo(reticleX + 10, reticleY);
        ctx.moveTo(reticleX, reticleY - 10);
        ctx.lineTo(reticleX, reticleY + 10);
        
        // Box
        ctx.strokeRect(reticleX - 8, reticleY - 8, 16, 16);
        ctx.stroke();
    }
}

class Bullet {
    constructor(x, y) {
        this.x = x;
        this.y = y;
        this.width = 4;
        this.height = 12;
        this.speed = 10;
        this.color = '#ff0';
        this.markedForDeletion = false;
    }

    update() {
        this.y -= this.speed;
        if (this.y < 0) this.markedForDeletion = true;
    }

    draw(ctx) {
        ctx.fillStyle = this.color;
        ctx.fillRect(this.x - this.width / 2, this.y - this.height / 2, this.width, this.height);
    }
}

class Bomb {
    constructor(startX, startY, targetX, targetY) {
        this.x = startX;
        this.y = startY;
        this.targetX = targetX;
        this.targetY = targetY;
        
        this.progress = 0; // 0 to 1
        this.speed = 0.03;
        this.size = 10;
        this.markedForDeletion = false;
        this.exploded = false;
        this.explosionTimer = 0;
    }

    update() {
        if (this.exploded) {
            this.explosionTimer++;
            if (this.explosionTimer > 15) {
                this.markedForDeletion = true;
            }
            return;
        }

        this.progress += this.speed;
        
        // Lerp position
        this.x = this.x + (this.targetX - this.x) * 0.1;
        this.y = this.y + (this.targetY - this.y) * this.progress;

        if (this.progress >= 1) {
            this.progress = 1;
            if (!this.exploded) {
                this.exploded = true;
                audioManager.playExplosionGround();
            }
            this.y = this.targetY; // snap to target
            this.x = this.targetX;
        }
    }

    draw(ctx) {
        if (this.exploded) {
            // Draw explosion
            ctx.fillStyle = '#f80';
            ctx.beginPath();
            ctx.arc(this.x, this.y, 20 + this.explosionTimer, 0, Math.PI * 2);
            ctx.fill();
            
            ctx.fillStyle = '#ff0';
            ctx.beginPath();
            ctx.arc(this.x, this.y, 10 + this.explosionTimer * 0.5, 0, Math.PI * 2);
            ctx.fill();
        } else {
            // Draw falling bomb
            const currentSize = this.size * (1 - this.progress * 0.5); // Gets smaller as it falls
            ctx.fillStyle = '#f0f';
            ctx.beginPath();
            ctx.arc(this.x, this.y, currentSize, 0, Math.PI * 2);
            ctx.fill();
        }
    }
}

class Enemy {
    constructor(x, y, type) {
        this.x = x;
        this.y = y;
        this.type = type; // 'air' or 'ground'
        this.markedForDeletion = false;
        
        if (type === 'air') {
            this.width = 24;
            this.height = 24;
            this.color = '#f00';
            this.speed = 2;
            this.angle = 0;
            this.movementType = Math.floor(Math.random() * 3);
        } else {
            // Ground target
            this.width = 32;
            this.height = 32;
            this.color = '#a0a';
            this.speed = 1; // Scrolls with background
        }
    }

    update() {
        if (this.type === 'air') {
            switch(this.movementType) {
                case 0: // Straight down
                    this.y += this.speed;
                    break;
                case 1: // Sine wave
                    this.y += this.speed * 0.8;
                    this.x += Math.sin(this.y * 0.05) * 3;
                    break;
                case 2: // Diagonal
                    this.y += this.speed;
                    this.x += this.speed * (this.x > 240 ? -0.5 : 0.5);
                    break;
            }
        } else {
            // Ground targets just scroll down
            this.y += this.speed;
        }

        if (this.y > 700) {
            this.markedForDeletion = true;
        }
    }

    draw(ctx) {
        ctx.fillStyle = this.color;
        if (this.type === 'air') {
            // Draw UFO shape
            ctx.beginPath();
            ctx.ellipse(this.x, this.y, this.width/2, this.height/4, 0, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = '#0f0'; // cockpit
            ctx.beginPath();
            ctx.arc(this.x, this.y - 4, this.width/4, 0, Math.PI * 2);
            ctx.fill();
        } else {
            // Draw ground pyramid/base
            ctx.fillRect(this.x - this.width/2, this.y - this.height/2, this.width, this.height);
            ctx.fillStyle = '#f00';
            ctx.beginPath();
            ctx.arc(this.x, this.y, 8, 0, Math.PI * 2);
            ctx.fill();
        }
    }
}

class Background {
    constructor(width, height) {
        this.width = width;
        this.height = height;
        this.y = 0;
        this.speed = 1;
        // Generate some basic terrain elements
        this.elements = [];
        for (let i = 0; i < 20; i++) {
            this.elements.push({
                x: Math.random() * width,
                y: Math.random() * height * 2 - height, // Pre-fill above and current screen
                size: Math.random() * 40 + 20,
                color: Math.random() > 0.5 ? '#030' : '#040' // Forest greens
            });
        }
        
        // Add a "river"
        this.riverPoints = [];
        for (let i = 0; i < 20; i++) {
            this.riverPoints.push(Math.sin(i * 0.5) * 40 + width / 2);
        }
    }

    update() {
        this.y += this.speed;
        if (this.y > this.height) {
            this.y = 0;
        }
        
        // Recycle terrain elements
        this.elements.forEach(el => {
            el.y += this.speed;
            if (el.y > this.height) {
                el.y = -el.size;
                el.x = Math.random() * this.width;
            }
        });
    }

    draw(ctx) {
        // Base ground color
        ctx.fillStyle = '#002200';
        ctx.fillRect(0, 0, this.width, this.height);

        // Draw terrain elements (trees/forests)
        this.elements.forEach(el => {
            ctx.fillStyle = el.color;
            ctx.fillRect(el.x, el.y, el.size, el.size);
            // Draw wrap-around for smooth scrolling
            ctx.fillRect(el.x, el.y - this.height, el.size, el.size);
        });

        // Draw River
        ctx.strokeStyle = '#0000aa';
        ctx.lineWidth = 30;
        ctx.lineJoin = 'round';
        
        ctx.beginPath();
        for(let i=0; i < 20; i++) {
            let ry = (i * (this.height/10)) + this.y % (this.height/10);
            if (i === 0) ctx.moveTo(this.riverPoints[i], ry);
            else ctx.lineTo(this.riverPoints[i], ry);
        }
        ctx.stroke();
    }
}
