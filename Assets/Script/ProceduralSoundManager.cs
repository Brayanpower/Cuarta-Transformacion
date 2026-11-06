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
        BossBattle
    }

    private const int SampleRate = 44100;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private readonly Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<MusicTheme, AudioClip> musicClips = new Dictionary<MusicTheme, AudioClip>();

    private MusicTheme currentTheme = MusicTheme.None;
    private MusicTheme savedLevelTheme = MusicTheme.Level1;
    private bool isInitialized = false;

    [Header("Volúmenes Internos (escalados por AudioListener.volume)")]
    [Range(0f, 1f)] public float sfxVolume = 0.72f;
    [Range(0f, 1f)] public float musicVolume = 0.28f;

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
        // Aplicar volumen guardado en PlayerPrefs si existe
        float savedVol = PlayerPrefs.GetFloat(LevelProgressionManager.VOLUME_KEY, 0.85f);
        AudioListener.volume = Mathf.Clamp01(savedVol);

        PlayThemeForSceneName(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayThemeForSceneName(scene.name);
    }

    private void InitializeAudioSystem()
    {
        if (isInitialized)
            return;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

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

        if (sfxSource == null || !sfxClips.TryGetValue(key, out AudioClip clip) || clip == null)
            return;

        sfxSource.pitch = Mathf.Approximately(minPitch, maxPitch)
            ? minPitch
            : UnityEngine.Random.Range(minPitch, maxPitch);

        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
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
        PlayMusicTheme(MusicTheme.BossBattle);
    }

    public void RestoreLevelMusic()
    {
        PlayMusicTheme(savedLevelTheme);
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
        currentTheme = MusicTheme.None;
    }

    public void PlayMusicTheme(MusicTheme theme)
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

        if (currentTheme == theme && musicSource.isPlaying)
            return;

        if (musicClips.TryGetValue(theme, out AudioClip clip) && clip != null)
        {
            currentTheme = theme;
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
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
        // Fanfarria triunfal estilo mariachi / 16-bit en Do Mayor
        float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f, 783.99f, 1046.50f };
        float[] durs  = { 0.12f,   0.12f,   0.12f,   0.26f,    0.14f,   0.52f };

        int totalSamples = 0;
        for (int i = 0; i < durs.Length; i++)
            totalSamples += Mathf.RoundToInt(SampleRate * durs[i]);

        float[] data = new float[totalSamples];
        System.Random rng = new System.Random(9999);
        int cursor = 0;

        for (int n = 0; n < freqs.Length; n++)
        {
            int count = Mathf.RoundToInt(SampleRate * durs[n]);
            float f1 = freqs[n];
            float f2 = f1 * 0.5f; // Octava inferior armónica
            float p1 = 0f, p2 = 0f;

            for (int i = 0; i < count; i++)
            {
                float norm = (float)i / count;
                float env = Mathf.Clamp01(norm * 20f) * Mathf.Clamp01((1f - norm) * 3.5f);
                p1 += f1 / SampleRate;
                p2 += f2 / SampleRate;

                float lead = SampleWave(WaveType.Square, p1, rng) * 0.36f;
                float bass = SampleWave(WaveType.Triangle, p2, rng) * 0.30f;
                data[cursor + i] = (lead + bass) * env;
            }
            cursor += count;
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SÍNTESIS PROCEDURAL DE MÚSICA DE FONDO (LOOPS CHIPTUNE / REGIONALES)
    // ═══════════════════════════════════════════════════════════════════════════

    private void GenerateAllMusicThemes()
    {
        // 1. Menú Principal: Serenata colonial alegre en Sol Mayor (118 BPM)
        musicClips[MusicTheme.MainMenu] = CreateChiptuneLoop(
            "BGM_MainMenu",
            118f,
            new int[] { 67, 71, 74, 79, 76, 74, 71, 0,  69, 72, 76, 74, 71, 69, 67, 0,
                        67, 71, 74, 79, 81, 79, 76, 74, 76, 74, 71, 69, 67, 0,  67, 0 },
            new int[] { 43, 50, 43, 50, 48, 55, 43, 50, 45, 52, 50, 57, 43, 50, 43, 50,
                        43, 50, 43, 50, 48, 55, 48, 55, 50, 57, 50, 57, 43, 50, 43, 47 },
            WaveType.Triangle,
            0.30f
        );

        // 2. Nivel 1 (Jardín Principal): Fiesta patronal viva en Do Mayor (138 BPM)
        musicClips[MusicTheme.Level1] = CreateChiptuneLoop(
            "BGM_Level1",
            138f,
            new int[] { 72, 76, 79, 76, 81, 79, 76, 72, 74, 77, 81, 79, 77, 74, 71, 0,
                        72, 76, 79, 84, 83, 81, 79, 76, 74, 76, 74, 71, 72, 0,  79, 0 },
            new int[] { 48, 55, 48, 55, 53, 60, 48, 55, 50, 57, 43, 50, 43, 50, 43, 47,
                        48, 55, 48, 55, 53, 60, 48, 55, 50, 57, 43, 50, 48, 55, 48, 52 },
            WaveType.Square,
            0.24f
        );

        // 3. Nivel 2 (Talleres de Talavera): Ritmo sincopado artesanal en Re Menor (132 BPM)
        musicClips[MusicTheme.Level2] = CreateChiptuneLoop(
            "BGM_Level2",
            132f,
            new int[] { 62, 65, 69, 70, 69, 65, 64, 0,  62, 65, 69, 74, 72, 70, 69, 0,
                        70, 74, 77, 76, 74, 72, 70, 69, 67, 69, 65, 64, 62, 0,  69, 0 },
            new int[] { 38, 45, 38, 45, 41, 48, 45, 52, 38, 45, 38, 45, 46, 53, 45, 52,
                        46, 53, 46, 53, 48, 55, 48, 55, 43, 50, 45, 52, 38, 45, 38, 41 },
            WaveType.Square,
            0.24f
        );

        // 4. Nivel 3 (Callejones de Guanajuato): Nocturno rápido y dramático en Mi Menor (148 BPM)
        musicClips[MusicTheme.Level3] = CreateChiptuneLoop(
            "BGM_Level3",
            148f,
            new int[] { 64, 67, 71, 76, 75, 71, 67, 64, 66, 71, 75, 78, 76, 71, 67, 0,
                        76, 79, 78, 76, 74, 72, 71, 69, 67, 71, 75, 71, 76, 0,  71, 0 },
            new int[] { 40, 47, 40, 47, 47, 54, 40, 47, 42, 47, 47, 54, 40, 47, 40, 47,
                        48, 55, 47, 54, 45, 52, 43, 50, 42, 47, 47, 51, 40, 47, 40, 43 },
            WaveType.Sawtooth,
            0.22f
        );

        // 5. Batalla contra Jefe: Tema veloz de alta adrenalina en La Menor (166 BPM)
        musicClips[MusicTheme.BossBattle] = CreateChiptuneLoop(
            "BGM_BossBattle",
            166f,
            new int[] { 69, 69, 72, 76, 75, 76, 72, 69, 71, 71, 74, 77, 76, 74, 71, 68,
                        69, 72, 76, 81, 80, 76, 74, 72, 76, 74, 72, 71, 69, 0,  76, 0 },
            new int[] { 45, 45, 52, 45, 45, 45, 52, 48, 43, 43, 50, 43, 44, 44, 52, 44,
                        45, 45, 52, 45, 48, 48, 55, 48, 50, 50, 52, 52, 45, 52, 45, 44 },
            WaveType.Sawtooth,
            0.25f
        );
    }

    private AudioClip CreateChiptuneLoop(string clipName, float bpm, int[] melodyMidi, int[] bassMidi, WaveType leadWave, float masterAmp)
    {
        float stepDuration = (60f / bpm) * 0.5f; // Corcheas
        int steps = Mathf.Min(melodyMidi.Length, bassMidi.Length);
        int samplesPerStep = Mathf.RoundToInt(SampleRate * stepDuration);
        int totalSamples = samplesPerStep * steps;
        float[] data = new float[totalSamples];

        System.Random rng = new System.Random(clipName.GetHashCode());
        float leadPhase = 0f;
        float bassPhase = 0f;

        for (int s = 0; s < steps; s++)
        {
            float leadFreq = MidiToFreq(melodyMidi[s]);
            float bassFreq = MidiToFreq(bassMidi[s]);
            bool isKick = (s % 4 == 0);
            bool isHat  = (s % 2 == 1);

            for (int i = 0; i < samplesPerStep; i++)
            {
                float norm = (float)i / samplesPerStep;
                float leadEnv = Mathf.Clamp01(norm * 30f) * Mathf.Clamp01((1f - norm) * 2.2f);
                float bassEnv = Mathf.Clamp01(norm * 25f) * Mathf.Pow(1f - norm, 0.8f);

                float sample = 0f;
                if (leadFreq > 1f)
                {
                    leadPhase += leadFreq / SampleRate;
                    sample += SampleWave(leadWave, leadPhase, rng) * 0.44f * leadEnv;
                }

                if (bassFreq > 1f)
                {
                    bassPhase += bassFreq / SampleRate;
                    sample += SampleWave(WaveType.Triangle, bassPhase, rng) * 0.46f * bassEnv;
                }

                // Percusión rítmica chiptune ligera
                if (isKick && norm < 0.25f)
                {
                    float kickNorm = norm / 0.25f;
                    float kickFreq = Mathf.Lerp(130f, 45f, kickNorm);
                    sample += Mathf.Sin(kickNorm * kickFreq * 0.2f) * (1f - kickNorm) * 0.35f;
                }
                else if (isHat && norm < 0.12f)
                {
                    float hatNorm = norm / 0.12f;
                    sample += SampleWave(WaveType.Noise, 0f, rng) * (1f - hatNorm) * 0.10f;
                }

                data[s * samplesPerStep + i] = Mathf.Clamp(sample * masterAmp, -0.95f, 0.95f);
            }
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float MidiToFreq(int midi)
    {
        if (midi <= 0)
            return 0f;
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }
}
