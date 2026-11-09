using UnityEngine;
using UnityEngine.UI;

// Pilihan untuk menentukan Slider ini mengatur tipe audio yang mana
public enum AudioChannel { SFX, BGM }

[RequireComponent(typeof(Slider))]
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Tipe Slider")]
    [Tooltip("Pilih apakah slider ini untuk mengatur SFX atau BGM")]
    public AudioChannel channel;

    private Slider volumeSlider;

    private void Start()
    {
        volumeSlider = GetComponent<Slider>();

        if (AudioManager.Instance != null)
        {
            // Tarik data terakhir yang tersimpan saat menu baru dibuka
            if (channel == AudioChannel.SFX)
                volumeSlider.value = AudioManager.Instance.GetCurrentSFXVolume();
            else
                volumeSlider.value = AudioManager.Instance.GetCurrentBGMVolume();

            // Mulai dengarkan geseran player
            volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnSliderValueChanged(float newValue)
    {
        if (AudioManager.Instance != null)
        {
            // Kirim nilai yang digeser langsung ke fungsi yang tepat di AudioManager
            if (channel == AudioChannel.SFX)
                AudioManager.Instance.SetSFXVolume(newValue);
            else
                AudioManager.Instance.SetBGMVolume(newValue);
        }
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}