using System.Collections.Generic;
using UnityEngine;

public enum SoundType { Jump, Slide, Stumble, Gacha, Win, Lose, ButtonPressed, Hit }
public enum BGMType { MainMenu, Gameplay, BossFight }

[System.Serializable]
public struct SoundData
{
    public SoundType type;
    public AudioClip clip;
    public float fallbackFreq;
    public float duration;
}

[System.Serializable]
public struct BGMData
{
    public BGMType type;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Balancing")]
    [Tooltip("Pengali volume BGM. Jika 0.4, saat slider UI 100%, BGM hanya berbunyi 40% dari SFX.")]
    [Range(0.1f, 1f)] public float bgmMasterMultiplier = 0.4f;
    [Tooltip("Sisa persentase BGM saat SFX Win/Lose dimainkan (0.2 = sisa 20% volume)")]
    [Range(0f, 1f)] public float bgmDuckingMultiplier = 0.2f;

    [Header("SFX Library")]
    [SerializeField] private SoundData[] sfxArray;
    private Dictionary<SoundType, SoundData> sfxDictionary;
    private AudioSource sfxSource;

    [Header("BGM Library")]
    [SerializeField] private BGMData[] bgmArray;
    private Dictionary<BGMType, BGMData> bgmDictionary;
    private AudioSource bgmSource;

    private const string SFX_VOL_KEY = "SFXVolume";
    private const string BGM_VOL_KEY = "BGMVolume";
    private Coroutine bgmFadeCoroutine; // Dipakai bersama untuk Transisi Scene dan Ducking

    // PROPERTI CERDAS: Otomatis menghitung volume BGM riil berdasarkan slider dikali master multiplier
    private float ActualBGMVolume => GetCurrentBGMVolume() * bgmMasterMultiplier;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        InitializeDictionaries();
        LoadVolumes();
    }

    private void InitializeDictionaries()
    {
        sfxDictionary = new Dictionary<SoundType, SoundData>();
        foreach (var sfx in sfxArray)
            if (!sfxDictionary.ContainsKey(sfx.type)) sfxDictionary.Add(sfx.type, sfx);

        bgmDictionary = new Dictionary<BGMType, BGMData>();
        foreach (var bgm in bgmArray)
            if (!bgmDictionary.ContainsKey(bgm.type)) bgmDictionary.Add(bgm.type, bgm);
    }

    // ==========================================
    // KONTROL VOLUME
    // ==========================================
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
        PlayerPrefs.Save();
       sfxSource.volume = volume; // Terapkan volume baru segera
    }

    public void SetBGMVolume(float volume)
    {
        PlayerPrefs.SetFloat(BGM_VOL_KEY, volume);
        PlayerPrefs.Save();
        // Langsung terapkan jika tidak sedang fade out / ducking
        if (bgmFadeCoroutine == null) bgmSource.volume = ActualBGMVolume;
        bgmSource.volume = ActualBGMVolume; // Terapkan volume baru segera
    }

    public float GetCurrentSFXVolume() => PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
    public float GetCurrentBGMVolume() => PlayerPrefs.GetFloat(BGM_VOL_KEY, 1f);

    private void LoadVolumes()
    {
        sfxSource.volume = GetCurrentSFXVolume();
        bgmSource.volume = ActualBGMVolume; // Menggunakan properti baru
    }

    // ==========================================
    // FUNGSI PEMUTAR AUDIO
    // ==========================================
    public void PlaySFX(SoundType type)
    {
        if (sfxDictionary.TryGetValue(type, out SoundData data))
        {
            PlayClipOrSyntheticTone(data.clip, data.fallbackFreq, data.duration);

            // LOGIKA DUCKING (Peredupan Otomatis)
            // Jika suara Win atau Lose, otomatis kecilkan BGM!
            if (type == SoundType.Win || type == SoundType.Lose)
            {
                // Ambil durasi: panjang file audio asli, atau data.duration jika pakai suara sintetik
                float sfxDuration = data.clip != null ? data.clip.length : data.duration;
                TriggerBGMDucking(sfxDuration);
            }
        }
        else
        {
            Debug.LogWarning($"SFX {type} tidak ditemukan di AudioManager!");
        }
    }

    public void PlayBGM(BGMType type)
    {
        if (bgmDictionary.TryGetValue(type, out BGMData data))
        {
            if (bgmSource.clip == data.clip && bgmSource.isPlaying) return;

            bgmSource.clip = data.clip;
            bgmSource.Play();

            if (bgmFadeCoroutine == null && bgmSource.volume == 0f)
            {
                bgmSource.volume = ActualBGMVolume; // Kembali ke volume aman
            }
        }
    }

    public void StopBGM() => bgmSource.Stop();

    private void PlayClipOrSyntheticTone(AudioClip clip, float fallbackFreq, float duration)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
        else sfxSource.PlayOneShot(CreateToneClip(fallbackFreq, duration));
    }

    private AudioClip CreateToneClip(float frequency, float duration)
    {
        int sampleRate = 44100;
        int samplesCount = (int)(sampleRate * duration);
        float[] samples = new float[samplesCount];
        for (int i = 0; i < samplesCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * (1f - ((float)i / samplesCount));
        }
        AudioClip clip = AudioClip.Create("SynthTone", samplesCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ==========================================
    // EFEK FADE (Transisi Layar) & DUCKING (Win/Lose)
    // ==========================================
    public void FadeBGM(bool isFadingOut, float duration)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeBGMRoutine(isFadingOut, duration));
    }

    private void TriggerBGMDucking(float sfxDuration)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(DuckBGMRoutine(sfxDuration));
    }

    private System.Collections.IEnumerator FadeBGMRoutine(bool isFadingOut, float duration)
    {
        float timer = 0f;
        float maxVolume = ActualBGMVolume; // Ambil nilai yang sudah dikali
        float startVol = isFadingOut ? maxVolume : 0f;
        float targetVol = isFadingOut ? 0f : maxVolume;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, targetVol, timer / duration);
            yield return null;
        }
        bgmSource.volume = targetVol;
    }

    private System.Collections.IEnumerator DuckBGMRoutine(float sfxDuration)
    {
        float normalVol = ActualBGMVolume;
        float duckedVol = normalVol * bgmDuckingMultiplier;
        float transitionSpeed = 0.5f; // BGM meredup/membesar selama 0.5 detik

        // 1. Fade Down (Mengecil)
        float timer = 0f;
        float startVol = bgmSource.volume;
        while (timer < transitionSpeed)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, duckedVol, timer / transitionSpeed);
            yield return null;
        }
        bgmSource.volume = duckedVol;

        // 2. Tunggu sampai suara Win/Lose selesai
        yield return new WaitForSecondsRealtime(sfxDuration);

        // 3. Fade Up (Kembali Normal)
        timer = 0f;
        while (timer < transitionSpeed)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(duckedVol, normalVol, timer / transitionSpeed);
            yield return null;
        }
        bgmSource.volume = normalVol;
        bgmFadeCoroutine = null;
    }
}