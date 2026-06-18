import pygame
import math
import random
from audio import audio_manager

class Player:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.width = 30
        self.height = 30
        self.speed = 4
        self.color = (0, 255, 255)
        self.cooldown_air = 0
        self.cooldown_ground = 0
        self.reticle_distance = 120
        # Charge Hadouho states
        self.charge_timer = 0
        self.charge_max = 60
        self.was_z_pressed = False
        self.shoot_normal = False
        self.shoot_wave = False
        self.played_complete = False
        self.shield_count = 0
        self.weapon_level = 1

    def update(self, keys, canvas_width, canvas_height):
        if keys.get(pygame.K_UP): self.y -= self.speed
        if keys.get(pygame.K_DOWN): self.y += self.speed
        if keys.get(pygame.K_LEFT): self.x -= self.speed
        if keys.get(pygame.K_RIGHT): self.x += self.speed

        self.x = max(self.width/2, min(canvas_width - self.width/2, self.x))
        self.y = max(self.height/2, min(canvas_height - self.height/2, self.y))

        if self.cooldown_air > 0: self.cooldown_air -= 1
        if self.cooldown_ground > 0: self.cooldown_ground -= 1

        # Hadouho charging input logic
        z_pressed = keys.get(pygame.K_z, False) or keys.get(pygame.K_c, False)
        
        if z_pressed:
            if not self.was_z_pressed:
                if self.cooldown_air <= 0:
                    self.shoot_normal = True
            
            self.charge_timer = min(self.charge_max, self.charge_timer + 1)
            
            if self.charge_timer == 1:
                audio_manager.play_charge()
            elif self.charge_timer == self.charge_max:
                if not getattr(self, 'played_complete', False):
                    audio_manager.play('charge_complete')
                    self.played_complete = True
        else:
            if self.was_z_pressed:
                audio_manager.stop_charge()
                if self.charge_timer >= self.charge_max:
                    self.shoot_wave = True
                self.charge_timer = 0
                self.played_complete = False
                
        self.was_z_pressed = z_pressed

    def draw(self, surface):
        # Draw charging aura if charging
        if self.charge_timer > 0:
            charge_ratio = self.charge_timer / self.charge_max
            pulse = math.sin(pygame.time.get_ticks() * 0.02) * 5
            radius = int(25 * charge_ratio + 5 + pulse)
            
            if charge_ratio >= 1.0:
                color = (255, 255, 255) if (pygame.time.get_ticks() // 50) % 2 == 0 else (0, 255, 255)
                pygame.draw.circle(surface, color, (int(self.x), int(self.y)), radius, 2)
                pygame.draw.circle(surface, (0, 170, 255), (int(self.x), int(self.y)), radius - 4, 1)
            else:
                color = (0, 100, 255)
                pygame.draw.circle(surface, color, (int(self.x), int(self.y)), radius, 1)

        points = [
            (self.x, self.y - self.height / 2),
            (self.x + self.width / 2, self.y + self.height / 2),
            (self.x, self.y + self.height / 4),
            (self.x - self.width / 2, self.y + self.height / 2)
        ]
        pygame.draw.polygon(surface, self.color, points)

        if getattr(self, 'shield_count', 0) > 0:
            pygame.draw.circle(surface, (0, 255, 255), (int(self.x), int(self.y)), int(self.width / 2 + 8), 2)
            for i in range(self.shield_count):
                angle = i * (2 * math.pi / self.shield_count) + pygame.time.get_ticks() * 0.005
                hx = self.x + math.cos(angle) * (self.width / 2 + 8)
                hy = self.y + math.sin(angle) * (self.width / 2 + 8)
                pygame.draw.circle(surface, (255, 255, 255), (int(hx), int(hy)), 3)

        self.draw_reticle(surface)

    def draw_reticle(self, surface):
        reticle_x = self.x
        reticle_y = self.y - self.reticle_distance
        red = (255, 0, 0)
        
        pygame.draw.line(surface, red, (reticle_x - 10, reticle_y), (reticle_x + 10, reticle_y), 2)
        pygame.draw.line(surface, red, (reticle_x, reticle_y - 10), (reticle_x, reticle_y + 10), 2)
        pygame.draw.rect(surface, red, (reticle_x - 8, reticle_y - 8, 16, 16), 2)

class Bullet:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.width = 4
        self.height = 12
        self.speed = 10
        self.color = (255, 255, 0)
        self.marked_for_deletion = False

    def update(self):
        self.y -= self.speed
        if self.y < 0: self.marked_for_deletion = True

    def draw(self, surface):
        pygame.draw.rect(surface, self.color, (self.x - self.width / 2, self.y - self.height / 2, self.width, self.height))

class EnemyBullet:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.width = 6
        self.height = 6
        self.speed = 5
        self.color = (255, 100, 100)
        self.marked_for_deletion = False

    def update(self):
        self.y += self.speed
        if self.y > 700: self.marked_for_deletion = True

    def draw(self, surface):
        pygame.draw.circle(surface, self.color, (int(self.x), int(self.y)), int(self.width / 2))

class WaveCannon:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.width = 32
        self.height = 32
        self.speed = 12
        self.color = (0, 255, 255)
        self.marked_for_deletion = False

    def update(self):
        self.y -= self.speed
        if self.y < -50: self.marked_for_deletion = True

    def draw(self, surface):
        pulse = int(math.sin(pygame.time.get_ticks() * 0.05) * 4)
        pygame.draw.circle(surface, (255, 255, 255), (int(self.x), int(self.y)), 12 + pulse)
        pygame.draw.circle(surface, (0, 170, 255), (int(self.x), int(self.y)), 22 + pulse, 3)
        pygame.draw.ellipse(surface, (0, 255, 255), (self.x - 30, self.y - 8, 60, 16), 2)

class Bomb:
    def __init__(self, start_x, start_y, target_x, target_y):
        self.x = start_x
        self.y = start_y
        self.target_x = target_x
        self.target_y = target_y
        
        self.progress = 0.0
        self.speed = 0.03
        self.size = 10
        self.marked_for_deletion = False
        self.exploded = False
        self.explosion_timer = 0

    def update(self):
        if self.exploded:
            self.explosion_timer += 1
            if self.explosion_timer > 15:
                self.marked_for_deletion = True
            return

        self.progress += self.speed
        
        self.x = self.x + (self.target_x - self.x) * 0.1
        self.y = self.y + (self.target_y - self.y) * self.progress

        if self.progress >= 1.0:
            self.progress = 1.0
            if not self.exploded:
                self.exploded = True
                audio_manager.play('explosion_ground')
            self.y = self.target_y
            self.x = self.target_x

    def draw(self, surface):
        if self.exploded:
            pygame.draw.circle(surface, (255, 136, 0), (int(self.x), int(self.y)), 20 + self.explosion_timer)
            pygame.draw.circle(surface, (255, 255, 0), (int(self.x), int(self.y)), int(10 + self.explosion_timer * 0.5))
        else:
            current_size = max(1, int(self.size * (1 - self.progress * 0.5)))
            pygame.draw.circle(surface, (255, 0, 255), (int(self.x), int(self.y)), current_size)

class Enemy:
    def __init__(self, x, y, type_):
        self.x = x
        self.y = y
        self.type = type_
        self.marked_for_deletion = False
        self.shoot_timer = random.randint(30, 120)
        self.shoot_now = False
        
        if type_ == 'air':
            self.width = 24
            self.height = 24
            self.color = (255, 0, 0)
            self.speed = 2
            self.movement_type = random.randint(0, 2)
        else:
            self.width = 32
            self.height = 32
            self.color = (170, 0, 170)
            self.speed = 1

    def update(self):
        if self.type == 'air':
            self.shoot_timer -= 1
            if self.shoot_timer <= 0:
                self.shoot_now = True
                self.shoot_timer = random.randint(60, 180)

            if self.movement_type == 0:
                self.y += self.speed
            elif self.movement_type == 1:
                self.y += self.speed * 0.8
                self.x += math.sin(self.y * 0.05) * 3
            elif self.movement_type == 2:
                self.y += self.speed
                self.x += self.speed * (-0.5 if self.x > 240 else 0.5)
        else:
            self.y += self.speed

        if self.y > 700:
            self.marked_for_deletion = True

    def draw(self, surface):
        if self.type == 'air':
            rect = (self.x - self.width/2, self.y - self.height/4, self.width, self.height/2)
            pygame.draw.ellipse(surface, self.color, rect)
            pygame.draw.circle(surface, (0, 255, 0), (int(self.x), int(self.y - 4)), int(self.width/4))
        else:
            pygame.draw.rect(surface, self.color, (self.x - self.width/2, self.y - self.height/2, self.width, self.height))
            pygame.draw.circle(surface, (255, 0, 0), (int(self.x), int(self.y)), 8)

class Background:
    def __init__(self, width, height):
        self.width = width
        self.height = height
        self.y = 0
        self.speed = 1
        
        self.elements = []
        for _ in range(20):
            self.elements.append({
                'x': random.uniform(0, width),
                'y': random.uniform(-height, height),
                'size': random.uniform(20, 60),
                'color': (0, 51, 0) if random.random() > 0.5 else (0, 68, 0)
            })
            
        self.river_points = [math.sin(i * 0.5) * 40 + width / 2 for i in range(20)]

    def update(self):
        self.y += self.speed
        if self.y > self.height:
            self.y = 0
            
        for el in self.elements:
            el['y'] += self.speed
            if el['y'] > self.height:
                el['y'] = -el['size']
                el['x'] = random.uniform(0, self.width)

    def draw(self, surface):
        surface.fill((0, 34, 0))

        for el in self.elements:
            color = el['color']
            x, y, size = el['x'], el['y'], el['size']
            pygame.draw.rect(surface, color, (x, y, size, size))
            pygame.draw.rect(surface, color, (x, y - self.height, size, size))
            
        points = []
        for i in range(20):
            ry = (i * (self.height / 10)) + self.y % (self.height / 10)
            points.append((self.river_points[i], ry))
            
        if len(points) > 1:
            pygame.draw.lines(surface, (0, 0, 170), False, points, 30)

class ShieldCapsule:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.width = 16
        self.height = 16
        self.speed = 1.5
        self.marked_for_deletion = False

    def update(self):
        self.y += self.speed
        if self.y > 700: self.marked_for_deletion = True

    def draw(self, surface):
        pygame.draw.ellipse(surface, (0, 200, 255), (self.x - self.width / 2, self.y - self.height / 2, self.width, self.height))
        pygame.draw.circle(surface, (255, 255, 255), (int(self.x - 2), int(self.y - 2)), 3)

class WeaponCapsule:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.width = 16
        self.height = 16
        self.speed = 1.5
        self.marked_for_deletion = False

    def update(self):
        self.y += self.speed
        if self.y > 700: self.marked_for_deletion = True

    def draw(self, surface):
        pygame.draw.ellipse(surface, (255, 50, 50), (self.x - self.width / 2, self.y - self.height / 2, self.width, self.height))
        pygame.draw.circle(surface, (255, 255, 255), (int(self.x - 2), int(self.y - 2)), 3)

