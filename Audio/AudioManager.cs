using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using XeviShot.Settings;

namespace XeviShot.Audio;

/// <summary>
/// メモリ上にキャッシュされた音声波形データ（float配列）
/// </summary>
public class CachedSound
{
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }

    public CachedSound(float[] audioData, int sampleRate = 44100)
    {
        AudioData = audioData;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
    }
}

/// <summary>
/// CachedSound を再生するための SampleProvider
/// </summary>
public class CachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _cachedSound;
    private long _position;

    public CachedSoundSampleProvider(CachedSound cachedSound)
    {
        _cachedSound = cachedSound;
    }

    public WaveFormat WaveFormat => _cachedSound.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public int Read(Span<float> buffer)
    {
        var availableSamples = _cachedSound.AudioData.Length - _position;
        var samplesToCopy = (int)Math.Min(availableSamples, buffer.Length);
        _cachedSound.AudioData.AsSpan((int)_position, samplesToCopy).CopyTo(buffer);
        _position += samplesToCopy;
        return samplesToCopy;
    }
}

/// <summary>
/// ループ再生用の SampleProvider
/// </summary>
public class LoopingSampleProvider : ISampleProvider
{
    private readonly CachedSound _cachedSound;
    private long _position;
    public bool IsPlaying { get; set; } = true;
    public float Volume { get; set; } = 1.0f;

    public LoopingSampleProvider(CachedSound cachedSound, float volume = 1.0f)
    {
        _cachedSound = cachedSound;
        Volume = volume;
    }

    public WaveFormat WaveFormat => _cachedSound.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public int Read(Span<float> buffer)
    {
        if (!IsPlaying || _cachedSound.AudioData.Length == 0)
        {
            buffer.Clear();
            return buffer.Length;
        }

        int totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            int bytesRequired = buffer.Length - totalBytesRead;
            int availableSamples = (int)(_cachedSound.AudioData.Length - _position);
            int samplesToCopy = Math.Min(availableSamples, bytesRequired);

            for (int i = 0; i < samplesToCopy; i++)
            {
                buffer[totalBytesRead + i] = _cachedSound.AudioData[_position + i] * Volume;
            }

            totalBytesRead += samplesToCopy;
            _position += samplesToCopy;

            if (_position >= _cachedSound.AudioData.Length)
            {
                _position = 0;
            }
        }

        return buffer.Length;
    }

    public void Stop()
    {
        IsPlaying = false;
        _position = 0;
    }
}

/// <summary>
/// 再生終了（Readが0を返した）した音源を自動的にリストから除外する高機能ミキサー
/// </summary>
public class AutoRemoveMixingSampleProvider : ISampleProvider
{
    private readonly List<ISampleProvider> _sources = new();
    private readonly object _lock = new();

    public WaveFormat WaveFormat { get; }

    public AutoRemoveMixingSampleProvider(WaveFormat waveFormat)
    {
        WaveFormat = waveFormat;
    }

    public void AddInput(ISampleProvider input)
    {
        lock (_lock)
        {
            // 最大同時発音数（32トラック）を超えた場合は古いものを安全に除外
            if (_sources.Count >= 32)
            {
                _sources.RemoveAt(0);
            }
            _sources.Add(input);
        }
    }

    public void RemoveInput(ISampleProvider? input)
    {
        if (input == null) return;
        lock (_lock)
        {
            _sources.Remove(input);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public int Read(Span<float> buffer)
    {
        buffer.Clear();

        // 一時バッファ
        float[] tempArray = new float[buffer.Length];
        var tempSpan = tempArray.AsSpan();

        lock (_lock)
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                var source = _sources[i];
                tempSpan.Clear();
                int read = source.Read(tempSpan);

                for (int j = 0; j < read; j++)
                {
                    buffer[j] += tempSpan[j];
                }

                // 再生終了（0サンプル返却）したものは自動的にミキサーから除去！
                if (read == 0)
                {
                    _sources.RemoveAt(i);
                }
            }
        }

        return buffer.Length;
    }
}

