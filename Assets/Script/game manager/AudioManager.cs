using System.Collections.Generic;
using UnityEngine;

// Enum untuk SFX dan BGM agar rapi
public enum SoundType { Jump, Slide, Stumble, Gacha, Win, Lose, ButtonPressed }
public enum BGMType { MainMenu, Gameplay, BossFight } // Tambahkan tipe BGM di sini

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

    [Header("SFX Library")]
    [SerializeField] private SoundData[] sfxArray;
    private Dictionary<SoundType, SoundData> sfxDictionary;
    private AudioSource sfxSource;

    [Header("BGM Library")]
    [SerializeField] private BGMData[] bgmArray;
    private Dictionary<BGMType, BGMData> bgmDictionary;
    private AudioSource bgmSource;

    // Key untuk PlayerPrefs
    private const string SFX_VOL_KEY = "SFXVolume";
    private const string BGM_VOL_KEY = "BGMVolume";

    private void Awake()
    {
        // Singleton pattern & DontDestroyOnLoad
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

        // Setup komponen AudioSource untuk SFX (Tidak Loop)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // Setup komponen AudioSource untuk BGM (Otomatis Loop)
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
        {
            if (!sfxDictionary.ContainsKey(sfx.type))
                sfxDictionary.Add(sfx.type, sfx);
        }

        bgmDictionary = new Dictionary<BGMType, BGMData>();
        foreach (var bgm in bgmArray)
        {
            if (!bgmDictionary.ContainsKey(bgm.type))
                bgmDictionary.Add(bgm.type, bgm);
        }
    }

    // ==========================================
    // KONTROL VOLUME (Dipanggil dari Slider UI)
    // ==========================================
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat(BGM_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    public float GetCurrentSFXVolume() => PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
    public float GetCurrentBGMVolume() => PlayerPrefs.GetFloat(BGM_VOL_KEY, 1f);

    private void LoadVolumes()
    {
        sfxSource.volume = GetCurrentSFXVolume();
        bgmSource.volume = GetCurrentBGMVolume();
    }

    // ==========================================
    // FUNGSI PEMUTAR AUDIO
    // ==========================================
    public void PlaySFX(SoundType type)
    {
        if (sfxDictionary.TryGetValue(type, out SoundData data))
        {
            PlayClipOrSyntheticTone(data.clip, data.fallbackFreq, data.duration);
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
            // Mencegah lagu mengulang jika BGM-nya sama
            if (bgmSource.clip == data.clip && bgmSource.isPlaying) return;

            bgmSource.clip = data.clip;
            bgmSource.Play();

            // PENGAMAN: Jika saat BGM diputar ternyata volume sedang 0 (tapi bukan karena sedang proses fade in),
            // kembalikan volume secara instan ke settingan normal.
            if (bgmFadeCoroutine == null && bgmSource.volume == 0f)
            {
                bgmSource.volume = GetCurrentBGMVolume();
            }
        }
        else
        {
            Debug.LogWarning($"BGM {type} tidak ditemukan di AudioManager!");
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // Fungsi suara sintetik murni (Tidak ada perubahan)
    private void PlayClipOrSyntheticTone(AudioClip clip, float fallbackFreq, float duration)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
        else
        {
            AudioClip beep = CreateToneClip(fallbackFreq, duration);
            sfxSource.PlayOneShot(beep);
        }
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

    // Tambahkan variabel ini di bagian atas (di bawah deklarasi bgmSource)
    private Coroutine bgmFadeCoroutine;

    // ==========================================
    // FUNGSI EFEK FADE AUDIO (Tambahkan di bagian bawah)
    // ==========================================
    public void FadeBGM(bool isFadingOut, float duration)
    {
        // Hentikan proses fade sebelumnya agar tidak bentrok (misal layar baru fade in, tiba-tiba di-pause)
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(FadeBGMRoutine(isFadingOut, duration));
    }

    private System.Collections.IEnumerator FadeBGMRoutine(bool isFadingOut, float duration)
    {
        float timer = 0f;

        // PENTING: Ambil volume asli dari setting (agar tidak nge-reset paksa ke 100%)
        float maxVolume = GetCurrentBGMVolume();

        float startVol = isFadingOut ? maxVolume : 0f;
        float targetVol = isFadingOut ? 0f : maxVolume;

        while (timer < duration)
        {
            // Pakai unscaledDeltaTime agar suara tetap fade out dengan halus walau game sedang Pause
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, targetVol, timer / duration);
            yield return null;
        }

        // Pastikan nilai akhir presisi
        bgmSource.volume = targetVol;
    }
}