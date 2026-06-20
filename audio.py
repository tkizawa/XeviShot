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
        self.sounds['bgm'] = self._create_sound(self._gen_bgm())
        self.sounds['bgm'].set_volume(0.4)
        self.sounds['opening_bgm'] = self._create_sound(self._gen_opening_bgm())
        self.sounds['opening_bgm'].set_volume(0.4)
        self.sounds['boss_bgm'] = self._create_sound(self._gen_boss_bgm())
        self.sounds['boss_bgm'].set_volume(0.4)
        self.sounds['charge'] = self._create_sound(self._gen_charge())
        self.sounds['charge_complete'] = self._create_sound(self._gen_charge_complete())
        self.sounds['wave_cannon'] = self._create_sound(self._gen_wave_cannon())

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

    def _gen_bgm(self):
        sample_rate = 44100
        bpm = 130
        step_duration = 60.0 / (bpm * 4)
        step_samples = int(sample_rate * step_duration)
        total_steps = 64
        total_samples = step_samples * total_steps
        samples = [0.0] * total_samples
        
        melody = [
            # Bar 1 (Am)
            76, 0, 72, 76, 0, 81, 0, 76, 0, 74, 72, 71, 0, 72, 74, 0,
            # Bar 2 (F)
            72, 0, 69, 72, 0, 77, 0, 72, 0, 71, 69, 67, 0, 69, 71, 0,
            # Bar 3 (C)
            67, 0, 64, 67, 0, 72, 0, 67, 0, 65, 64, 62, 0, 64, 65, 0,
            # Bar 4 (G)
            62, 0, 59, 62, 0, 67, 0, 71, 0, 69, 67, 66, 0, 67, 69, 0
        ]
        
        bass = [
            # Bar 1 (Am)
            45, 0, 45, 45, 0, 45, 45, 0, 45, 0, 45, 45, 0, 45, 45, 0,
            # Bar 2 (F)
            41, 0, 41, 41, 0, 41, 41, 0, 41, 0, 41, 41, 0, 41, 41, 0,
            # Bar 3 (C)
            36, 0, 36, 36, 0, 36, 36, 0, 36, 0, 36, 36, 0, 36, 36, 0,
            # Bar 4 (G)
            43, 0, 43, 43, 0, 43, 43, 0, 43, 0, 43, 43, 0, 43, 43, 0
        ]
        
        drums = [
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0
        ]
        
        for s in range(total_steps):
            start_idx = s * step_samples
            
            m_note = melody[s]
            m_freq = 0.0
            if m_note > 0:
                m_freq = 440.0 * (2.0 ** ((m_note - 69) / 12.0))
                
            b_note = bass[s]
            b_freq = 0.0
            if b_note > 0:
                b_freq = 440.0 * (2.0 ** ((b_note - 69) / 12.0))
                
            drum_type = drums[s]
            
            for i in range(step_samples):
                t = i / sample_rate
                idx = start_idx + i
                
                # Melody: Square wave with exponential decay
                if m_freq > 0.0:
                    phase = 2 * math.pi * m_freq * t
                    val = 1.0 if math.sin(phase) > 0 else -1.0
                    vol = 0.03 * math.exp(-6.0 * t)
                    samples[idx] += val * vol
                    
                # Bass: Triangle wave with decay
                if b_freq > 0.0:
                    phase = b_freq * t
                    val = 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0
                    vol = 0.05 * math.exp(-8.0 * t)
                    samples[idx] += val * vol
                    
                # Drums
                if drum_type == 1: # Kick
                    if t < 0.08:
                        phase = 10 * math.pi * (1.0 - math.exp(-30.0 * t))
                        val = math.sin(phase)
                        vol = 0.10 * math.exp(-15.0 * t)
                        samples[idx] += val * vol
                elif drum_type == 2: # Snare
                    if t < 0.12:
                        noise = random.uniform(-1.0, 1.0)
                        vol_noise = 0.03 * math.exp(-20.0 * t)
                        samples[idx] += noise * vol_noise
                        
                        phase = 180.0 * t
                        tri = 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0
                        vol_tri = 0.02 * math.exp(-15.0 * t)
                        samples[idx] += tri * vol_tri
                elif drum_type == 3: # Hi-hat
                    if t < 0.03:
                        noise = random.uniform(-1.0, 1.0)
                        vol = 0.015 * math.exp(-80.0 * t)
                        samples[idx] += noise * vol
                        
        return samples

    def play(self, name):
        if self.sound_enabled and name in self.sounds:
            self.sounds[name].play()

    def play_bgm(self):
        if self.sound_enabled and 'bgm' in self.sounds:
            self.stop_bgm()
            self.bgm_channel = self.sounds['bgm'].play(loops=-1)

    def play_boss_bgm(self):
        if self.sound_enabled and 'boss_bgm' in self.sounds:
            self.stop_bgm()
            self.boss_bgm_channel = self.sounds['boss_bgm'].play(loops=-1)

    def stop_bgm(self):
        if hasattr(self, 'bgm_channel') and self.bgm_channel:
            self.bgm_channel.stop()
            self.bgm_channel = None
        if hasattr(self, 'boss_bgm_channel') and self.boss_bgm_channel:
            self.boss_bgm_channel.stop()
            self.boss_bgm_channel = None

    def play_charge(self):
        if self.sound_enabled and 'charge' in self.sounds:
            self.stop_charge()
            self.charge_channel = self.sounds['charge'].play()

    def stop_charge(self):
        if hasattr(self, 'charge_channel') and self.charge_channel:
            self.charge_channel.stop()
            self.charge_channel = None

    def play_opening_bgm(self):
        if self.sound_enabled and 'opening_bgm' in self.sounds:
            self.stop_opening_bgm()
            self.opening_bgm_channel = self.sounds['opening_bgm'].play(loops=-1)

    def stop_opening_bgm(self):
        if hasattr(self, 'opening_bgm_channel') and self.opening_bgm_channel:
            self.opening_bgm_channel.stop()
            self.opening_bgm_channel = None

    def _gen_opening_bgm(self):
        sample_rate = 44100
        bpm = 120
        step_duration = 60.0 / (bpm * 4)
        step_samples = int(sample_rate * step_duration)
        total_steps = 32
        total_samples = step_samples * total_steps
        samples = [0.0] * total_samples
        
        melody = [
            # Bar 1 (Am)
            69, 72, 76, 72, 69, 72, 76, 72,
            # Bar 2 (G)
            67, 71, 74, 71, 67, 71, 74, 71,
            # Bar 3 (F)
            65, 69, 72, 69, 65, 69, 72, 69,
            # Bar 4 (E)
            64, 68, 71, 68, 64, 68, 71, 68
        ]
        
        bass = [
            # Bar 1 (Am)
            45, 0, 45, 45, 0, 45, 45, 0,
            # Bar 2 (G)
            43, 0, 43, 43, 0, 43, 43, 0,
            # Bar 3 (F)
            41, 0, 41, 41, 0, 41, 41, 0,
            # Bar 4 (E)
            40, 0, 40, 40, 0, 40, 40, 0
        ]
        
        drums = [
            1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0
        ]
        
        for s in range(total_steps):
            start_idx = s * step_samples
            
            m_note = melody[s]
            m_freq = 0.0
            if m_note > 0:
                m_freq = 440.0 * (2.0 ** ((m_note - 69) / 12.0))
                
            b_note = bass[s]
            b_freq = 0.0
            if b_note > 0:
                b_freq = 440.0 * (2.0 ** ((b_note - 69) / 12.0))
                
            drum_type = drums[s]
            
            for i in range(step_samples):
                t = i / sample_rate
                idx = start_idx + i
                
                # Melody: Square wave with exponential decay
                if m_freq > 0.0:
                    phase = 2 * math.pi * m_freq * t
                    val = 1.0 if math.sin(phase) > 0 else -1.0
                    vol = 0.025 * math.exp(-6.0 * t)
                    samples[idx] += val * vol
                    
                # Bass: Triangle wave with decay
                if b_freq > 0.0:
                    phase = b_freq * t
                    val = 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0
                    vol = 0.04 * math.exp(-8.0 * t)
                    samples[idx] += val * vol
                    
                # Drums
                if drum_type == 1: # Kick
                    if t < 0.08:
                        phase = 10 * math.pi * (1.0 - math.exp(-30.0 * t))
                        val = math.sin(phase)
                        vol = 0.08 * math.exp(-15.0 * t)
                        samples[idx] += val * vol
                elif drum_type == 2: # Snare
                    if t < 0.12:
                        noise = random.uniform(-1.0, 1.0)
                        vol_noise = 0.02 * math.exp(-20.0 * t)
                        samples[idx] += noise * vol_noise
                        
                        phase = 180.0 * t
                        tri = 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0
                        vol_tri = 0.015 * math.exp(-15.0 * t)
                        samples[idx] += tri * vol_tri
                elif drum_type == 3: # Hi-hat
                    if t < 0.03:
                        noise = random.uniform(-1.0, 1.0)
                        vol = 0.01 * math.exp(-80.0 * t)
                        samples[idx] += noise * vol
                        
        return samples

    def _gen_charge(self):
        samples = []
        sample_rate = 44100
        duration = 1.0
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            freq = 220.0 + 660.0 * (t / duration)
            phase = 2 * math.pi * freq * t
            val = math.sin(phase)
            vol = 0.08 * (t / duration)
            samples.append(val * vol)
        return samples

    def _gen_charge_complete(self):
        samples = []
        notes = [(880.0, 0.05), (1046.5, 0.05), (1318.5, 0.1)]
        for f, d in notes:
            samples.extend(self._gen_tone(f, d, 'square', 0.04))
        return samples

    def _gen_wave_cannon(self):
        samples = []
        sample_rate = 44100
        duration = 0.6
        for i in range(int(sample_rate * duration)):
            t = i / sample_rate
            freq = 800.0 - 600.0 * (t / duration) + 50.0 * math.sin(2.0 * math.pi * 50.0 * t)
            phase = 2.0 * math.pi * freq * t
            val = 0.7 * (1.0 if math.sin(phase) > 0.0 else -1.0) + 0.3 * random.uniform(-1.0, 1.0)
            vol = 0.25 * (1.0 - t / duration)
            samples.append(val * vol)
        return samples

    def _gen_boss_bgm(self):
        sample_rate = 44100
        bpm = 100
        step_duration = 60.0 / (bpm * 4)
        step_samples = int(sample_rate * step_duration)
        total_steps = 32
        total_samples = step_samples * total_steps
        samples = [0.0] * total_samples
        
        melody = [
            # Bar 1 (creepy diminished/chromatic)
            72, 0, 73, 0, 78, 0, 77, 0, 72, 73, 78, 77, 84, 0, 83, 0,
            # Bar 2
            72, 0, 73, 0, 78, 0, 77, 0, 84, 83, 78, 77, 73, 72, 0, 0
        ]
        
        bass = [
            # Bar 1 (ominous heavy bass)
            36, 0, 36, 36, 37, 0, 37, 37, 42, 0, 42, 42, 41, 41, 0, 0,
            # Bar 2
            36, 0, 36, 36, 37, 0, 37, 37, 42, 0, 42, 42, 41, 37, 36, 0
        ]
        
        drums = [
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0
        ]
        
        for s in range(total_steps):
            start_idx = s * step_samples
            
            m_note = melody[s]
            m_freq = 0.0
            if m_note > 0:
                m_freq = 440.0 * (2.0 ** ((m_note - 69) / 12.0))
                
            b_note = bass[s]
            b_freq = 0.0
            if b_note > 0:
                b_freq = 440.0 * (2.0 ** ((b_note - 69) / 12.0))
                
            drum_type = drums[s]
            
            for i in range(step_samples):
                t = i / sample_rate
                idx = start_idx + i
                
                # Melody: Dissonant square wave with vibrato
                if m_freq > 0.0:
                    vib = 1.0 + 0.015 * math.sin(2.0 * math.pi * 8.0 * t)
                    phase = 2 * math.pi * (m_freq * vib) * t
                    val = 1.0 if math.sin(phase) > 0 else -1.0
                    vol = 0.025 * math.exp(-4.0 * t)
                    samples[idx] += val * vol
                    
                # Bass: Low detuned triangle waves
                if b_freq > 0.0:
                    phase1 = b_freq * t
                    val1 = 2.0 * abs(2.0 * (phase1 - math.floor(phase1 + 0.5))) - 1.0
                    phase2 = b_freq * 1.01 * t
                    val2 = 2.0 * abs(2.0 * (phase2 - math.floor(phase2 + 0.5))) - 1.0
                    vol = 0.06 * math.exp(-6.0 * t)
                    samples[idx] += (val1 + val2) * 0.5 * vol
                    
                # Drums
                if drum_type == 1:
                    if t < 0.15:
                        phase = 8 * math.pi * (1.0 - math.exp(-20.0 * t))
                        val = math.sin(phase)
                        vol = 0.12 * math.exp(-8.0 * t)
                        samples[idx] += val * vol
                elif drum_type == 2:
                    if t < 0.15:
                        noise = random.uniform(-1.0, 1.0)
                        vol_noise = 0.025 * math.exp(-15.0 * t)
                        samples[idx] += noise * vol_noise
                        
                        phase = 140.0 * t
                        tri = 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0
                        vol_tri = 0.015 * math.exp(-12.0 * t)
                        samples[idx] += tri * vol_tri
                elif drum_type == 3:
                    if t < 0.04:
                        noise = random.uniform(-1.0, 1.0)
                        vol = 0.01 * math.exp(-70.0 * t)
                        samples[idx] += noise * vol
                        
        return samples

audio_manager = AudioManager()