/// <summary>
/// プロシージャル波形合成と音声再生を統括するオーディオマネージャー
/// </summary>
public class AudioManager : IDisposable
{
    private static AudioManager? _instance;
    public static AudioManager Instance => _instance ??= new AudioManager();

    private IWavePlayer? _outputDevice;
    private AutoRemoveMixingSampleProvider? _mixer;
    private readonly Dictionary<string, CachedSound> _sounds = new();

    private LoopingSampleProvider? _currentBgm;
    private LoopingSampleProvider? _chargeSound;

    public bool SoundEnabled { get; private set; } = false;

    public void Initialize()
    {
        try
        {
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            _mixer = new AutoRemoveMixingSampleProvider(waveFormat);

            var waveOut = new WaveOut();
            waveOut.Init(_mixer);
            waveOut.Play();

            _outputDevice = waveOut;
            SoundEnabled = true;

            GenerateSounds();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"オーディオ初期化失敗: {ex.Message}");
            SoundEnabled = false;
        }
    }

    public void Play(string name)
    {
        if (!SoundEnabled || _mixer == null || !_sounds.TryGetValue(name, out var sound))
            return;

        try
        {
            var provider = new CachedSoundSampleProvider(sound);
            var volProvider = new VolumeSampleProvider(provider)
            {
                Volume = SettingsManager.Current.SfxVolume * SettingsManager.Current.MasterVolume
            };
            _mixer.AddInput(volProvider);
        }
        catch
        {
            // 音声再生エラーは無視してゲームを継続
        }
    }

    public void PlayBgm()
    {
        PlayLoopingBgm("bgm", 0.4f);
    }

    public void PlayOpeningBgm()
    {
        PlayLoopingBgm("opening_bgm", 0.4f);
    }

    public void PlayBossBgm()
    {
        PlayLoopingBgm("boss_bgm", 0.4f);
    }

    public void StopBgm()
    {
        if (_currentBgm != null)
        {
            _currentBgm.Stop();
            _mixer?.RemoveInput(_currentBgm);
            _currentBgm = null;
        }
    }

    public void StopOpeningBgm() => StopBgm();

    private void PlayLoopingBgm(string name, float baseVolume)
    {
        if (!SoundEnabled || _mixer == null || !_sounds.TryGetValue(name, out var sound))
            return;

        StopBgm();

        float vol = baseVolume * SettingsManager.Current.BgmVolume * SettingsManager.Current.MasterVolume;
        _currentBgm = new LoopingSampleProvider(sound, vol);
        _mixer.AddInput(_currentBgm);
    }

    public void PlayCharge()
    {
        if (!SoundEnabled || _mixer == null || !_sounds.TryGetValue("charge", out var sound))
            return;

        StopCharge();
        float vol = 0.5f * SettingsManager.Current.SfxVolume * SettingsManager.Current.MasterVolume;
        _chargeSound = new LoopingSampleProvider(sound, vol);
        _mixer.AddInput(_chargeSound);
    }

    public void StopCharge()
    {
        if (_chargeSound != null)
        {
            _chargeSound.Stop();
            _mixer?.RemoveInput(_chargeSound);
            _chargeSound = null;
        }
    }

    #region 波形生成ロジック (Python版の完全移植)
    private void GenerateSounds()
    {
        _sounds["laser"] = new CachedSound(GenLaser());
        _sounds["bomb_launch"] = new CachedSound(GenBombLaunch());
        _sounds["explosion_air"] = new CachedSound(GenExplosion(0.2, high: true));
        _sounds["explosion_ground"] = new CachedSound(GenExplosion(0.4, high: false));
        _sounds["player_hit"] = new CachedSound(GenPlayerHit());
        _sounds["start_jingle"] = new CachedSound(GenStartJingle());
        _sounds["game_over"] = new CachedSound(GenGameOver());
        _sounds["bgm"] = new CachedSound(GenBgm());
        _sounds["opening_bgm"] = new CachedSound(GenOpeningBgm());
        _sounds["boss_bgm"] = new CachedSound(GenBossBgm());
        _sounds["charge"] = new CachedSound(GenCharge());
        _sounds["charge_complete"] = new CachedSound(GenChargeComplete());
        _sounds["wave_cannon"] = new CachedSound(GenWaveCannon());
    }

    private static float[] GenLaser()
    {
        const int sampleRate = 44100;
        const double duration = 0.13;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            double freq = 800.0 * Math.Exp(-15.0 * t);
            double phase = 2.0 * Math.PI * freq * t;
            float val = Math.Sin(phase) > 0 ? 1.0f : -1.0f;
            float vol = (float)(0.08 * Math.Exp(-20.0 * t));
            samples[i] = val * vol;
        }
        return samples;
    }

    private static float[] GenBombLaunch()
    {
        const int sampleRate = 44100;
        const double duration = 0.31;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            double freq = 260.0 - (200.0 * (t / duration));
            double phase = freq * t;
            float val = (float)(2.0 * (phase - Math.Floor(phase + 0.5)));
            float vol = (float)(0.06 * Math.Exp(-10.0 * t));
            samples[i] = val * vol;
        }
        return samples;
    }

    private static float[] GenExplosion(double duration, bool high)
    {
        const int sampleRate = 44100;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];
        var rand = new Random(high ? 1234 : 5678);

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            float val = (float)(rand.NextDouble() * 2.0 - 1.0);
            float vol = (float)((high ? 0.12 : 0.2) * Math.Exp(-15.0 * t));
            samples[i] = val * vol;
        }
        return samples;
    }

    private static float[] GenPlayerHit()
    {
        const int sampleRate = 44100;
        const double duration = 0.62;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];
        var rand = new Random(42);

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
            double freq = 180.0 - (150.0 * (t / duration));
            double phase = freq * t;
            float tri = (float)(2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0);

            float volNoise = (float)(0.25 * Math.Exp(-10.0 * t));
            float volTri = (float)(0.2 * Math.Exp(-10.0 * t));
            samples[i] = noise * volNoise + tri * volTri;
        }
        return samples;
    }

    private static float[] GenTone(double freq, double duration, string type = "square", double volStart = 0.05)
    {
        const int sampleRate = 44100;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            double phase = 2.0 * Math.PI * freq * t;
            float val;
            if (type == "square")
            {
                val = Math.Sin(phase) > 0 ? 1.0f : -1.0f;
            }
            else
            {
                double phaseNorm = freq * t;
                val = (float)(2.0 * Math.Abs(2.0 * (phaseNorm - Math.Floor(phaseNorm + 0.5))) - 1.0);
            }
            float vol = (float)(volStart * Math.Exp(-15.0 * t));
            samples[i] = val * vol;
        }
        return samples;
    }

    private static float[] GenStartJingle()
    {
        var notes = new (double f, double d)[]
        {
            (523.25, 0.08), (659.25, 0.08), (783.99, 0.08), (1046.50, 0.18)
        };
        var list = new List<float>();
        foreach (var (f, d) in notes)
        {
            list.AddRange(GenTone(f, d, "square", 0.05));
        }
        return list.ToArray();
    }

    private static float[] GenGameOver()
    {
        var notes = new (double f, double d)[]
        {
            (392.00, 0.15), (329.63, 0.15), (261.63, 0.20), (246.94, 0.40)
        };
        var list = new List<float>();
        foreach (var (f, d) in notes)
        {
            list.AddRange(GenTone(f, d, "triangle", 0.08));
        }
        return list.ToArray();
    }

    private static float[] GenBgm()
    {
        const int sampleRate = 44100;
        const double bpm = 130;
        double stepDuration = 60.0 / (bpm * 4);
        int stepSamples = (int)(sampleRate * stepDuration);
        const int totalSteps = 64;
        int totalSamples = stepSamples * totalSteps;
        var samples = new float[totalSamples];

        int[] melody =
        {
            76, 0, 72, 76, 0, 81, 0, 76, 0, 74, 72, 71, 0, 72, 74, 0,
            72, 0, 69, 72, 0, 77, 0, 72, 0, 71, 69, 67, 0, 69, 71, 0,
            67, 0, 64, 67, 0, 72, 0, 67, 0, 65, 64, 62, 0, 64, 65, 0,
            62, 0, 59, 62, 0, 67, 0, 71, 0, 69, 67, 66, 0, 67, 69, 0
        };

        int[] bass =
        {
            45, 0, 45, 45, 0, 45, 45, 0, 45, 0, 45, 45, 0, 45, 45, 0,
            41, 0, 41, 41, 0, 41, 41, 0, 41, 0, 41, 41, 0, 41, 41, 0,
            36, 0, 36, 36, 0, 36, 36, 0, 36, 0, 36, 36, 0, 36, 36, 0,
            43, 0, 43, 43, 0, 43, 43, 0, 43, 0, 43, 43, 0, 43, 43, 0
        };

        int[] drums =
        {
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0
        };

        var rand = new Random(777);

        for (int s = 0; s < totalSteps; s++)
        {
            int startIdx = s * stepSamples;
            int mNote = melody[s];
            double mFreq = mNote > 0 ? 440.0 * Math.Pow(2.0, (mNote - 69) / 12.0) : 0.0;

            int bNote = bass[s];
            double bFreq = bNote > 0 ? 440.0 * Math.Pow(2.0, (bNote - 69) / 12.0) : 0.0;

            int drumType = drums[s];

            for (int i = 0; i < stepSamples; i++)
            {
                double t = (double)i / sampleRate;
                int idx = startIdx + i;

                if (mFreq > 0.0)
                {
                    double phase = 2.0 * Math.PI * mFreq * t;
                    float val = Math.Sin(phase) > 0 ? 1.0f : -1.0f;
                    float vol = (float)(0.03 * Math.Exp(-6.0 * t));
                    samples[idx] += val * vol;
                }

                if (bFreq > 0.0)
                {
                    double phase = bFreq * t;
                    float val = (float)(2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0);
                    float vol = (float)(0.05 * Math.Exp(-8.0 * t));
                    samples[idx] += val * vol;
                }

                if (drumType == 1)
                {
                    if (t < 0.08)
                    {
                        double phase = 10.0 * Math.PI * (1.0 - Math.Exp(-30.0 * t));
                        float val = (float)Math.Sin(phase);
                        float vol = (float)(0.10 * Math.Exp(-15.0 * t));
                        samples[idx] += val * vol;
                    }
                }
                else if (drumType == 2)
                {
                    if (t < 0.12)
                    {
                        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                        float volNoise = (float)(0.03 * Math.Exp(-20.0 * t));
                        samples[idx] += noise * volNoise;

                        double phase = 180.0 * t;
                        float tri = (float)(2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0);
                        float volTri = (float)(0.02 * Math.Exp(-15.0 * t));
                        samples[idx] += tri * volTri;
                    }
                }
                else if (drumType == 3)
                {
                    if (t < 0.03)
                    {
                        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                        float vol = (float)(0.015 * Math.Exp(-80.0 * t));
                        samples[idx] += noise * vol;
                    }
                }
            }
        }

        return samples;
    }

    private static float[] GenOpeningBgm()
    {
        const int sampleRate = 44100;
        const double bpm = 120;
        double stepDuration = 60.0 / (bpm * 4);
        int stepSamples = (int)(sampleRate * stepDuration);
        const int totalSteps = 32;
        int totalSamples = stepSamples * totalSteps;
        var samples = new float[totalSamples];

        int[] melody =
        {
            69, 72, 76, 72, 69, 72, 76, 72,
            67, 71, 74, 71, 67, 71, 74, 71,
            65, 69, 72, 69, 65, 69, 72, 69,
            64, 68, 71, 68, 64, 68, 71, 68
        };

        int[] bass =
        {
            45, 0, 45, 45, 0, 45, 45, 0,
            43, 0, 43, 43, 0, 43, 43, 0,
            41, 0, 41, 41, 0, 41, 41, 0,
            40, 0, 40, 40, 0, 40, 40, 0
        };

        int[] drums =
        {
            1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0
        };

        var rand = new Random(888);

        for (int s = 0; s < totalSteps; s++)
        {
            int startIdx = s * stepSamples;
            int mNote = melody[s];
            double mFreq = mNote > 0 ? 440.0 * Math.Pow(2.0, (mNote - 69) / 12.0) : 0.0;

            int bNote = bass[s];
            double bFreq = bNote > 0 ? 440.0 * Math.Pow(2.0, (bNote - 69) / 12.0) : 0.0;

            int drumType = drums[s];

            for (int i = 0; i < stepSamples; i++)
            {
                double t = (double)i / sampleRate;
                int idx = startIdx + i;

                if (mFreq > 0.0)
                {
                    double phase = 2.0 * Math.PI * mFreq * t;
                    float val = Math.Sin(phase) > 0 ? 1.0f : -1.0f;
                    float vol = (float)(0.025 * Math.Exp(-6.0 * t));
                    samples[idx] += val * vol;
                }

                if (bFreq > 0.0)
                {
                    double phase = bFreq * t;
                    float val = (float)(2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0);
                    float vol = (float)(0.04 * Math.Exp(-8.0 * t));
                    samples[idx] += val * vol;
                }

                if (drumType == 1)
                {
                    if (t < 0.08)
                    {
                        double phase = 10.0 * Math.PI * (1.0 - Math.Exp(-30.0 * t));
                        float val = (float)Math.Sin(phase);
                        float vol = (float)(0.08 * Math.Exp(-15.0 * t));
                        samples[idx] += val * vol;
                    }
                }
                else if (drumType == 2)
                {
                    if (t < 0.12)
                    {
                        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                        float volNoise = (float)(0.02 * Math.Exp(-20.0 * t));
                        samples[idx] += noise * volNoise;

                        double phase = 180.0 * t;
                        float tri = (float)(2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0);
                        float volTri = (float)(0.015 * Math.Exp(-15.0 * t));
                        samples[idx] += tri * volTri;
                    }
                }
                else if (drumType == 3)
                {
                    if (t < 0.03)
                    {
                        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                        float vol = (float)(0.01 * Math.Exp(-80.0 * t));
                        samples[idx] += noise * vol;
                    }
                }
            }
        }

        return samples;
    }

    private static float[] GenCharge()
    {
        const int sampleRate = 44100;
        const double duration = 1.0;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            double freq = 220.0 + 660.0 * (t / duration);
            double phase = 2.0 * Math.PI * freq * t;
            float val = (float)Math.Sin(phase);
            float vol = (float)(0.08 * (t / duration));
            samples[i] = val * vol;
        }
        return samples;
    }

    private static float[] GenChargeComplete()
    {
        var notes = new (double f, double d)[]
        {
            (880.0, 0.05), (1046.5, 0.05), (1318.5, 0.1)
        };
        var list = new List<float>();
        foreach (var (f, d) in notes)
        {
            list.AddRange(GenTone(f, d, "square", 0.04));
        }
        return list.ToArray();
    }

    private static float[] GenWaveCannon()
    {
        const int sampleRate = 44100;
        const double duration = 0.6;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];
        var rand = new Random(999);

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            double freq = 800.0 - 600.0 * (t / duration) + 50.0 * Math.Sin(2.0 * Math.PI * 50.0 * t);
            double phase = 2.0 * Math.PI * freq * t;
            float val = (float)(0.7 * (Math.Sin(phase) > 0.0 ? 1.0 : -1.0) + 0.3 * (rand.NextDouble() * 2.0 - 1.0));
            float vol = (float)(0.25 * (1.0 - t / duration));
            samples[i] = val * vol;
        }
        return samples;
    }

    private static float[] GenBossBgm()
    {
        const int sampleRate = 44100;
        const double bpm = 100;
        double stepDuration = 60.0 / (bpm * 4);
        int stepSamples = (int)(sampleRate * stepDuration);
        const int totalSteps = 32;
        int totalSamples = stepSamples * totalSteps;
        var samples = new float[totalSamples];

        int[] melody =
        {
            72, 0, 73, 0, 78, 0, 77, 0, 72, 73, 78, 77, 84, 0, 83, 0,
            72, 0, 73, 0, 78, 0, 77, 0, 84, 83, 78, 77, 73, 72, 0, 0
        };

        int[] bass =
        {
            36, 0, 36, 36, 37, 0, 37, 37, 42, 0, 42, 42, 41, 41, 0, 0,
            36, 0, 36, 36, 37, 0, 37, 37, 42, 0, 42, 42, 41, 37, 36, 0
        };

        int[] drums =
        {
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0,
            1, 0, 3, 0, 2, 0, 3, 0, 1, 0, 3, 0, 2, 0, 3, 0
        };

        var rand = new Random(333);

        for (int s = 0; s < totalSteps; s++)
        {
            int startIdx = s * stepSamples;
            int mNote = melody[s];
            double mFreq = mNote > 0 ? 440.0 * Math.Pow(2.0, (mNote - 69) / 12.0) : 0.0;

            int bNote = bass[s];
            double bFreq = bNote > 0 ? 440.0 * Math.Pow(2.0, (bNote - 69) / 12.0) : 0.0;

            int drumType = drums[s];

            for (int i = 0; i < stepSamples; i++)
            {
                double t = (double)i / sampleRate;
                int idx = startIdx + i;

                if (mFreq > 0.0)
                {
                    double vib = 1.0 + 0.015 * Math.Sin(2.0 * Math.PI * 8.0 * t);
                    double phase = 2.0 * Math.PI * (mFreq * vib) * t;
                    float val = Math.Sin(phase) > 0 ? 1.0f : -1.0f;
                    float vol = (float)(0.025 * Math.Exp(-4.0 * t));
                    samples[idx] += val * vol;
                }

                if (bFreq > 0.0)
                {
                    double phase1 = bFreq * t;
                    float val1 = (float)(2.0 * Math.Abs(2.0 * (phase1 - Math.Floor(phase1 + 0.5))) - 1.0);
                    double phase2 = bFreq * 1.01 * t;
                    float val2 = (float)(2.0 * Math.Abs(2.0 * (phase2 - Math.Floor(phase2 + 0.5))) - 1.0);
                    float vol = (float)(0.06 * Math.Exp(-6.0 * t));
                    samples[idx] += (val1 + val2) * 0.5f * vol;
                }

                if (drumType == 1)
                {
                    if (t < 0.15)
                    {
                        double phase = 8.0 * Math.PI * (1.0 - Math.Exp(-20.0 * t));
                        float val = (float)Math.Sin(phase);
                        float vol = (float)(0.12 * Math.Exp(-8.0 * t));
                        samples[idx] += val * vol;
                    }
                }
                else if (drumType == 2)
                {
                    if (t < 0.15)
                    {
                        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                        float volNoise = (float)(0.025 * Math.Exp(-15.0 * t));
                        samples[idx] += noise * volNoise;

                        double phase = 140.0 * t;
                        float tri = (float)(2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0);
                        float volTri = (float)(0.015 * Math.Exp(-12.0 * t));
                        samples[idx] += tri * volTri;
                    }
                }
                else if (drumType == 3)
                {
                    if (t < 0.04)
                    {
                        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                        float vol = (float)(0.01 * Math.Exp(-70.0 * t));
                        samples[idx] += noise * vol;
                    }
                }
            }
        }

        return samples;
    }
    #endregion

    public void Dispose()
    {
        StopBgm();
        StopCharge();
        _outputDevice?.Dispose();
        _outputDevice = null;
    }
}
