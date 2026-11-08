using UnityEngine;

/// <summary>
/// Obstacle yang BERGERAK menuju player (seperti kereta yang melaju berlawanan arah di Subway Surfers).
///
/// TreadmillMover sudah menggeser semua objek ke belakang sebesar kecepatan lari player.
/// Skrip ini menambah kecepatan ekstra = currentRunSpeed * approachFactor, jadi total
/// kecepatan mendekat = (1 + approachFactor) x kecepatan lari.
///
/// Karena tambahan kecepatannya PROPORSIONAL terhadap kecepatan lari, ObstacleSpawner bisa menempatkannya
/// lebih jauh di depan (z * (1 + approachFactor)) sehingga tiba di player PERSIS pada saat yang sama
/// dengan baris pola yang statis. Jalur aman hasil validasi pola tetap adil, bahkan saat Speed Boost
/// atau stumble.
/// </summary>
[RequireComponent(typeof(TreadmillMover))]
public class MovingObstacle : MonoBehaviour
{
    [Tooltip("Kecepatan tambahan sebagai kelipatan kecepatan lari player. 0.5 = mendekat 1.5x lebih cepat.")]
    public float approachFactor = 0.5f;

    private void Update()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null) return;

        transform.position += Vector3.back * (player.currentRunSpeed * approachFactor * Time.deltaTime);
    }
}