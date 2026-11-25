using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor y sintetizador procedural de audio (SFX + Música Chiptune/Regional)
/// para "Cuarta Transformación". Se auto-instancia en cualquier escena y genera
/// todos los AudioClips matemáticamente en memoria sin requerir archivos .wav externos.
/// Respeta automáticamente AudioListener.volume configurado en SettingsMenuUI y PauseMenu.
/// </summary>
public class ProceduralSoundManager : MonoBehaviour
{
    public static ProceduralSoundManager Instance { get; private set; }

    public enum MusicTheme
    {
        None,
        MainMenu,
        Level1,
        Level2,
        Level3,
        BossBattle,
        Finale,
        Credits
    }

    private const int SampleRate = 44100;
    private const int MusicSampleRate = 22050;
    private const int SinTableSize = 4096;
    private static readonly float[] sinTable = BuildSinTable();

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private readonly Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<MusicTheme, AudioClip> musicClips = new Dictionary<MusicTheme, AudioClip>();

    private MusicTheme currentTheme = MusicTheme.None;
    private MusicTheme savedLevelTheme = MusicTheme.Level1;
    private bool isInitialized = false;

    [Header("Volúmenes Internos (escalados por Preferencias de Usuario)")]
    [Range(0f, 1f)] public float sfxVolume = 0.72f;
    [Range(0f, 1f)] public float musicVolume = 0.36f;

    private float userMusicScale = LevelProgressionManager.DEFAULT_VOLUME;
    private float userSfxScale = LevelProgressionManager.DEFAULT_VOLUME;

    private static float[] BuildSinTable()
    {
        float[] t = new float[SinTableSize];
        for (int i = 0; i < SinTableSize; i++)
            t[i] = Mathf.Sin((i / (float)SinTableSize) * Mathf.PI * 2f);
        return t;
    }

    private static float FastSin(float turns)
    {
        float frac = turns - Mathf.Floor(turns);
        int idx = (int)(frac * SinTableSize);
        if (idx >= SinTableSize) idx -= SinTableSize;
        if (idx < 0) idx += SinTableSize;
        return sinTable[idx];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        EnsureInstance();
    }

