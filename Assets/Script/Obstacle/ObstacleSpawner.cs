using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject lowObstaclePrefab;   // Rintangan ceper (Lompat)
    public GameObject highObstaclePrefab;  // Plang melayang (Slide)
    public GameObject fullObstaclePrefab;  // Tembok / Orang / Box penuh
    public GameObject gachaPickupPrefab;   // Kotak Gacha

    [Header("Toilet Prefab (Finish)")]
    public GameObject toiletPrefab;

    [Header("Spawn Pacing & Density")]
    [Tooltip("Waktu antar gelombang rintangan (0.85s - 1.0s membuat lintasan ramai)")]
    public float spawnInterval = 0.95f;
    public float spawnDistanceZ = 55f;
    public float laneDistance = 2.5f;

    [Header("Gacha Spawn Balancing")]
    [Tooltip("Peluang munculnya kotak gacha saat ada kesempatan (0.28 = 28% peluang)")]
    [Range(0f, 1f)] public float gachaSpawnChance = 0.28f;
    [Tooltip("Jeda waktu minimal antar kotak gacha agar tidak spam")]
    public float gachaCooldown = 4.0f;
    private float lastGachaTime = -99f;

    private float timer;
    private bool toiletSpawned = false;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing) return;

        // Mendekati garis akhir: Berhenti spawn rintangan dan spawn Toilet
        if (GameManager.Instance.remainingDistance <= GameManager.Instance.toiletSpawnThreshold)
        {
            if (!toiletSpawned)
            {
                SpawnToiletFinish();
            }
            return;
        }

        // Loop spawn rintangan berdasarkan interval waktu
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnDensePattern();
        }
    }

    private void SpawnDensePattern()
    {
        float[] lanesX = { -laneDistance, 0f, laneDistance }; // 0: Kiri, 1: Tengah, 2: Kanan

        // Variasi 6 pola rintangan yang padat dan dinamis
        int pattern = Random.Range(0, 6);

        switch (pattern)
        {
            case 0:
                // POLA 1: DUAL WALL (2 Jalur tertutup penuh, 1 jalur terbuka)
                int safeLane = Random.Range(0, 3);
                for (int i = 0; i < 3; i++)
                {
                    if (i != safeLane)
                    {
                        SpawnObstacle(fullObstaclePrefab, lanesX[i], 0.9f);
                    }
                    else
                    {
                        TrySpawnGacha(lanesX[i], 0.8f); // Peluang gacha di jalur aman
                    }
                }
                break;

            case 1:
                // POLA 2: CHOKE JUMP (Kiri & Kanan ditembok, Tengah WAJIB LOMPAT)
                SpawnObstacle(fullObstaclePrefab, lanesX[0], 0.9f);
                SpawnObstacle(lowObstaclePrefab, lanesX[1], 0.4f);
                SpawnObstacle(fullObstaclePrefab, lanesX[2], 0.9f);

                // Peluang gacha melayang di atas rintangan lompat
                TrySpawnGacha(lanesX[1], 1.8f);
                break;

            case 2:
                // POLA 3: CHOKE SLIDE (Kiri & Kanan ditembok, Tengah WAJIB SLIDE)
                SpawnObstacle(fullObstaclePrefab, lanesX[0], 0.9f);
                SpawnObstacle(highObstaclePrefab, lanesX[1], 1.7f);
                SpawnObstacle(fullObstaclePrefab, lanesX[2], 0.9f);

                // Peluang gacha di kolong slide
                TrySpawnGacha(lanesX[1], 0.4f);
                break;

            case 3:
                // POLA 4: TRIPLE ACTION (Ketiga jalur terisi, player bebas pilih cara lolos)
                // Kiri: Lompat | Tengah: Slide | Kanan: Tembok
                SpawnObstacle(lowObstaclePrefab, lanesX[0], 0.4f);
                SpawnObstacle(highObstaclePrefab, lanesX[1], 1.7f);
                SpawnObstacle(fullObstaclePrefab, lanesX[2], 0.9f);

                // Umpan gacha acak
                if (Random.value > 0.5f) TrySpawnGacha(lanesX[0], 1.8f);
                else TrySpawnGacha(lanesX[1], 0.4f);
                break;

            case 4:
                // POLA 5: STAGGERED HURDLES (2 Rintangan Aksi Bersebelahan)
                int emptyLane = Random.Range(0, 3);
                for (int i = 0; i < 3; i++)
                {
                    if (i == emptyLane) continue;

                    // Acak antara rintangan lompat atau slide
                    if (Random.value > 0.5f)
                        SpawnObstacle(lowObstaclePrefab, lanesX[i], 0.4f);
                    else
                        SpawnObstacle(highObstaclePrefab, lanesX[i], 1.7f);
                }
                break;

            case 5:
                // POLA 6: SINGLE BLOCK (Rintangan tunggal acak - memberi jeda bernapas sejenak)
                int singleLane = Random.Range(0, 3);
                SpawnObstacle(fullObstaclePrefab, lanesX[singleLane], 0.9f);
                break;
        }
    }

    private void TrySpawnGacha(float xPos, float yPos)
    {
        // Cek Cooldown & Roll Persentase Peluang
        if (Time.time - lastGachaTime >= gachaCooldown && Random.value <= gachaSpawnChance)
        {
            lastGachaTime = Time.time;
            SpawnItem(gachaPickupPrefab, xPos, yPos);
        }
    }

    private void SpawnObstacle(GameObject prefab, float xPos, float yPos)
    {
        if (prefab == null) return;
        Vector3 spawnPos = new Vector3(xPos, yPos, spawnDistanceZ);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private void SpawnItem(GameObject prefab, float xPos, float yPos)
    {
        if (prefab == null) return;
        Vector3 spawnPos = new Vector3(xPos, yPos, spawnDistanceZ);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private void SpawnToiletFinish()
    {
        toiletSpawned = true;
        GameManager.Instance.isToiletSpawned = true;

        if (toiletPrefab != null)
        {
            Vector3 spawnPos = new Vector3(0f, 0f, spawnDistanceZ);
            Instantiate(toiletPrefab, spawnPos, Quaternion.identity);
            Debug.Log("<color=green>[FINISH]</color> Toilet umum telah muncul di depan!");
        }
    }
}