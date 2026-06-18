import pygame
from entities import Player, Background, Bullet, Bomb, Enemy, WaveCannon, EnemyBullet, ShieldCapsule, WeaponCapsule
from audio import audio_manager
import random
import math

class Game:
    def __init__(self, width, height):
        self.width = width
        self.height = height
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

    def update(self):
        if self.game_over:
            return

        self.background.update()
        self.player.update(self.keys, self.width, self.height)

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

        for b in self.bullets: b.update()
        for b in self.bombs: b.update()
        for e in self.enemies:
            e.update()
            if getattr(e, 'shoot_now', False):
                e.shoot_now = False
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
            if e.type == 'air': e.draw(surface)
            
        for b in self.bullets: b.draw(surface)
        for eb in self.enemy_bullets: eb.draw(surface)

    def spawn_enemy(self, type_):
        x = random.uniform(30, self.width - 30)
        y = -50
        self.enemies.append(Enemy(x, y, type_))

    def check_collisions(self):
        for bullet in self.bullets:
            for enemy in [e for e in self.enemies if e.type == 'air']:
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

            for enemy in [e for e in self.enemies if e.type == 'air']:
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
        else:
            self.lose_life()

    def lose_life(self):
        self.lives -= 1
        audio_manager.play('player_hit')
        
        # Reset player charging state
        self.player.charge_timer = 0
        self.player.was_z_pressed = False
        self.player.played_complete = False
        self.player.weapon_level = 1
        audio_manager.stop_charge()
        
        self.enemies = [e for e in self.enemies if e.y >= self.player.y - 100]
        self.enemy_bullets.clear()
        self.player.x = self.width / 2
        self.player.y = self.height - 100
        
        if self.lives <= 0:
            self.game_over = True
            audio_manager.play('game_over')
