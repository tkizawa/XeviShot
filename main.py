import pygame
import sys
from game import Game
from audio import audio_manager

def main():
    pygame.init()
    audio_manager.init()
    
    pygame.joystick.init()
    joystick = None
    
    width, height = 480, 640
    screen = pygame.display.set_mode((width, height))
    pygame.display.set_caption("Retro Vertical Shooter")
    clock = pygame.time.Clock()
    
    font = pygame.font.SysFont(None, 48)
    small_font = pygame.font.SysFont(None, 24)
    ui_font = pygame.font.SysFont(None, 24)
    
    current_state = 'TITLE'
    game = None
    
    blink_timer = 0
 
    while True:
        keys_pressed = pygame.key.get_pressed()
        
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                pygame.quit()
                sys.exit()
            elif event.type == pygame.JOYDEVICEADDED:
                if joystick is None:
                    joystick = pygame.joystick.Joystick(event.device_index)
                    print(f"Joystick connected: {joystick.get_name()}")
            elif event.type == pygame.JOYDEVICEREMOVED:
                if joystick and joystick.get_instance_id() == event.instance_id:
                    print("Joystick disconnected")
                    joystick = None
            elif event.type == pygame.KEYDOWN or event.type == pygame.JOYBUTTONDOWN:
                if current_state == 'TITLE' or current_state == 'GAMEOVER':
                    game = Game(width, height)
                    current_state = 'PLAYING'
                    audio_manager.play('start_jingle')
                    audio_manager.play_bgm()

        screen.fill((0, 0, 0))
        
        if current_state == 'TITLE':
            title_surf = font.render("RETRO SHOOTER", True, (255, 255, 0))
            screen.blit(title_surf, (width/2 - title_surf.get_width()/2, height/3))
            
            p1 = small_font.render("Press ARROW KEYS to move", True, (255, 255, 255))
            screen.blit(p1, (width/2 - p1.get_width()/2, height/2))
            
            p2 = small_font.render("X - Zapper (Air Attack)", True, (255, 255, 255))
            screen.blit(p2, (width/2 - p2.get_width()/2, height/2 + 30))
            
            p3 = small_font.render("Z - Blaster (Ground Attack)", True, (255, 255, 255))
            screen.blit(p3, (width/2 - p3.get_width()/2, height/2 + 60))
            
            if joystick:
                ctrl_p = small_font.render("Gamepad: L-Stick to Move / A & B to Attack", True, (0, 255, 255))
                screen.blit(ctrl_p, (width/2 - ctrl_p.get_width()/2, height/2 + 90))

            blink_timer += 1
            if blink_timer % 60 < 30:
                start_text = "Press ANY BUTTON to Start" if joystick else "Press ANY KEY to Start"
                p4 = small_font.render(start_text, True, (0, 255, 0))
                screen.blit(p4, (width/2 - p4.get_width()/2, height/2 + 120))
                
        elif current_state == 'PLAYING':
            up = keys_pressed[pygame.K_UP]
            down = keys_pressed[pygame.K_DOWN]
            left = keys_pressed[pygame.K_LEFT]
            right = keys_pressed[pygame.K_RIGHT]
            x_key = keys_pressed[pygame.K_x]
            z_key = keys_pressed[pygame.K_z]
            
            if joystick:
                axis_x = joystick.get_axis(0)
                axis_y = joystick.get_axis(1)
                
                if axis_x < -0.2:
                    left = True
                elif axis_x > 0.2:
                    right = True
                    
                if axis_y < -0.2:
                    up = True
                elif axis_y > 0.2:
                    down = True
                    
                if joystick.get_button(0):  # A Button
                    x_key = True
                if joystick.get_button(1):  # B Button
                    z_key = True
                    
            game.keys = {
                pygame.K_UP: up,
                pygame.K_DOWN: down,
                pygame.K_LEFT: left,
                pygame.K_RIGHT: right,
                pygame.K_x: x_key,
                pygame.K_z: z_key
            }
            game.update()
            game.draw(screen)
            
            score_surf = ui_font.render(f"SCORE: {game.score}", True, (0, 255, 255))
            lives_surf = ui_font.render(f"LIVES: {game.lives}", True, (0, 255, 255))
            
            screen.blit(score_surf, (20, 20))
            screen.blit(lives_surf, (width - lives_surf.get_width() - 20, 20))
            
            if game.game_over:
                current_state = 'GAMEOVER'
                audio_manager.stop_bgm()
                audio_manager.stop_charge()
                blink_timer = 0
                
        elif current_state == 'GAMEOVER':
            game.draw(screen)
            
            overlay = pygame.Surface((width, height))
            overlay.set_alpha(150)
            overlay.fill((0, 0, 0))
            screen.blit(overlay, (0, 0))
            
            go_surf = font.render("GAME OVER", True, (255, 255, 0))
            screen.blit(go_surf, (width/2 - go_surf.get_width()/2, height/3))
            
            fs_surf = small_font.render(f"FINAL SCORE: {game.score}", True, (255, 255, 255))
            screen.blit(fs_surf, (width/2 - fs_surf.get_width()/2, height/2))
            
            blink_timer += 1
            if blink_timer % 60 < 30:
                restart_text = "Press ANY BUTTON to Restart" if joystick else "Press ANY KEY to Restart"
                rs_surf = small_font.render(restart_text, True, (0, 255, 0))
                screen.blit(rs_surf, (width/2 - rs_surf.get_width()/2, height/2 + 60))

        pygame.display.flip()
        clock.tick(60)

if __name__ == "__main__":
    main()
