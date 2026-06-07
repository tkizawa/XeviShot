class AudioManager {
    constructor() {
        this.ctx = null;
        this.noiseBuffer = null;
        this.soundEnabled = true;
    }

    init() {
        if (this.ctx) return;
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) {
                console.warn("Web Audio API is not supported in this browser.");
                this.soundEnabled = false;
                return;
            }
            this.ctx = new AudioContext();
            this.createNoiseBuffer();
        } catch (e) {
            console.error("Failed to initialize AudioContext:", e);
            this.soundEnabled = false;
        }
    }

    createNoiseBuffer() {
        if (!this.ctx) return;
        const bufferSize = this.ctx.sampleRate * 1.5; // 1.5 seconds of noise
        this.noiseBuffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
        const data = this.noiseBuffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
            data[i] = Math.random() * 2 - 1;
        }
    }

    resumeContext() {
        this.init();
        if (this.ctx && this.ctx.state === 'suspended') {
            this.ctx.resume();
        }
    }

    playLaser() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx) return;

        const now = this.ctx.currentTime;
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        // Retro square wave sound
        osc.type = 'square';
        osc.frequency.setValueAtTime(800, now);
        osc.frequency.exponentialRampToValueAtTime(120, now + 0.12);

        // Quick volume decay
        gain.gain.setValueAtTime(0.08, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 0.13);
    }

    playBombLaunch() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx) return;

        const now = this.ctx.currentTime;
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        // Sawtooth wave for a descending "whistle"
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(260, now);
        osc.frequency.linearRampToValueAtTime(60, now + 0.3);

        // Fade out
        gain.gain.setValueAtTime(0.06, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 0.31);
    }

    playExplosionAir() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx || !this.noiseBuffer) return;

        const now = this.ctx.currentTime;
        const noise = this.ctx.createBufferSource();
        noise.buffer = this.noiseBuffer;

        // Bandpass filter to make it sound crunchy/metallic
        const filter = this.ctx.createBiquadFilter();
        filter.type = 'bandpass';
        filter.frequency.setValueAtTime(1200, now);
        filter.frequency.exponentialRampToValueAtTime(150, now + 0.18);

        const gain = this.ctx.createGain();
        gain.gain.setValueAtTime(0.12, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.18);

        noise.connect(filter);
        filter.connect(gain);
        gain.connect(this.ctx.destination);

        noise.start(now);
        noise.stop(now + 0.2);
    }

    playExplosionGround() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx || !this.noiseBuffer) return;

        const now = this.ctx.currentTime;
        const noise = this.ctx.createBufferSource();
        noise.buffer = this.noiseBuffer;

        // Lowpass filter to make it a deep rumble
        const filter = this.ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(350, now);
        filter.frequency.linearRampToValueAtTime(40, now + 0.4);

        const gain = this.ctx.createGain();
        gain.gain.setValueAtTime(0.2, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.4);

        noise.connect(filter);
        filter.connect(gain);
        gain.connect(this.ctx.destination);

        noise.start(now);
        noise.stop(now + 0.42);
    }

    playPlayerHit() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx || !this.noiseBuffer) return;

        const now = this.ctx.currentTime;

        // 1. Noise crash component
        const noise = this.ctx.createBufferSource();
        noise.buffer = this.noiseBuffer;

        const filter = this.ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(500, now);
        filter.frequency.linearRampToValueAtTime(50, now + 0.6);

        const noiseGain = this.ctx.createGain();
        noiseGain.gain.setValueAtTime(0.25, now);
        noiseGain.gain.exponentialRampToValueAtTime(0.001, now + 0.6);

        noise.connect(filter);
        filter.connect(noiseGain);
        noiseGain.connect(this.ctx.destination);

        // 2. Low sine/triangle sweep for heavy base impact
        const osc = this.ctx.createOscillator();
        const oscGain = this.ctx.createGain();

        osc.type = 'triangle';
        osc.frequency.setValueAtTime(180, now);
        osc.frequency.linearRampToValueAtTime(30, now + 0.6);

        oscGain.gain.setValueAtTime(0.2, now);
        oscGain.gain.exponentialRampToValueAtTime(0.001, now + 0.6);

        osc.connect(oscGain);
        oscGain.connect(this.ctx.destination);

        noise.start(now);
        osc.start(now);
        
        noise.stop(now + 0.62);
        osc.stop(now + 0.62);
    }

    playStartJingle() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx) return;

        const now = this.ctx.currentTime;
        const playTone = (freq, startOffset, duration) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'square';
            osc.frequency.setValueAtTime(freq, now + startOffset);

            gain.gain.setValueAtTime(0.05, now + startOffset);
            gain.gain.exponentialRampToValueAtTime(0.002, now + startOffset + duration);

            osc.connect(gain);
            gain.connect(this.ctx.destination);

            osc.start(now + startOffset);
            osc.stop(now + startOffset + duration);
        };

        // Retro arpeggio (C5 -> E5 -> G5 -> C6)
        playTone(523.25, 0, 0.08);     // C5
        playTone(659.25, 0.08, 0.08);  // E5
        playTone(783.99, 0.16, 0.08);  // G5
        playTone(1046.50, 0.24, 0.18); // C6
    }

    playGameOver() {
        this.resumeContext();
        if (!this.soundEnabled || !this.ctx) return;

        const now = this.ctx.currentTime;
        const playTone = (freq, startOffset, duration) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'triangle';
            osc.frequency.setValueAtTime(freq, now + startOffset);

            gain.gain.setValueAtTime(0.08, now + startOffset);
            gain.gain.linearRampToValueAtTime(0.002, now + startOffset + duration);

            osc.connect(gain);
            gain.connect(this.ctx.destination);

            osc.start(now + startOffset);
            osc.stop(now + startOffset + duration);
        };

        // Sad falling arpeggio
        playTone(392.00, 0, 0.15);     // G4
        playTone(329.63, 0.15, 0.15);  // E4
        playTone(261.63, 0.30, 0.20);  // C4
        playTone(246.94, 0.50, 0.40);  // B3
    }
}

// Global instance
const audioManager = new AudioManager();
