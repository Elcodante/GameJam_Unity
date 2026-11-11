using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private Button myButton;

    private void Start()
    {
        myButton = GetComponent<Button>();

        // Menambahkan perintah suara ke tombol secara otomatis lewat kode
        myButton.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonPressed);
        }
    }

    private void OnDestroy()
    {
        // Mencegah memory leak jika tombol dihancurkan (misal pindah scene)
        if (myButton != null)
        {
            myButton.onClick.RemoveListener(PlayClickSound);
        }
    }
}