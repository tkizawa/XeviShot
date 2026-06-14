import io
import wave
import struct
import math
import random
import pygame

class AudioManager:
    def __init__(self):
        self.sounds = {}
        self.sound_enabled = False

    def init(self):
        try:
            if not pygame.mixer.get_init():
                pygame.mixer.init(frequency=44100, size=-16, channels=2, buffer=512)
            self.sound_enabled = True
            self.generate_sounds()
        except Exception as e:
            print("Failed to init audio:", e)
            self.sound_enabled = False

    def generate_sounds(self):
        self.sounds['laser'] = self._create_sound(self._gen_laser())
        self.sounds['bomb_launch'] = self._create_sound(self._gen_bomb_launch())
        self.sounds['explosion_air'] = self._create_sound(self._gen_explosion(0.2, high=True))
        self.sounds['explosion_ground'] = self._create_sound(self._gen_explosion(0.4, high=False))
        self.sounds['player_hit'] = self._create_sound(self._gen_player_hit())
        self.sounds['start_jingle'] = self._create_sound(self._gen_start_jingle())
        self.sounds['game_over'] = self._create_sound(self._gen_game_over())

    def _create_sound(self, samples):
        # Convert to 16-bit PCM WAV in memory
        wav_io = io.BytesIO()
        with wave.open(wav_io, 'w') as wav_file:
            wav_file.setnchannels(1)
            wav_file.setsampwidth(2)
            wav_file.setframerate(44100)
            
            max_amp = 32767.0
            packed = b''.join(struct.pack('<h', max(-32768, min(32767, int(s * max_amp)))) for s in samples)
            wav_file.writeframes(packed)
        
        wav_io.seek(0)
        return pygame.mixer.Sound(wav_io)

    def _gen_laser(self):
        samples = []
        sample_rate = 44100
        duration = 0.13
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            freq = 800 * math.exp(-15 * t)
            phase = 2 * math.pi * freq * t
            val = 1.0 if math.sin(phase) > 0 else -1.0
            vol = 0.08 * math.exp(-20 * t)
            samples.append(val * vol)
        return samples

    def _gen_bomb_launch(self):
        samples = []
        sample_rate = 44100
        duration = 0.31
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            freq = 260 - (200 * (t / duration))
            phase = freq * t
            val = 2.0 * (phase - math.floor(phase + 0.5))
            vol = 0.06 * math.exp(-10 * t)
            samples.append(val * vol)
        return samples

    def _gen_explosion(self, duration, high=True):
        samples = []
        sample_rate = 44100
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            val = random.uniform(-1.0, 1.0)
            vol = (0.12 if high else 0.2) * math.exp(-15 * t)
            samples.append(val * vol)
        return samples

    def _gen_player_hit(self):
        samples = []
        sample_rate = 44100
        duration = 0.62
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            noise = random.uniform(-1.0, 1.0)
            freq = 180 - (150 * (t / duration))
            phase = freq * t
            tri = 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0
            
            vol_noise = 0.25 * math.exp(-10 * t)
            vol_tri = 0.2 * math.exp(-10 * t)
            
            samples.append(noise * vol_noise + tri * vol_tri)
        return samples

    def _gen_tone(self, freq, duration, type='square', vol_start=0.05):
        samples = []
        sample_rate = 44100
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            phase = 2 * math.pi * freq * t
            if type == 'square':
                val = 1.0 if math.sin(phase) > 0 else -1.0
            else:
                phase_norm = freq * t
                val = 2.0 * abs(2.0 * (phase_norm - math.floor(phase_norm + 0.5))) - 1.0
            vol = vol_start * math.exp(-15 * t)
            samples.append(val * vol)
        return samples

    def _gen_start_jingle(self):
        samples = []
        notes = [(523.25, 0.08), (659.25, 0.08), (783.99, 0.08), (1046.50, 0.18)]
        for f, d in notes:
            samples.extend(self._gen_tone(f, d, 'square', 0.05))
        return samples

    def _gen_game_over(self):
        samples = []
        notes = [(392.00, 0.15), (329.63, 0.15), (261.63, 0.20), (246.94, 0.40)]
        for f, d in notes:
            samples.extend(self._gen_tone(f, d, 'triangle', 0.08))
        return samples

    def play(self, name):
        if self.sound_enabled and name in self.sounds:
            self.sounds[name].play()

audio_manager = AudioManager()
