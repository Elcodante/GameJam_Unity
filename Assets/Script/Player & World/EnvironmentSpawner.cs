using System.Collections.Generic;
using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("Prefabs Pool")]
    [Tooltip("Daftar prefab bangunan / dekorasi samping jalan")]
    public GameObject[] buildingPrefabs;

    [Header("Spawn Positions")]
    public float sideOffset = 6.0f; // Jarak dari tengah lintasan ke samping
    public float spawnZDistance = 60f; // Jarak di depan player saat spawn pertama kali
    public float pieceLength = 10f; // Estimasi panjang satu blok bangunan
    public int initialPieces = 8; // Berapa potong yang di-spawn di awal game

    private float nextSpawnZ = 0f;

    private void Start()
    {
        nextSpawnZ = -pieceLength; // Mulai dari agak belakang player
        for (int i = 0; i < initialPieces; i++)
        {
            SpawnEnvironmentRow();
        }
    }

    private void Update()
    {
        // Jika potongan paling depan sudah mendekati jarak spawn, spawn baris baru
        if (nextSpawnZ < spawnZDistance)
        {
            SpawnEnvironmentRow();
        }
        else
        {
            // Karena dunia bergerak mundur via TreadmillMover, majukan titik nextSpawnZ relatif
            float speed = PlayerController.Instance != null ? PlayerController.Instance.currentRunSpeed : 12f;
            nextSpawnZ -= speed * Time.deltaTime;
        }
    }

    private void SpawnEnvironmentRow()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        // Spawn Kiri
        GameObject leftPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        Vector3 leftPos = new Vector3(-sideOffset, 0, nextSpawnZ);
        GameObject leftObj = Instantiate(leftPrefab, leftPos, Quaternion.Euler(0, 90, 0), transform);
        EnsureMoverComponent(leftObj);

        // Spawn Kanan
        GameObject rightPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        Vector3 rightPos = new Vector3(sideOffset, 0, nextSpawnZ);
        GameObject rightObj = Instantiate(rightPrefab, rightPos, Quaternion.Euler(0, -90, 0), transform);
        EnsureMoverComponent(rightObj);

        nextSpawnZ += pieceLength;
    }

    private void EnsureMoverComponent(GameObject obj)
    {
        if (obj.GetComponent<TreadmillMover>() == null)
        {
            TreadmillMover mover = obj.AddComponent<TreadmillMover>();
            mover.destroyZPosition = -20f;
        }
    }
}