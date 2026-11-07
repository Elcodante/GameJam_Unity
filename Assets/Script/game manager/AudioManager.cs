using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips (Bisa diisi oleh Sound Designer)")]
    public AudioClip jumpSFX;
    public AudioClip slideSFX;
    public AudioClip stumbleSFX;
    public AudioClip gachaRollSFX;
    public AudioClip winFlushSFX;
    public AudioClip loseSFX;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void PlayJump() => PlayClipOrSyntheticTone(jumpSFX, 520f, 0.1f);
    public void PlaySlide() => PlayClipOrSyntheticTone(slideSFX, 260f, 0.15f);
    public void PlayStumble() => PlayClipOrSyntheticTone(stumbleSFX, 150f, 0.25f);
    public void PlayGacha() => PlayClipOrSyntheticTone(gachaRollSFX, 880f, 0.2f);
    public void PlayWin() => PlayClipOrSyntheticTone(winFlushSFX, 440f, 0.5f);
    public void PlayLose() => PlayClipOrSyntheticTone(loseSFX, 110f, 0.6f);

    private void PlayClipOrSyntheticTone(AudioClip clip, float fallbackFreq, float duration)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            // Bikin suara beeps sederhana via kode jika belum ada file audio
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
            // Sine wave sederhana dengan fade out halus
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * (1f - ((float)i / samplesCount));
        }

        AudioClip clip = AudioClip.Create("SynthTone", samplesCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}