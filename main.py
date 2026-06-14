import pygame
import sys
from game import Game
from audio import audio_manager

def main():
    pygame.init()
    audio_manager.init()
    
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
            if event.type == pygame.KEYDOWN:
                if current_state == 'TITLE' or current_state == 'GAMEOVER':
                    game = Game(width, height)
                    current_state = 'PLAYING'
                    audio_manager.play('start_jingle')

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
            
            blink_timer += 1
            if blink_timer % 60 < 30:
                p4 = small_font.render("Press ANY KEY to Start", True, (0, 255, 0))
                screen.blit(p4, (width/2 - p4.get_width()/2, height/2 + 120))
                
        elif current_state == 'PLAYING':
            game.keys = {
                pygame.K_UP: keys_pressed[pygame.K_UP],
                pygame.K_DOWN: keys_pressed[pygame.K_DOWN],
                pygame.K_LEFT: keys_pressed[pygame.K_LEFT],
                pygame.K_RIGHT: keys_pressed[pygame.K_RIGHT],
                pygame.K_x: keys_pressed[pygame.K_x],
                pygame.K_z: keys_pressed[pygame.K_z]
            }
            game.update()
            game.draw(screen)
            
            score_surf = ui_font.render(f"SCORE: {game.score}", True, (0, 255, 255))
            lives_surf = ui_font.render(f"LIVES: {game.lives}", True, (0, 255, 255))
            
            screen.blit(score_surf, (20, 20))
            screen.blit(lives_surf, (width - lives_surf.get_width() - 20, 20))
            
            if game.game_over:
                current_state = 'GAMEOVER'
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
                rs_surf = small_font.render("Press ANY KEY to Restart", True, (0, 255, 0))
                screen.blit(rs_surf, (width/2 - rs_surf.get_width()/2, height/2 + 60))

        pygame.display.flip()
        clock.tick(60)

if __name__ == "__main__":
    main()