    public static ProceduralSoundManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        ProceduralSoundManager existing = UnityEngine.Object.FindAnyObjectByType<ProceduralSoundManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("_ProceduralSoundManager");
        Instance = go.AddComponent<ProceduralSoundManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAudioSystem();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshVolumesFromPrefs();
        PlayThemeForSceneName(SceneManager.GetActiveScene().name);
    }

    private static float GetThemeVolumeMultiplier(MusicTheme theme)
    {
        return theme == MusicTheme.Finale ? 0.50f : 1.0f;
    }

    public void RefreshVolumesFromPrefs()
    {
        userMusicScale = LevelProgressionManager.GetMusicVolume();
        userSfxScale = LevelProgressionManager.GetSfxVolume();
        if (musicSource != null)
            musicSource.volume = musicVolume * userMusicScale * GetThemeVolumeMultiplier(currentTheme);
    }

    public void SetMusicVolume(float normalized)
    {
        userMusicScale = Mathf.Clamp01(normalized);
        if (musicSource != null)
            musicSource.volume = musicVolume * userMusicScale * GetThemeVolumeMultiplier(currentTheme);
    }

    public void SetSfxVolume(float normalized)
    {
        userSfxScale = Mathf.Clamp01(normalized);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayThemeForSceneName(scene.name);
    }

    private void InitializeAudioSystem()
    {
        if (isInitialized)
            return;

        userMusicScale = LevelProgressionManager.GetMusicVolume();
        userSfxScale = LevelProgressionManager.GetSfxVolume();

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume * userMusicScale;

        GenerateAllSfx();
        GenerateAllMusicThemes();
        isInitialized = true;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // API PÚBLICA DE EFECTOS DE SONIDO (SFX)
    // ═══════════════════════════════════════════════════════════════════════════

    public void PlayJump()          => PlaySfx("jump", 0.70f, 0.96f, 1.06f);
    public void PlayDash()          => PlaySfx("dash", 0.80f, 0.95f, 1.08f);
    public void PlaySwordSwing()    => PlaySfx("sword", 0.72f, 0.92f, 1.10f);
    public void PlayPlayerHit()     => PlaySfx("player_hit", 0.90f, 0.95f, 1.02f);
    public void PlayEnemyHit()      => PlaySfx("enemy_hit", 0.75f, 0.90f, 1.12f);
    public void PlayEnemyDefeat()   => PlaySfx("enemy_defeat", 0.85f, 0.95f, 1.05f);
    public void PlayCoinPickup()    => PlaySfx("coin", 0.72f, 0.98f, 1.06f);
    public void PlayHeartPickup()   => PlaySfx("heart", 0.80f, 1.00f, 1.02f);
    public void PlaySpringBounce()  => PlaySfx("spring", 0.82f, 0.96f, 1.04f);
    public void PlayBossAlert()     => PlaySfx("boss_alert", 0.95f, 1.00f, 1.00f);
    public void PlayBossShoot()     => PlaySfx("boss_shoot", 0.70f, 0.90f, 1.12f);
    public void PlayBossMelee()     => PlaySfx("boss_melee", 0.90f, 0.94f, 1.04f);
    public void PlayVictoryFanfare()
    {
        StopMusic();
        PlaySfx("victory", 0.95f, 1.00f, 1.00f);
    }
    public void PlayButtonClick()   => PlaySfx("button", 0.65f, 0.98f, 1.04f);

    // SFX adicionales para mecánicas de Niveles 2 y 3
    public void PlayPlatformCrack()  => PlaySfx("crack", 0.78f, 0.92f, 1.08f);
    public void PlayExplosion()      => PlaySfx("explosion", 0.92f, 0.92f, 1.04f);
    public void PlayLaserFlash()     => PlaySfx("laser_flash", 0.85f, 0.98f, 1.04f);
    public void PlayMegaphoneBlast() => PlaySfx("megaphone", 0.88f, 0.96f, 1.04f);
    public void PlayPortalEnter()    => PlaySfx("portal", 0.85f, 1.00f, 1.02f);
    public void PlayFireworkBurst()  => PlaySfx("firework", 0.72f, 0.85f, 1.22f);

    private void PlaySfx(string key, float volumeScale = 1f, float minPitch = 1f, float maxPitch = 1f)
    {
        if (!isInitialized)
            InitializeAudioSystem();

        if (userSfxScale <= 0.001f || sfxSource == null || !sfxClips.TryGetValue(key, out AudioClip clip) || clip == null)
            return;

        sfxSource.pitch = Mathf.Approximately(minPitch, maxPitch)
            ? minPitch
            : UnityEngine.Random.Range(minPitch, maxPitch);

        sfxSource.PlayOneShot(clip, sfxVolume * userSfxScale * volumeScale);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // API PÚBLICA DE MÚSICA DE FONDO POR NIVEL / JEFE
    // ═══════════════════════════════════════════════════════════════════════════

    public void PlayThemeForSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        string lower = sceneName.ToLowerInvariant();
        if (lower.Contains("menu"))
        {
            savedLevelTheme = MusicTheme.MainMenu;
            PlayMusicTheme(MusicTheme.MainMenu);
        }
        else if (lower.Contains("nivel2") || lower.Contains("level2"))
        {
            savedLevelTheme = MusicTheme.Level2;
            PlayMusicTheme(MusicTheme.Level2);
        }
        else if (lower.Contains("nivel3") || lower.Contains("level3"))
        {
            savedLevelTheme = MusicTheme.Level3;
            PlayMusicTheme(MusicTheme.Level3);
        }
        else
        {
            savedLevelTheme = MusicTheme.Level1;
            PlayMusicTheme(MusicTheme.Level1);
        }
    }

    public void PlayBossMusic()
    {
        PlayMusicTheme(MusicTheme.BossBattle, true);
    }

    public void PlayFinaleMusic()
    {
        PlayMusicTheme(MusicTheme.Finale);
    }

    public void PlayCreditsMusic()
    {
        PlayMusicTheme(MusicTheme.Credits);
    }

    public void RestoreLevelMusic()
    {
        PlayMusicTheme(savedLevelTheme);
    }

    public void RestartLevelMusic()
    {
        PlayMusicTheme(savedLevelTheme, true);
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
        currentTheme = MusicTheme.None;
    }

    public void PlayMusicTheme(MusicTheme theme, bool forceRestart = false)
    {
        if (!isInitialized)
            InitializeAudioSystem();

        if (musicSource == null)
            return;

        if (theme == MusicTheme.None)
        {
            StopMusic();
            return;
        }

        if (!forceRestart && currentTheme == theme && musicSource.isPlaying)
            return;

        AudioClip clip = GetOrCreateMusicClip(theme);
        if (clip != null)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            if (forceRestart && musicSource.isPlaying)
                musicSource.Stop();

            currentTheme = theme;
            musicSource.clip = clip;
            musicSource.volume = musicVolume * userMusicScale * GetThemeVolumeMultiplier(currentTheme);
            musicSource.loop = true;
            musicSource.time = 0f;
            musicSource.Play();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SÍNTESIS PROCEDURAL DE EFECTOS DE SONIDO (8-BIT / 16-BIT)
    // ═══════════════════════════════════════════════════════════════════════════

    private void GenerateAllSfx()
    {
        // Salto: barrido ascendente rápido onda cuadrada/triangular
        sfxClips["jump"] = CreateSweepClip("SFX_Jump", 0.17f, 240f, 680f, WaveType.Square, 0.45f, 0.01f, 0.14f);

        // Dash: ráfaga de viento (ruido filtrado + barrido descendente veloz)
        sfxClips["dash"] = CreateDashClip("SFX_Dash", 0.20f);

        // Espadazo: whoosh metálico breve
        sfxClips["sword"] = CreateSwordClip("SFX_Sword", 0.13f);

        // Golpe al jugador: golpe grave + ruido de impacto
        sfxClips["player_hit"] = CreateHitClip("SFX_PlayerHit", 0.28f, 190f, 65f, 0.45f);

        // Golpe a enemigo: impacto seco agudo-medio
        sfxClips["enemy_hit"] = CreateHitClip("SFX_EnemyHit", 0.14f, 440f, 160f, 0.30f);

        // Derrota de enemigo: arpegio descendente + pop
        sfxClips["enemy_defeat"] = CreateMelodyClip("SFX_EnemyDefeat", new float[] { 523.25f, 392.00f, 311.13f, 220.00f }, 0.065f, WaveType.Square, 0.48f);

        // Moneda: clásico doble tono brillante (Si5 -> Mi6)
        sfxClips["coin"] = CreateMelodyClip("SFX_Coin", new float[] { 987.77f, 1318.51f }, 0.075f, WaveType.Square, 0.42f);

        // Corazón: arpegio cálido ascendente (Do5-Mi5-Sol5-Do6)
        sfxClips["heart"] = CreateMelodyClip("SFX_Heart", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.07f, WaveType.Triangle, 0.55f);

        // Trampolín: boing elástico con vibrato ascendente
        sfxClips["spring"] = CreateSpringClip("SFX_Spring", 0.30f);

        // Alerta de jefe: fanfarria dramática de sirena/trompeta
        sfxClips["boss_alert"] = CreateMelodyClip("SFX_BossAlert", new float[] { 220f, 220f, 277.18f, 329.63f, 440f, 440f }, 0.11f, WaveType.Sawtooth, 0.52f);

        // Disparo del jefe: proyectil rápido con caída de tono
        sfxClips["boss_shoot"] = CreateSweepClip("SFX_BossShoot", 0.16f, 620f, 190f, WaveType.Sawtooth, 0.42f, 0.005f, 0.14f);

        // Golpe cuerpo a cuerpo de jefe (cucharazo / selfie stick / megáfono)
        sfxClips["boss_melee"] = CreateHitClip("SFX_BossMelee", 0.32f, 160f, 48f, 0.60f);

        // Fanfarria de victoria de fiesta patronal
        sfxClips["victory"] = CreateVictoryFanfareClip("SFX_Victory");

        // Clic de botón UI
        sfxClips["button"] = CreateSweepClip("SFX_Button", 0.07f, 660f, 880f, WaveType.Triangle, 0.40f, 0.005f, 0.06f);

        // Grieta de plataforma de Talavera
        sfxClips["crack"] = CreateHitClip("SFX_Crack", 0.18f, 720f, 220f, 0.55f);

        // Explosión de jarrón de cerámica
        sfxClips["explosion"] = CreateExplosionClip("SFX_Explosion", 0.42f);

        // Flashazo / Láser de Ring Light
        sfxClips["laser_flash"] = CreateSweepClip("SFX_LaserFlash", 0.35f, 1280f, 340f, WaveType.Sawtooth, 0.50f, 0.01f, 0.30f);

        // Megáfono del Munícipe Carismático
        sfxClips["megaphone"] = CreateMegaphoneClip("SFX_Megaphone", 0.36f);

        // Portal / Fin de nivel
        sfxClips["portal"] = CreateMelodyClip("SFX_Portal", new float[] { 440f, 554.37f, 659.25f, 880f, 1108.73f }, 0.08f, WaveType.Triangle, 0.50f);

        // Estallido festivo de fuego artificial (Fiestas Patronales)
        sfxClips["firework"] = CreateExplosionClip("SFX_Firework", 0.28f);
    }

    private enum WaveType
    {
        Sine,
        Square,
        Triangle,
        Sawtooth,
        Noise
    }

    private float SampleWave(WaveType wave, float phase, System.Random rng)
    {
        float p = phase - Mathf.Floor(phase);
        switch (wave)
        {
            case WaveType.Sine:
                return Mathf.Sin(p * Mathf.PI * 2f);
            case WaveType.Square:
                return p < 0.5f ? 1f : -1f;
            case WaveType.Triangle:
                return 4f * Mathf.Abs(p - 0.5f) - 1f;
            case WaveType.Sawtooth:
                return 2f * p - 1f;
            case WaveType.Noise:
                return (float)(rng.NextDouble() * 2.0 - 1.0);
            default:
                return 0f;
        }
    }

    private AudioClip CreateSweepClip(string clipName, float duration, float startFreq, float endFreq, WaveType wave, float amplitude, float attack, float release)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(clipName.GetHashCode());
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float norm = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, norm);
            phase += freq / SampleRate;

            float env = 1f;
            if (attack > 0f && t < attack)
                env = t / attack;
            else if (release > 0f && t > duration - release)
                env = Mathf.Clamp01((duration - t) / release);

            data[i] = SampleWave(wave, phase, rng) * amplitude * env;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateDashClip(string clipName, float duration)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(1024);
        float phase = 0f;
        float lp = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float norm = (float)i / sampleCount;
            float freq = Mathf.Lerp(780f, 180f, norm);
            phase += freq / SampleRate;

            float rawNoise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float cutoff = Mathf.Lerp(0.35f, 0.08f, norm);
            lp += cutoff * (rawNoise - lp);

            float tone = SampleWave(WaveType.Triangle, phase, rng) * 0.35f;
            float env = Mathf.Sin(norm * Mathf.PI);
            data[i] = (lp * 0.75f + tone) * env * 0.65f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateSwordClip(string clipName, float duration)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(2048);
        float phase = 0f;
        float prev = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float norm = (float)i / sampleCount;
            float freq = Mathf.Lerp(950f, 240f, norm);
            phase += freq / SampleRate;

            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float hpNoise = noise - prev * 0.75f;
            prev = noise;

            float env = Mathf.Pow(1f - norm, 1.5f);
            float metallic = SampleWave(WaveType.Square, phase, rng) * 0.25f;
            data[i] = (hpNoise * 0.55f + metallic) * env * 0.6f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateHitClip(string clipName, float duration, float startFreq, float endFreq, float noiseMix)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(clipName.GetHashCode());
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float norm = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, norm);
            phase += freq / SampleRate;

            float sq = SampleWave(WaveType.Square, phase, rng);
            float ns = SampleWave(WaveType.Noise, phase, rng);
            float env = Mathf.Pow(1f - norm, 1.8f);
            data[i] = Mathf.Lerp(sq, ns, noiseMix) * env * 0.55f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateSpringClip(string clipName, float duration)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(3030);
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float norm = (float)i / sampleCount;
            float baseFreq = Mathf.Lerp(200f, 840f, norm);
            float vibrato = Mathf.Sin(t * 95f) * 65f * (1f - norm * 0.5f);
            phase += (baseFreq + vibrato) / SampleRate;

            float env = Mathf.Clamp01(norm * 20f) * (1f - norm * 0.85f);
            data[i] = SampleWave(WaveType.Triangle, phase, rng) * env * 0.62f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateExplosionClip(string clipName, float duration)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(4096);
        float lp = 0f;
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float norm = (float)i / sampleCount;
            float rawNoise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float cutoff = Mathf.Lerp(0.45f, 0.04f, norm);
            lp += cutoff * (rawNoise - lp);

            phase += Mathf.Lerp(110f, 35f, norm) / SampleRate;
            float sub = SampleWave(WaveType.Sine, phase, rng) * 0.55f;
            float env = Mathf.Pow(1f - norm, 1.4f);

            data[i] = Mathf.Clamp((lp * 0.9f + sub) * env * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateMegaphoneClip(string clipName, float duration)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(5555);
        float p1 = 0f;
        float p2 = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float norm = (float)i / sampleCount;
            float wobble = Mathf.Sin(t * 50f) * 35f;
            p1 += (390f + wobble) / SampleRate;
            p2 += (585f - wobble * 0.5f) / SampleRate;

            float sig = SampleWave(WaveType.Sawtooth, p1, rng) * 0.5f + SampleWave(WaveType.Square, p2, rng) * 0.35f;
            float env = Mathf.Clamp01(norm * 16f) * Mathf.Pow(1f - norm, 0.7f);
            data[i] = Mathf.Clamp(sig * 1.4f, -1f, 1f) * env * 0.52f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateMelodyClip(string clipName, float[] notes, float noteDuration, WaveType wave, float amplitude)
    {
        int samplesPerNote = Mathf.RoundToInt(SampleRate * noteDuration);
        int totalSamples = samplesPerNote * notes.Length;
        float[] data = new float[totalSamples];
        System.Random rng = new System.Random(clipName.GetHashCode());

        for (int n = 0; n < notes.Length; n++)
        {
            float freq = notes[n];
            float phase = 0f;
            for (int i = 0; i < samplesPerNote; i++)
            {
                float localNorm = (float)i / samplesPerNote;
                float env = Mathf.Clamp01(localNorm * 25f) * Mathf.Clamp01((1f - localNorm) * 4f);
                phase += freq / SampleRate;
                data[n * samplesPerNote + i] = freq > 1f
                    ? SampleWave(wave, phase, rng) * amplitude * env
                    : 0f;
            }
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateVictoryFanfareClip(string clipName)
    {
        // Fanfarria triunfal de trompetas de mariachi en tercera paralela (Re Mayor -> Sol/La/Re)
        float[] freqsLead = { 587.33f, 739.99f, 880.00f, 1174.66f, 987.77f, 1174.66f };
        float[] freqsHarm = { 493.88f, 587.33f, 739.99f,  987.77f, 783.99f,  987.77f };
        float[] durs      = { 0.13f,   0.13f,   0.13f,    0.28f,   0.15f,    0.58f };

        int totalSamples = 0;
        for (int i = 0; i < durs.Length; i++)
            totalSamples += Mathf.RoundToInt(SampleRate * durs[i]);

        float[] data = new float[totalSamples];
        int cursor = 0;

        for (int n = 0; n < freqsLead.Length; n++)
        {
            int count = Mathf.RoundToInt(SampleRate * durs[n]);
            float f1 = freqsLead[n];
            float f2 = freqsHarm[n];
            float fb = f1 * 0.25f;
            float p1 = 0f, p2 = 0f, pb = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / count;
                float env = Mathf.Clamp01(norm * 24f) * Mathf.Clamp01((1f - norm) * 3.8f);
                float vib = (n == freqsLead.Length - 1) ? (1f + 0.009f * Mathf.Sin(t * 36f)) : 1f;

                p1 += (f1 * vib) / SampleRate;
                p2 += (f2 * vib) / SampleRate;
                pb += fb / SampleRate;

                float t1 = MariachiBrassSample(p1);
                float t2 = MariachiBrassSample(p2) * 0.72f;
                float gbass = FastSin(pb) * 0.45f;
                data[cursor + i] = Mathf.Clamp((t1 * 0.34f + t2 * 0.24f + gbass * 0.28f) * env, -0.95f, 0.95f);
            }
            cursor += count;
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BANDA SONORA PROCEDURAL MULTI-INSTRUMENTO (REGIONAL DOLORES HIDALGO + NCS EDM)
    // También soporta archivos locales en Assets/Resources/Music/<Theme>.mp3|.wav|.ogg
    // ═══════════════════════════════════════════════════════════════════════════

    private void GenerateAllMusicThemes()
    {
        // Pre-cargar únicamente el tema inicial para arranque instantáneo;
        // los demás temas se cargan o sintetizan al vuelo en el primer uso y quedan en caché.
        GetOrCreateMusicClip(MusicTheme.MainMenu);
    }

    private static AudioClip TryLoadCustomMusic(MusicTheme theme)
    {
        string[] aliases;
        switch (theme)
        {
            case MusicTheme.MainMenu:
                aliases = new[] { "MainMenu", "Menu", "Caminos_de_Guanajuato", "CaminosDeGuanajuato", "El_Rey", "ElRey", "JoseAlfredo_Menu" };
                break;
            case MusicTheme.Level1:
                aliases = new[] { "Level1", "Nivel1" };
                break;
            case MusicTheme.Level2:
                aliases = new[] { "Level2", "Nivel2" };
                break;
            case MusicTheme.Level3:
                aliases = new[] { "Level3", "Nivel3" };
                break;
            case MusicTheme.BossBattle:
                aliases = new[] { "BossBattle", "Boss", "Jefe" };
                break;
            case MusicTheme.Finale:
                aliases = new[] { "Finale", "Final", "15_de_septiembre", "15_de_Septiembre", "15DeSeptiembre", "Grito", "FiestaFinal" };
                break;
            case MusicTheme.Credits:
                aliases = new[] { "Credits", "Creditos", "Si_Nos_Dejan", "SiNosDejan" };
                break;
            default:
                return null;
        }

        for (int i = 0; i < aliases.Length; i++)
        {
            AudioClip loaded = Resources.Load<AudioClip>("Music/" + aliases[i]);
            if (loaded != null)
            {
                Debug.Log($"🎵 [ProceduralSoundManager] Música local cargada para {theme}: Resources/Music/{aliases[i]} ({loaded.length:F1}s)");
                return loaded;
            }
        }
        return null;
    }

    private AudioClip GetOrCreateMusicClip(MusicTheme theme)
    {
        if (theme == MusicTheme.None)
            return null;

        if (musicClips.TryGetValue(theme, out AudioClip cached) && cached != null)
            return cached;

        // 1. Prioridad a música local colocada en Assets/Resources/Music/
        AudioClip custom = TryLoadCustomMusic(theme);
        if (custom != null)
        {
            musicClips[theme] = custom;
            return custom;
        }

        // 2. Síntesis multi-instrumento (Mariachi Regional / Electrónica estilo NCS)
        AudioClip generated = null;
        switch (theme)
        {
            case MusicTheme.MainMenu:
                // Serenata Ranchera Nocturna de Dolores Hidalgo en Sol Mayor (3/4, 108 BPM)
                generated = CreateRancheraTrack(
                    "BGM_MainMenu_Ranchera",
                    108f,
                    true,
                    new int[]
                    {
                        79, 79, 83,  86, 86, 83,  81, 79, 78,  79,  0,  0,
                        81, 81, 84,  88, 86, 84,  83, 81, 79,  81,  0,  0,
                        83, 83, 86,  91, 89, 86,  84, 83, 81,  79,  0,  0,
                        81, 83, 84,  86, 84, 81,  79, 78, 79,  79,  0,  0,
                        74, 78, 81,  83, 81, 79,  76, 79, 83,  81,  0,  0
                    },
                    new int[]
                    {
                        43, 55, 59,  43, 55, 59,  45, 57, 60,  43, 55, 59,
                        45, 57, 60,  48, 60, 64,  50, 57, 62,  43, 55, 59,
                        47, 59, 62,  48, 60, 64,  45, 57, 60,  43, 55, 59,
                        45, 57, 60,  50, 57, 62,  43, 55, 59,  43, 50, 55,
                        50, 57, 62,  43, 55, 59,  48, 60, 64,  50, 57, 62
                    },
                    0.35f,
                    true,
                    false
                );
                break;

            case MusicTheme.Level1:
                // Son Alegre de Fiesta Patronal en Do Mayor (2/4, 138 BPM) — Jardín Principal
                generated = CreateRancheraTrack(
                    "BGM_Level1_FiestaSon",
                    138f,
                    false,
                    new int[]
                    {
                        72, 76, 79, 76,  81, 84, 81, 79,  77, 79, 77, 74,  72, 76, 79,  0,
                        76, 79, 84, 83,  81, 83, 81, 79,  77, 76, 74, 71,  72,  0, 72,  0,
                        84, 83, 81, 79,  81, 79, 76, 72,  74, 77, 81, 79,  77, 74, 71,  0,
                        72, 76, 79, 84,  86, 84, 81, 79,  77, 76, 74, 79,  72,  0,  0,  0
                    },
                    new int[]
                    {
                        48, 60, 48, 60,  53, 65, 48, 60,  55, 62, 55, 62,  48, 60, 55, 62,
                        48, 60, 48, 60,  53, 65, 48, 60,  50, 62, 55, 62,  48, 60, 48, 55,
                        53, 65, 48, 60,  53, 65, 48, 60,  50, 62, 53, 65,  55, 62, 43, 55,
                        48, 60, 48, 60,  53, 65, 55, 62,  50, 62, 55, 62,  48, 60, 48,  0
                    },
                    0.33f,
                    false,
                    false
                );
                break;

            case MusicTheme.Level2:
                // Electrónica Melódica estilo NCS en Re Menor (128 BPM) — Talleres de Talavera
                generated = CreateNcsElectronicTrack(
                    "BGM_Level2_NCS_Talavera",
                    128f,
                    new int[]
                    {
                        74,  0, 77, 81,  79,  0, 77, 76,  74,  0, 77, 81,  86,  0, 84, 81,
                        82,  0, 81, 79,  77,  0, 76, 74,  76, 77, 79, 81,  79, 77, 76,  0,
                        74, 77, 81, 86,  84, 81, 79, 77,  82, 81, 79, 77,  79, 81, 82,  0,
                        86,  0, 84, 81,  79, 77, 76, 74,  77, 76, 74, 73,  74,  0,  0,  0
                    },
                    new int[]
                    {
                        38, 38, 50, 38,  41, 41, 53, 41,  46, 46, 58, 46,  45, 45, 57, 45,
                        46, 46, 58, 46,  43, 43, 55, 43,  45, 45, 57, 45,  45, 45, 52, 45,
                        38, 38, 50, 38,  41, 41, 53, 41,  46, 46, 58, 46,  48, 48, 60, 48,
                        46, 46, 58, 46,  43, 43, 55, 43,  45, 45, 57, 45,  38, 38, 50, 45
                    },
                    0.34f,
                    false
                );
                break;

            case MusicTheme.Level3:
                // Electrónica Oscura Épica estilo NCS (vibra Mortals) en Mi Menor (134 BPM) — Guanajuato
                generated = CreateNcsElectronicTrack(
                    "BGM_Level3_NCS_Guanajuato",
                    134f,
                    new int[]
                    {
                        76,  0, 79, 83,  82, 83, 79, 76,  78,  0, 83, 86,  84, 83, 79,  0,
                        84,  0, 83, 81,  79,  0, 78, 76,  79, 78, 76, 75,  76,  0,  0,  0,
                        76, 79, 83, 88,  86, 83, 81, 79,  84, 83, 81, 79,  78, 79, 83,  0,
                        88,  0, 86, 83,  84, 83, 81, 79,  78, 79, 78, 75,  76,  0,  0,  0
                    },
                    new int[]
                    {
                        40, 40, 52, 40,  47, 47, 59, 47,  42, 42, 54, 42,  48, 48, 60, 48,
                        48, 48, 60, 48,  43, 43, 55, 43,  47, 47, 59, 47,  40, 40, 52, 47,
                        40, 40, 52, 40,  43, 43, 55, 43,  48, 48, 60, 48,  47, 47, 59, 47,
                        48, 48, 60, 48,  45, 45, 57, 45,  47, 47, 59, 47,  40, 40, 52, 40
                    },
                    0.35f,
                    true
                );
                break;

            case MusicTheme.BossBattle:
                // Batalla de Jefe estilo NCS Drop Intenso en La Menor (148 BPM)
                generated = CreateNcsElectronicTrack(
                    "BGM_BossBattle_NCS_Drop",
                    148f,
                    new int[]
                    {
                        69, 69, 72, 76,  75, 76, 72, 69,  71, 71, 74, 77,  76, 74, 71, 68,
                        69, 72, 76, 81,  80, 76, 74, 72,  76, 74, 72, 71,  69,  0, 76,  0,
                        81, 81, 84, 88,  87, 88, 84, 81,  79, 79, 83, 86,  84, 83, 79, 76,
                        77, 81, 84, 83,  81, 79, 77, 76,  74, 76, 74, 71,  69,  0, 76,  0
                    },
                    new int[]
                    {
                        45, 45, 57, 45,  45, 45, 57, 48,  43, 43, 55, 43,  44, 44, 56, 44,
                        45, 45, 57, 45,  48, 48, 60, 48,  50, 50, 52, 52,  45, 57, 45, 44,
                        45, 45, 57, 45,  48, 48, 60, 48,  43, 43, 55, 43,  48, 48, 60, 48,
                        41, 41, 53, 41,  43, 43, 55, 43,  44, 44, 56, 44,  45, 57, 45,  0
                    },
                    0.36f,
                    true
                );
                break;

            case MusicTheme.Finale:
                // Fiesta Patria estilo 15 de Septiembre en Dolores Hidalgo (Re Mayor 2/4, 134 BPM)
                generated = CreateRancheraTrack(
                    "BGM_Finale_15Septiembre",
                    134f,
                    false,
                    new int[]
                    {
                        74, 78, 81, 86,  86, 83, 81, 78,  79, 83, 86, 83,  81,  0, 81,  0,
                        81, 85, 88, 90,  88, 86, 85, 81,  79, 78, 76, 79,  78,  0, 74,  0,
                        86, 86, 88, 90,  91, 90, 88, 86,  83, 86, 88, 86,  81,  0, 81,  0,
                        86, 85, 83, 81,  79, 81, 83, 85,  86, 81, 78, 86,  86,  0,  0,  0
                    },
                    new int[]
                    {
                        38, 50, 45, 57,  38, 50, 45, 57,  43, 55, 43, 55,  45, 57, 45, 57,
                        45, 57, 45, 57,  38, 50, 45, 57,  43, 55, 45, 57,  38, 50, 38, 50,
                        38, 50, 42, 54,  43, 55, 43, 55,  43, 55, 45, 57,  45, 57, 45, 57,
                        38, 50, 43, 55,  45, 57, 45, 57,  38, 50, 45, 57,  38, 50, 38,  0
                    },
                    0.37f,
                    false,
                    true
                );
                break;

            case MusicTheme.Credits:
                // Serenata Nostálgica de Dolores Hidalgo (3/4 lento, 88 BPM) para los créditos estilo Star Wars
                generated = CreateRancheraTrack(
                    "BGM_Credits_CaminosDolores",
                    88f,
                    true,
                    new int[]
                    {
                        79,  0, 83,  86, 84, 83,  81,  0, 79,  79,  0,  0,
                        81,  0, 84,  88, 86, 84,  83,  0, 81,  81,  0,  0,
                        83, 84, 86,  91,  0, 89,  88, 86, 84,  83,  0,  0,
                        81, 83, 84,  86, 84, 81,  79, 78, 79,  79,  0,  0,
                        74, 78, 81,  83, 81, 79,  76, 79, 83,  81,  0,  0,
                        84, 83, 81,  79, 78, 76,  74, 78, 81,  79,  0,  0
                    },
                    new int[]
                    {
                        43, 55, 59,  43, 55, 59,  45, 57, 60,  43, 55, 59,
                        45, 57, 60,  48, 60, 64,  50, 57, 62,  50, 57, 62,
                        47, 59, 62,  48, 60, 64,  48, 60, 64,  43, 55, 59,
                        45, 57, 60,  50, 57, 62,  43, 55, 59,  43, 55, 59,
                        50, 57, 62,  43, 55, 59,  48, 60, 64,  50, 57, 62,
                        48, 60, 64,  45, 57, 60,  50, 57, 62,  43, 55, 59
                    },
                    0.35f,
                    true,
                    false
                );
                break;
        }

        if (generated != null)
            musicClips[theme] = generated;
        return generated;
    }

    private AudioClip CreateRancheraTrack(
        string clipName,
        float bpm,
        bool isWaltz34,
        int[] melodyMidi,
        int[] bassChordMidi,
        float masterAmp,
        bool warmSerenade,
        bool festiveDrums)
    {
        // En vals 3/4 cada elemento equivale a 1 negra; en son 2/4 cada elemento equivale a 1 corchea.
        float stepDuration = isWaltz34 ? (60f / bpm) : (60f / bpm) * 0.5f;
        int steps = Mathf.Min(melodyMidi.Length, bassChordMidi.Length);
        int samplesPerStep = Mathf.RoundToInt(MusicSampleRate * stepDuration);
        int totalSamples = samplesPerStep * steps;
        float[] data = new float[totalSamples];

        System.Random rng = new System.Random(clipName.GetHashCode());
        float lead1Phase = 0f;
        float lead2Phase = 0f;
        float violinPhase = 0f;
        float bassPhase = 0f;
        float strumP1 = 0f, strumP2 = 0f, strumP3 = 0f, strumP4 = 0f;

        for (int s = 0; s < steps; s++)
        {
            int midiLead = melodyMidi[s];
            int midiHarm = midiLead > 0 ? (midiLead - 4) : 0; // Dueto de mariachi en 3a/6a inferior
            float f1 = MidiToFreq(midiLead);
            float f2 = MidiToFreq(midiHarm);

            int bcMidi = bassChordMidi[s];
            float fBass = MidiToFreq(bcMidi);

            // En 3/4: tiempo 1 = guitarrón grave, tiempos 2 y 3 = rasgueo de vihuela.
            // En 2/4: pares = guitarrón, impares = contratiempo de vihuela.
            bool isBassHit = isWaltz34 ? (s % 3 == 0) : (s % 2 == 0);
            bool isStrumHit = !isBassHit && bcMidi > 0;
            bool isSnareHit = festiveDrums && (s % 4 == 2);

            float chord1 = fBass;
            float chord2 = fBass * 1.25f;
            float chord3 = fBass * 1.50f;
            float chord4 = fBass * 2.00f;

            for (int i = 0; i < samplesPerStep; i++)
            {
                float norm = (float)i / samplesPerStep;
                float t = (float)i / MusicSampleRate;

                float vib = 1f + (warmSerenade ? 0.0085f : 0.006f) * FastSin(t * 5.8f);
                float sample = 0f;

                if (f1 > 1f)
                {
                    float leadEnv = Mathf.Clamp01(norm * 18f) * Mathf.Clamp01((1f - norm) * 2.4f);
                    lead1Phase += (f1 * vib) / MusicSampleRate;
                    lead2Phase += (f2 * vib) / MusicSampleRate;
                    violinPhase += (f1 * (1f + 0.009f * FastSin(t * 6.4f + 0.4f))) / MusicSampleRate;

                    float voice1 = warmSerenade ? WarmHornSample(lead1Phase) : MariachiBrassSample(lead1Phase);
                    float voice2 = warmSerenade ? WarmHornSample(lead2Phase) : MariachiBrassSample(lead2Phase);
                    float violin = MariachiViolinSample(violinPhase);
                    sample += (voice1 * 0.36f + voice2 * 0.22f + violin * (warmSerenade ? 0.22f : 0.14f)) * leadEnv;
                }

                if (isBassHit && fBass > 1f)
                {
                    float gEnv = Mathf.Clamp01(norm * 35f) * Mathf.Pow(1f - norm, 1.35f);
                    bassPhase += fBass / MusicSampleRate;
                    // Guitarrón cálido con cuerpo grave + armónico de cuerda pulsada
                    float guitarron = FastSin(bassPhase) * 0.72f + FastSin(bassPhase * 2f) * 0.28f;
                    sample += guitarron * gEnv * 0.56f;
                }
                else if (isStrumHit)
                {
                    float sEnv = Mathf.Clamp01(norm * 45f) * Mathf.Pow(1f - norm, 2.2f);
                    strumP1 += chord1 / MusicSampleRate;
                    strumP2 += chord2 / MusicSampleRate;
                    strumP3 += chord3 / MusicSampleRate;
                    strumP4 += chord4 / MusicSampleRate;

                    float vihuela = (PluckedStringSample(strumP1) + PluckedStringSample(strumP2) + PluckedStringSample(strumP3) + PluckedStringSample(strumP4)) * 0.25f;
                    float pickClick = norm < 0.04f ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - norm / 0.04f) * 0.14f : 0f;
                    sample += (vihuela * 0.38f + pickClick) * sEnv;
                }

                if (isSnareHit && norm < 0.22f)
                {
                    // Redoblante festivo de banda/desfile del 15 de Septiembre
                    float sn = norm / 0.22f;
                    float sEnv = Mathf.Pow(1f - sn, 1.8f);
                    sample += (float)(rng.NextDouble() * 2.0 - 1.0) * sEnv * 0.20f;
                }
                else if (!warmSerenade && !isBassHit && norm < 0.10f)
                {
                    float maracaEnv = 1f - (norm / 0.10f);
                    sample += (float)(rng.NextDouble() * 2.0 - 1.0) * maracaEnv * 0.06f;
                }

                data[s * samplesPerStep + i] = Mathf.Clamp(sample * masterAmp, -0.95f, 0.95f);
            }
        }

        // Fundido mínimo de 8 ms en extremos del clip para bucle 100% libre de clics
        ApplyLoopCrossfadeSafe(data, Mathf.Min(180, totalSamples / 16));
        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, MusicSampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateNcsElectronicTrack(
        string clipName,
        float bpm,
        int[] melodyMidi,
        int[] bassMidi,
        float masterAmp,
        bool aggressiveDrop)
    {
        float stepDuration = (60f / bpm) * 0.5f; // Corcheas en 4/4 (4 pasos por compás kick)
        int steps = Mathf.Min(melodyMidi.Length, bassMidi.Length);
        int samplesPerStep = Mathf.RoundToInt(MusicSampleRate * stepDuration);
        int totalSamples = samplesPerStep * steps;
        float[] data = new float[totalSamples];

        System.Random rng = new System.Random(clipName.GetHashCode());
        float leadPhaseA = 0f, leadPhaseB = 0f, leadPhaseC = 0f;
        float padP1 = 0f, padP2 = 0f, padP3 = 0f;
        float subPhase = 0f;

        for (int s = 0; s < steps; s++)
        {
            float fLead = MidiToFreq(melodyMidi[s]);
            float fBass = MidiToFreq(bassMidi[s]);

            // Kick four-on-the-floor cada negra (s % 2 == 0) + Clap/Snare en tiempos 2 y 4 (s % 4 == 2)
            bool isKick = (s % 2 == 0);
            bool isSnare = (s % 4 == 2);
            bool isOffHat = (s % 2 == 1);

            float padRoot = fBass * 2f;
            float padThird = padRoot * 1.1892f; // 3a menor melódica
            float padFifth = padRoot * 1.4983f;

            for (int i = 0; i < samplesPerStep; i++)
            {
                float norm = (float)i / samplesPerStep;
                // Sidechain pumping clásico de NCS tras cada kick
                int stepInBeat = s % 2;
                float beatProgress = (stepInBeat + norm) * 0.5f;
                float sidechain = Mathf.Clamp(0.24f + beatProgress * 0.92f, 0.24f, 1.0f);

                float sample = 0f;

                // Supersaw / Pluck lead estilo NCS
                if (fLead > 1f)
                {
                    float leadEnv = Mathf.Clamp01(norm * 35f) * Mathf.Pow(1f - norm * 0.65f, 1.1f);
                    leadPhaseA += fLead / MusicSampleRate;
                    leadPhaseB += (fLead * 1.0065f) / MusicSampleRate;
                    leadPhaseC += (fLead * 0.9935f) / MusicSampleRate;

                    float sA = SawSample(leadPhaseA);
                    float sB = SawSample(leadPhaseB);
                    float sC = SawSample(leadPhaseC);
                    float superSaw = (sA + sB * 0.7f + sC * 0.7f) / 2.4f;
                    sample += superSaw * 0.34f * leadEnv * (aggressiveDrop ? 1.05f : 0.90f);
                }

                // Acordes Pad con sidechain + Sub-Bass EDM
                if (fBass > 1f)
                {
                    subPhase += fBass / MusicSampleRate;
                    float sub = FastSin(subPhase) * 0.44f + SawSample(subPhase) * 0.10f;
                    sample += sub * sidechain;

                    padP1 += padRoot / MusicSampleRate;
                    padP2 += padThird / MusicSampleRate;
                    padP3 += padFifth / MusicSampleRate;
                    float pad = (SawSample(padP1) + SawSample(padP2) + SawSample(padP3)) * 0.085f;
                    sample += pad * sidechain;
                }

                // Kick EDM punchy
                if (isKick && norm < 0.48f)
                {
                    float kn = norm / 0.48f;
                    float kf = Mathf.Lerp(145f, 44f, Mathf.Pow(kn, 0.45f));
                    float kEnv = Mathf.Pow(1f - kn, 1.6f);
                    sample += FastSin(kn * kf * 0.14f) * kEnv * (aggressiveDrop ? 0.52f : 0.44f);
                }

                // Clap / Snare NCS
                if (isSnare && norm < 0.35f)
                {
                    float sn = norm / 0.35f;
                    float sEnv = Mathf.Pow(1f - sn, 2.0f);
                    float clapNoise = (float)(rng.NextDouble() * 2.0 - 1.0) * sEnv * 0.24f;
                    sample += clapNoise;
                }

                // Crisp Hi-Hat offbeat
                if ((isOffHat || aggressiveDrop) && norm < 0.14f)
                {
                    float hn = norm / 0.14f;
                    float hEnv = Mathf.Pow(1f - hn, 2.2f);
                    sample += (float)(rng.NextDouble() * 2.0 - 1.0) * hEnv * (isOffHat ? 0.11f : 0.06f);
                }

                data[s * samplesPerStep + i] = Mathf.Clamp(sample * masterAmp, -0.95f, 0.95f);
            }
        }

        ApplyLoopCrossfadeSafe(data, Mathf.Min(180, totalSamples / 16));
        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, MusicSampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float MariachiBrassSample(float turns)
    {
        return FastSin(turns) * 0.56f
             + FastSin(turns * 2f) * 0.28f
             + FastSin(turns * 3f) * 0.12f
             + FastSin(turns * 4f) * 0.05f;
    }

    private static float WarmHornSample(float turns)
    {
        return FastSin(turns) * 0.68f
             + FastSin(turns * 2f) * 0.22f
             + FastSin(turns * 3f) * 0.08f;
    }

    private static float MariachiViolinSample(float turns)
    {
        float p = turns - Mathf.Floor(turns);
        float bowed = (2f * p - 1f) * 0.40f + FastSin(turns) * 0.45f + FastSin(turns * 3f) * 0.12f;
        return bowed * 0.72f;
    }

    private static float PluckedStringSample(float turns)
    {
        return FastSin(turns) * 0.55f
             + FastSin(turns * 2f) * 0.28f
             + FastSin(turns * 3f) * 0.14f;
    }

    private static float SawSample(float turns)
    {
        float p = turns - Mathf.Floor(turns);
        return 2f * p - 1f;
    }

    private static void ApplyLoopCrossfadeSafe(float[] data, int edgeSamples)
    {
        int n = data.Length;
        for (int i = 0; i < edgeSamples && i < n; i++)
        {
            float t = (float)i / edgeSamples;
            data[i] *= t;
            data[n - 1 - i] *= t;
        }
    }

    private static float MidiToFreq(int midi)
    {
        if (midi <= 0)
            return 0f;
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }
}
