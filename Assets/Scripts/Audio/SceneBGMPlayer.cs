using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    [Header("Pengaturan BGM Scene Ini")]
    [Tooltip("Pilih musik apa yang ingin diputar saat scene ini dimulai")]
    public BGMType bgmToPlay;

    private void Start()
    {
        // Gunakan Start (bukan Awake) untuk memastikan AudioManager 
        // sudah selesai melakukan inisialisasi DontDestroyOnLoad-nya.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(bgmToPlay);
        }
        else
        {
            Debug.LogWarning("AudioManager tidak ditemukan! Pastikan Prefab AudioSystem ada di scene awal.");
        }
    }
}