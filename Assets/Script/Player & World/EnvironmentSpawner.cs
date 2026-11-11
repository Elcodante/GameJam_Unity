using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("Prefabs Pool")]
    [Tooltip("Daftar prefab bangunan / dekorasi samping jalan")]
    public GameObject[] buildingPrefabs;

    [Header("Spawn Heights & Positions")]
    [Tooltip("Tinggi lantai / permukaan jalan. Samakan dengan Y Jalan kamu (-0.5f)")]
    public float spawnY = -0.5f;

    [Tooltip("Jarak dari tengah jalan ke samping (coba 5.5 - 7 agar pas di pinggir jalan)")]
    public float sideOffset = 6.5f;

    [Tooltip("Jarak terjauh di depan player tempat bangunan pertama muncul")]
    public float spawnZDistance = 60f;

    [Tooltip("Panjang per blok bangunan (jarak Z antar baris)")]
    public float pieceLength = 10f;

    [Tooltip("Jumlah baris awal saat game dimulai")]
    public int initialPieces = 8;

    private float distanceAccumulator = 0f;
    private float currentSpawnZ = 0f;

    private void Start()
    {
        // Spawn baris-baris awal dari belakang player ke depan
        currentSpawnZ = -pieceLength;
        for (int i = 0; i < initialPieces; i++)
        {
            SpawnEnvironmentRow(currentSpawnZ);
            currentSpawnZ += pieceLength;
        }
    }

    private void Update()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        float speed = PlayerController.Instance != null ? PlayerController.Instance.currentRunSpeed : 12f;
        
        // Akumulasi jarak tempuh riil player agar sinkron dengan TreadmillMover
        distanceAccumulator += speed * Time.deltaTime;

        // Tiap menempuh pieceLength (10 meter), spawn baris baru tepat di depan
        while (distanceAccumulator >= pieceLength)
        {
            distanceAccumulator -= pieceLength;
            SpawnEnvironmentRow(spawnZDistance - distanceAccumulator);
        }
    }

    private void SpawnEnvironmentRow(float zPos)
    {
        // 1. Spawn Sisi Kiri (Menghadap kanan ke arah jalan)
        GameObject leftPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        if (leftPrefab != null)
        {
            Vector3 leftPos = new Vector3(-sideOffset, spawnY, zPos);
            GameObject leftObj = Instantiate(leftPrefab, leftPos, Quaternion.Euler(0, 90, 0), transform);
            EnsureMoverComponent(leftObj);
        }

        // 2. Spawn Sisi Kanan (Menghadap kiri ke arah jalan)
        GameObject rightPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        if (rightPrefab != null)
        {
            Vector3 rightPos = new Vector3(sideOffset, spawnY, zPos);
            GameObject rightObj = Instantiate(rightPrefab, rightPos, Quaternion.Euler(0, -90, 0), transform);
            EnsureMoverComponent(rightObj);
        }
    }

    private void EnsureMoverComponent(GameObject obj)
    {
        TreadmillMover mover = obj.GetComponent<TreadmillMover>();
        if (mover == null)
        {
            mover = obj.AddComponent<TreadmillMover>();
        }
        mover.destroyZPosition = -25f;
    }
}