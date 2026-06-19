import pygame
from entities import Player, Background, Bullet, Bomb, Enemy, WaveCannon, EnemyBullet, ShieldCapsule, WeaponCapsule
from audio import audio_manager
import random
import math

class Game:
    def __init__(self, width, height, joystick=None):
        self.width = width
        self.height = height
        self.joystick = joystick
        self.player = Player(self.width / 2, self.height - 100)
        self.background = Background(self.width, self.height)
        self.keys = {}
        
        self.bullets = []
        self.enemy_bullets = []
        self.bombs = []
        self.enemies = []
        self.items = []
        
        self.score = 0
        self.lives = 3
        self.game_over = False
        
        self.enemy_spawn_timer = 0
        self.enemy_spawn_interval = 60
        
        self.ground_spawn_timer = 0
        self.ground_spawn_interval = 180

        self.frame_count = 0
        self.phaeon_spawn_timer = 0
        self.phaeon_spawn_interval = 300

    def trigger_rumble(self, low, high, duration):
        if self.joystick and hasattr(self.joystick, 'rumble'):
            try:
                self.joystick.rumble(low, high, duration)
            except Exception as e:
                pass

    def update(self):
        if self.game_over:
            return

        self.frame_count += 1
        self.background.update()
        self.player.update(self.keys, self.width, self.height)

        if getattr(self.player, 'rumble_trigger', None):
            trigger = self.player.rumble_trigger
            self.player.rumble_trigger = None
            if trigger == 'charge_complete':
                self.trigger_rumble(0.0, 0.6, 120)
            elif trigger == 'charge_complete2':
                self.trigger_rumble(0.0, 1.0, 200)

        if getattr(self.player, 'shoot_normal', False):
            self.player.shoot_normal = False
            weapon_level = getattr(self.player, 'weapon_level', 1)
            if weapon_level >= 3:
                angles = [-25, -8, 8, 25]
                offsets = [-15, -5, 5, 15]
                for angle, offset in zip(angles, offsets):
                    rad = math.radians(angle)
                    vx = 10 * math.sin(rad)
                    vy = -10 * math.cos(rad)
                    self.bullets.append(Bullet(self.player.x + offset, self.player.y - self.player.height / 2, vx, vy))
            elif weapon_level == 2:
                self.bullets.append(Bullet(self.player.x - 10, self.player.y - self.player.height / 2))
                self.bullets.append(Bullet(self.player.x + 10, self.player.y - self.player.height / 2))
            else:
                self.bullets.append(Bullet(self.player.x, self.player.y - self.player.height / 2))
            self.player.cooldown_air = 10
            audio_manager.play('laser')
            
        if getattr(self.player, 'shoot_wave', False):
            self.player.shoot_wave = False
            self.bullets.append(WaveCannon(self.player.x, self.player.y - self.player.height / 2))
            audio_manager.play('wave_cannon')
            self.trigger_rumble(0.6, 0.4, 250)
            
        if getattr(self.player, 'shoot_diffusion_wave', False):
            self.player.shoot_diffusion_wave = False
            angles = [-30, -15, 0, 15, 30]
            base_speed = 12
            for angle in angles:
                rad = math.radians(angle)
                vx = base_speed * math.sin(rad)
                vy = -base_speed * math.cos(rad)
                self.bullets.append(WaveCannon(self.player.x, self.player.y - self.player.height / 2, vx=vx, vy=vy))
            audio_manager.play('wave_cannon')
            self.trigger_rumble(0.9, 0.7, 450)
                
        if self.keys.get(pygame.K_x) or self.keys.get(pygame.K_c):
            if self.player.cooldown_ground <= 0:
                target_x = self.player.x
                target_y = self.player.y - self.player.reticle_distance
                self.bombs.append(Bomb(self.player.x, self.player.y, target_x, target_y))
                self.player.cooldown_ground = 60
                audio_manager.play('bomb_launch')

        self.enemy_spawn_timer += 1
        if self.enemy_spawn_timer > self.enemy_spawn_interval:
            self.spawn_enemy('air')
            self.enemy_spawn_timer = 0
            if self.enemy_spawn_interval > 20:
                self.enemy_spawn_interval -= 0.5
                
        self.ground_spawn_timer += 1
        if self.ground_spawn_timer > self.ground_spawn_interval:
            self.spawn_enemy('ground')
            self.ground_spawn_timer = 0

        # Phaeon spawning logic after 60 seconds (3600 frames)
        if self.frame_count > 3600:
            self.phaeon_spawn_timer += 1
            if self.phaeon_spawn_timer > self.phaeon_spawn_interval:
                self.phaeon_spawn_timer = 0
                num_phaeons = random.randint(1, 3)
                base_x = random.uniform(80, self.width - 80)
                for i in range(num_phaeons):
                    spawn_x = max(30, min(self.width - 30, base_x + (i - (num_phaeons - 1) / 2) * 40))
                    spawn_y = -50 - i * 30
                    self.enemies.append(Enemy(spawn_x, spawn_y, 'phaeon'))

        for b in self.bullets: b.update()
        for b in self.bombs:
            was_exploded = b.exploded
            b.update()
            if b.exploded and not was_exploded:
                self.trigger_rumble(0.4, 0.2, 150)
        for e in self.enemies:
            e.update()
            if getattr(e, 'shoot_now', False):
                e.shoot_now = False
                if e.type == 'phaeon':
                    dx = self.player.x - e.x
                    dy = self.player.y - e.y
                    dist = math.hypot(dx, dy)
                    if dist > 0:
                        bullet_speed = 6.0
                        vx = (dx / dist) * bullet_speed
                        vy = (dy / dist) * bullet_speed
                    else:
                        vx = 0.0
                        vy = 6.0
                    self.enemy_bullets.append(EnemyBullet(e.x, e.y, vx, vy))
                else:
                    self.enemy_bullets.append(EnemyBullet(e.x, e.y + e.height / 2))
        for eb in self.enemy_bullets: eb.update()
        for item in self.items: item.update()

        self.bullets = [b for b in self.bullets if not b.marked_for_deletion]
        self.enemy_bullets = [eb for eb in self.enemy_bullets if not eb.marked_for_deletion]
        self.bombs = [b for b in self.bombs if not b.marked_for_deletion]
        self.enemies = [e for e in self.enemies if not e.marked_for_deletion]
        self.items = [i for i in self.items if not i.marked_for_deletion]

        self.check_collisions()

    def draw(self, surface):
        self.background.draw(surface)
        
        for e in self.enemies:
            if e.type == 'ground': e.draw(surface)
            
        for b in self.bombs: b.draw(surface)
        for item in self.items: item.draw(surface)
        
        if not self.game_over:
            self.player.draw(surface)
            
        for e in self.enemies:
            if e.type in ('air', 'phaeon'): e.draw(surface)
            
        for b in self.bullets: b.draw(surface)
        for eb in self.enemy_bullets: eb.draw(surface)

    def spawn_enemy(self, type_):
        x = random.uniform(30, self.width - 30)
        y = -50
        self.enemies.append(Enemy(x, y, type_))

    def check_collisions(self):
        for bullet in self.bullets:
            for enemy in [e for e in self.enemies if e.type in ('air', 'phaeon')]:
                if self.is_colliding(bullet, enemy):
                    if not isinstance(bullet, WaveCannon):
                        bullet.marked_for_deletion = True
                    if not enemy.marked_for_deletion:
                        enemy.marked_for_deletion = True
                        rand_drop = random.random()
                        if rand_drop <= 0.10:
                            self.items.append(ShieldCapsule(enemy.x, enemy.y))
                        elif rand_drop <= 0.20:
                            self.items.append(WeaponCapsule(enemy.x, enemy.y))
                        if enemy.type == 'phaeon':
                            self.score += 500
                        else:
                            self.score += 100
                        audio_manager.play('explosion_air')
                    
            if isinstance(bullet, WaveCannon):
                for eb in self.enemy_bullets:
                    if self.is_colliding(bullet, eb):
                        eb.marked_for_deletion = True
                        self.score += 10

        for bomb in [b for b in self.bombs if b.exploded]:
            for enemy in [e for e in self.enemies if e.type == 'ground']:
                dx = bomb.x - enemy.x
                dy = bomb.y - enemy.y
                distance = (dx * dx + dy * dy) ** 0.5
                if distance < (20 + bomb.explosion_timer) + enemy.width / 2:
                    if not enemy.marked_for_deletion:
                        enemy.marked_for_deletion = True
                        rand_drop = random.random()
                        if rand_drop <= 0.10:
                            self.items.append(ShieldCapsule(enemy.x, enemy.y))
                        elif rand_drop <= 0.20:
                            self.items.append(WeaponCapsule(enemy.x, enemy.y))
                        self.score += 300

        if not self.game_over:
            for item in self.items:
                if self.is_colliding(self.player, item):
                    item.marked_for_deletion = True
                    if isinstance(item, ShieldCapsule):
                        self.player.shield_count = 5
                    elif isinstance(item, WeaponCapsule):
                        self.player.weapon_level = min(3, getattr(self.player, 'weapon_level', 1) + 1)

            for enemy in [e for e in self.enemies if e.type in ('air', 'phaeon')]:
                if self.is_colliding(self.player, enemy):
                    enemy.marked_for_deletion = True
                    self.hit_player()

            for eb in self.enemy_bullets:
                if self.is_colliding(self.player, eb):
                    eb.marked_for_deletion = True
                    self.hit_player()

    def is_colliding(self, rect1, rect2):
        return (rect1.x - rect1.width/2 < rect2.x + rect2.width/2 and
                rect1.x + rect1.width/2 > rect2.x - rect2.width/2 and
                rect1.y - rect1.height/2 < rect2.y + rect2.height/2 and
                rect1.y + rect1.height/2 > rect2.y - rect2.height/2)

    def hit_player(self):
        if getattr(self.player, 'shield_count', 0) > 0:
            self.player.shield_count -= 1
            audio_manager.play('player_hit')
            self.trigger_rumble(0.6, 0.6, 250)
        else:
            self.lose_life()

    def lose_life(self):
        self.lives -= 1
        audio_manager.play('player_hit')
        self.trigger_rumble(1.0, 1.0, 500)
        
        # Reset player charging state
        self.player.charge_timer = 0
        self.player.was_z_pressed = False
        self.player.played_complete = False
        self.player.played_complete2 = False
        self.player.shoot_diffusion_wave = False
        self.player.weapon_level = 1
        audio_manager.stop_charge()
        
        self.enemies = [e for e in self.enemies if e.y >= self.player.y - 100]
        self.enemy_bullets.clear()
        self.player.x = self.width / 2
        self.player.y = self.height - 100
        
        if self.lives <= 0:
            self.game_over = True
            audio_manager.play('game_over')
