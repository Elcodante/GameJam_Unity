using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("Panjang per potongan blok. Jika panjang modular jalan/bangunanmu 10m, isi 10")]
    public float pieceLength = 10f;
    [Tooltip("Jarak terjauh di depan player tempat objek pertama muncul")]
    public float spawnZDistance = 60f;
    [Tooltip("Jumlah baris awal saat game dimulai")]
    public int initialPieces = 8;
    [Tooltip("Tinggi lantai / permukaan aspal (sesuaikan dengan Y jalan, default: -0.5f)")]
    public float groundY = -0.5f;

    [Header("Layer 1: Modular Road / Ground (Tengah)")]
    [Tooltip("Centang jika ingin jalanan bergerak mundur secara modular")]
    public bool enableMovingRoad = true;
    [Tooltip("Prefab potongan jalan yang akan menyambung terus")]
    public GameObject[] roadPrefabs;

    [Header("Layer 2: Roadside Buildings (Pinggir Jalan)")]
    public bool enableRoadsideBuildings = true;
    [Tooltip("Daftar prefab bangunan/warung/pohon di pinggir jalan")]
    public GameObject[] roadsidePrefabs;
    [Tooltip("Jarak dari tengah jalan ke pinggir jalan (misal 6.5)")]
    public float roadsideOffset = 6.5f;

    [Header("Layer 3: Background Buildings (Latar Belakang)")]
    public bool enableBackgroundBuildings = true;
    [Tooltip("Daftar prefab gedung tinggi/perumahan yang ada di belakang layer 2")]
    public GameObject[] backgroundPrefabs;
    [Tooltip("Jarak layer belakang dari tengah jalan (misal 16 - 22 agar berada di balik layer 2)")]
    public float backgroundOffset = 18f;
    [Tooltip("Tambahan ketinggian layer belakang (misal 0 atau dibuat sedikit berundak)")]
    public float backgroundYOffset = 0f;

    private float distanceAccumulator = 0f;
    private float currentSpawnZ = 0f;

    private void Start()
    {
        // Spawn baris-baris awal dari belakang player ke depan
        currentSpawnZ = -pieceLength;
        for (int i = 0; i < initialPieces; i++)
        {
            SpawnFullSlice(currentSpawnZ);
            currentSpawnZ += pieceLength;
        }
    }

    private void Update()
    {
        float speed = PlayerController.Instance != null ? PlayerController.Instance.currentRunSpeed : 12f;

        // Akumulasi jarak tempuh player
        distanceAccumulator += speed * Time.deltaTime;

        // Tiap player menempuh sejauh pieceLength, spawn potongan lingkungan baru
        while (distanceAccumulator >= pieceLength)
        {
            distanceAccumulator -= pieceLength;
            SpawnFullSlice(spawnZDistance - distanceAccumulator);
        }
    }

    /// <summary>
    /// Memunculkan 1 baris potongan lingkungan (Jalan + Bangunan Depan + Bangunan Belakang)
    /// </summary>
    private void SpawnFullSlice(float zPos)
    {
        // 1. SPAWN JALAN (Tengah, X = 0)
        if (enableMovingRoad && roadPrefabs != null && roadPrefabs.Length > 0)
        {
            GameObject roadPrefab = roadPrefabs[Random.Range(0, roadPrefabs.Length)];
            if (roadPrefab != null)
            {
                Vector3 roadPos = new Vector3(0f, groundY, zPos);
                GameObject roadObj = Instantiate(roadPrefab, roadPos, Quaternion.identity, transform);
                EnsureMoverComponent(roadObj);
            }
        }

        // 2. SPAWN BANGUNAN PINGGIR JALAN (Layer Depan)
        if (enableRoadsideBuildings && roadsidePrefabs != null && roadsidePrefabs.Length > 0)
        {
            // Sisi Kiri (Hadap kanan / ke arah jalan)
            SpawnSingleProp(roadsidePrefabs, -roadsideOffset, groundY, zPos, 90f);

            // Sisi Kanan (Hadap kiri / ke arah jalan)
            SpawnSingleProp(roadsidePrefabs, roadsideOffset, groundY, zPos, -90f);
        }

        // 3. SPAWN BANGUNAN LATAR BELAKANG (Layer Belakang)
        if (enableBackgroundBuildings && backgroundPrefabs != null && backgroundPrefabs.Length > 0)
        {
            float bgY = groundY + backgroundYOffset;

            // Sisi Kiri Belakang
            SpawnSingleProp(backgroundPrefabs, -backgroundOffset, bgY, zPos, 90f);

            // Sisi Kanan Belakang
            SpawnSingleProp(backgroundPrefabs, backgroundOffset, bgY, zPos, -90f);
        }
    }

    private void SpawnSingleProp(GameObject[] pool, float xPos, float yPos, float zPos, float yRotation)
    {
        GameObject prefab = pool[Random.Range(0, pool.Length)];
        if (prefab == null) return;

        Vector3 pos = new Vector3(xPos, yPos, zPos);
        GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0f, yRotation, 0f), transform);
        EnsureMoverComponent(obj);
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