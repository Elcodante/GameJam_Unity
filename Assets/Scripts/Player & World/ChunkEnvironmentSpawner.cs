using System.Collections.Generic;
using UnityEngine;

public class ChunkEnvironmentSpawner : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    [Tooltip("Daftar prefab segmen jalan+bangunan (panjang tiap chunk harus sama, misal 30m)")]
    public GameObject[] chunkPrefabs;

    [Header("Settings")]
    [Tooltip("Panjang 1 chunk dalam meter (misal 30)")]
    public float chunkLength = 30f;

    [Tooltip("Jarak pandang terjauh ke depan yang harus selalu tertutup jalan (misal 90 - 120)")]
    public float viewDistance = 90f;

    // Melacak posisi Z terdepan yang saat ini ada
    private float currentFrontZ = 0f;

    private void Start()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        // 1. Spawn chunk pertama mulai dari belakang player (-chunkLength)
        currentFrontZ = -chunkLength;

        // 2. Isi jalanan penuh ke depan sampai melebihi viewDistance
        while (currentFrontZ < viewDistance)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        float speed = PlayerController.Instance != null ? PlayerController.Instance.currentRunSpeed : 12f;

        // Karena semua chunk bergerak mundur bersama TreadmillMover, 
        // titik ujung terdepan juga ikut mundur setiap frame
        currentFrontZ -= speed * Time.deltaTime;

        // Jika ujung terdepan sudah mendekati viewDistance, langsung tempelkan chunk baru di ujungnya!
        while (currentFrontZ < viewDistance)
        {
            SpawnNextChunk();
        }
    }

    private void SpawnNextChunk()
    {
        GameObject prefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];
        if (prefab == null) return;

        // Selalu menempel tepat di currentFrontZ (mustahil ada gap)
        Vector3 spawnPos = new Vector3(0f, 0f, currentFrontZ);
        GameObject chunkObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Pastikan chunk bergerak mundur
        TreadmillMover mover = chunkObj.GetComponent<TreadmillMover>();
        if (mover == null) mover = chunkObj.AddComponent<TreadmillMover>();
        mover.destroyZPosition = -chunkLength - 10f; // Hancur setelah lewat di belakang layar

        // Majukan ujung terdepan sejauh panjang chunk
        currentFrontZ += chunkLength;
    }
}