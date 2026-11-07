using UnityEngine;

public enum ObstacleType
{
    Low_JumpOver,     // Rintangan rendah (bisa diloncati)
    High_SlideUnder,  // Rintangan tinggi/melayang (harus diseluncuri / slide)
    FullBlock         // Rintangan penuh (harus pindah jalur)
}

[RequireComponent(typeof(TreadmillMover))]
public class Obstacle : MonoBehaviour
{
    public ObstacleType obstacleType = ObstacleType.FullBlock;

    private void Start()
    {
        // Pastikan tag-nya selalu Obstacle agar deteksi raycast samping bekerja
        gameObject.tag = "Obstacle";
    }

    private void OnTriggerEnter(Collider other)
    {
        // Deteksi jika yang menabrak adalah player
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            // JIKA PLAYER MEMILIKI BUFF "TAHAN BATIN (INVINCIBLE)"
            if (PlayerController.Instance != null && PlayerController.Instance.isInvincible)
            {
                Debug.Log("<color=yellow>[INVINCIBLE]</color> Rintangan hancur tertabrak!");
                Destroy(gameObject);
                return;
            }

            // JIKA TIDAK KEBAL: TABRAKAN DEPAN = INSTANT LOSE!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver("KAMU MENABRAK RINTANGAN! Celanamu tidak tertolong lagi...");
            }
        }
    }
}