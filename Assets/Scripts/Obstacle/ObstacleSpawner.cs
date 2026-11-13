using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner obstacle bergaya Subway Surfers (v2, berdasarkan riset).
///
/// Prinsip yang diambil dari Subway Surfers:
///  - Level dibangun dari CHUNK/pola yang dirancang (data-driven), bukan acak murni.
///  - Ada obstacle DIAM (kereta parkir, tembok, pilar) dan obstacle BERGERAK (kereta melaju ke arah player).
///  - Ada barrier yang harus dilompati, ada yang harus di-slide, ada yang harus dihindari lewat pindah jalur.
///  - Tekanan tersebar di KETIGA jalur, bukan terus-menerus memaksa player ke tengah.
///  - Satu pola bisa tampil berbeda tiap kali muncul (layer variasi).
///  - Selalu ada jalur aman, dan ada jeda napas antar pola.
///
/// Cara menulis pola (satu baris = 3 karakter: kiri, tengah, kanan; baris paling ATAS ditemui lebih dulu):
///     .  kosong
///     F  tembok / kereta DIAM (harus pindah jalur)
///     M  kereta BERGERAK menuju player (harus pindah jalur)
///     L  barrier rendah (lompat)
///     H  barrier tinggi (slide)
///     ?  barrier acak: L atau H setiap kali muncul
///     *  blok acak: F atau M setiap kali muncul
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    public enum Cell : byte
    {
        Empty = 0, Full = 1, Low = 2, High = 3, Moving = 4,
        RandomBarrier = 5, // diselesaikan menjadi Low/High saat spawn
        RandomBlock = 6    // diselesaikan menjadi Full/Moving saat spawn
    }

    [System.Serializable]
    public class ObstaclePattern
    {
        public string name = "New Pattern";

        [Tooltip("Pola ini baru boleh muncul setelah kesulitan mencapai nilai ini (0 = awal, 1 = dekat toilet)")]
        [Range(0f, 1f)] public float minDifficulty = 0f;

        [Tooltip("Pola ini berhenti muncul setelah kesulitan melewati nilai ini")]
        [Range(0f, 1f)] public float maxDifficulty = 1f;

        [Tooltip("Bobot peluang terpilih. Makin besar makin sering muncul")]
        [Min(0f)] public float weight = 1f;

        [Tooltip(".=kosong  F=kereta diam  M=kereta bergerak  L=lompat  H=slide  ?=L/H acak  *=F/M acak\nSatu baris = 3 karakter (kiri tengah kanan). Baris paling atas = yang pertama ditemui player.")]
        [TextArea(2, 10)] public string rows = "...";

        [System.NonSerialized] public Cell[][] cells;      // [baris][jalur]
        [System.NonSerialized] public bool[][] safeLanes;  // [baris][jalur] jalur yang bisa dilewati sampai akhir pola
        [System.NonSerialized] public float[] laneProfile; // jalur mana yang "dipaksa" pola ini (jumlah = 1)
        [System.NonSerialized] public bool isValid;
    }

    private class Row
    {
        public Cell[] lanes = new Cell[LaneCount];
        public int gachaLane = -1;
        public float gachaY;
    }

    private const int LaneCount = 3;

    [Header("Obstacle Prefabs")]
    public GameObject lowObstaclePrefab;   // Rintangan ceper (Lompat)
    public GameObject highObstaclePrefab;  // Plang melayang (Slide)
    public GameObject fullObstaclePrefab;  // Tembok / Orang / Box penuh (diam)
    [Tooltip("Prefab tiang gawang 3 jalur yang membentang di jalan")]
    public GameObject highObstacleFramePrefab;
    [Tooltip("Prefab kereta yang bergerak. Kosong = pakai Full Obstacle Prefab")]
    public GameObject movingObstaclePrefab;
    public GameObject gachaPickupPrefab;   // Kotak Gacha

    [Header("Toilet Prefab (Finish)")]
    public GameObject toiletPrefab;

    [Header("Layout")]
    public float spawnDistanceZ = 55f;
    public float laneDistance = 2.5f;
    [Tooltip("Otomatis samakan laneDistance dengan PlayerController saat Play")]
    public bool syncLaneDistanceWithPlayer = true;

    [Header("Spawn Heights (titik tengah prefab)")]
    public float lowY = 0.4f;
    public float highY = 1.7f;
    public float fullY = 0.9f;
    public float gachaGroundY = 0.8f;   // gacha di jalur kosong
    public float gachaAirY = 1.8f;      // gacha melayang di atas rintangan lompat
    public float gachaUnderY = 0.4f;    // gacha di kolong rintangan slide
    [Tooltip("Ketinggian tiang gawang (sesuaikan tinggi jalan, misal -0.5f)")]
    public float highFrameY = 1.7f;

    [Header("Pattern Library")]
    [Tooltip("Pakai library pola bawaan (selalu versi terbaru). Matikan jika ingin hanya memakai Custom Patterns.")]
    public bool useBuiltInPatterns = true;
    [Tooltip("Pola buatan sendiri. Ditambahkan ke library bawaan. Klik kanan komponen > Copy Built-In Patterns To Custom List untuk mulai dari pola bawaan.")]
    public List<ObstaclePattern> customPatterns = new List<ObstaclePattern>();

    [Header("Pattern Pacing")]
    [Tooltip("Jarak (meter) antar baris dalam satu pola. Sebaiknya >= panjang prefab terpanjang. 5-7 terasa pas.")]
    public float rowSpacing = 6f;
    [Tooltip("Baris kosong sebelum pola pertama di awal game")]
    public int introGapRows = 0;
    [Tooltip("Baris kosong antar pola di awal game (santai)")]
    public int startGapRows = 3;
    [Tooltip("Baris kosong antar pola mendekati toilet (rapat). Minimal 1 supaya pindah jalur antar pola selalu mungkin")]
    public int endGapRows = 1;
    [Tooltip("Maksimal pindah jalur yang dianggap adil di antara 2 baris berurutan")]
    [Range(1, 2)] public int maxLaneShiftPerRow = 1;
    [Tooltip("Cermin pola kiri-kanan (dipilih agar beban jalur seimbang)")]
    public bool randomMirror = true;
    [Tooltip("Jangan ulang N pola terakhir")]
    public int avoidRepeatLast = 3;
    [Tooltip("Kurva progres jarak (0..1) menjadi kesulitan (0..1). Default linear.")]
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Jalur kosong (meter) sebelum toilet muncul supaya finish tidak tertutup rintangan")]
    public float finalClearZone = 20f;

    [Header("Lane Balance (anti-menumpuk di tengah)")]
    [Tooltip("Seberapa kuat spawner menghindari memaksa player ke jalur yang sudah sering dipaksa. 0 = mati.")]
    [Range(0f, 2f)] public float balanceStrength = 1f;
    [Tooltip("Seberapa lama 'ingatan' beban jalur (0.75 = sekitar 4 pola terakhir)")]
    [Range(0f, 0.95f)] public float laneLoadDecay = 0.75f;

    [Header("Moving Obstacles (kereta bergerak)")]
    public bool allowMovingObstacles = true;
    [Tooltip("Kecepatan tambahan sebagai kelipatan kecepatan lari. 0.5 = mendekat 1.5x lebih cepat. Kereta bergerak muncul lebih jauh (z x (1+nilai)) tapi tiba bersamaan dengan barisnya.")]
    [Range(0.1f, 1.5f)] public float movingApproachFactor = 0.5f;
    [Tooltip("Peluang sel '*' menjadi kereta bergerak (selain F) setelah kesulitan melewati batas")]
    [Range(0f, 1f)] public float randomBlockMovingChance = 0.5f;
    public float randomBlockMovingMinDifficulty = 0.3f;

    [Header("Forgiving Colliders")]
    [Tooltip("Persentase lebar collider (BoxCollider) obstacle yang dipotong agar sentuhan tipis di sisi tidak mematikan. Subway Surfers menyetel ukuran collider tiap obstacle untuk ini.")]
    [Range(0f, 0.4f)] public float obstacleWidthForgiveness = 0.1f;

    [Header("Gacha Spawn Balancing")]
    [Tooltip("Peluang sebuah pola membawa kotak gacha (0.28 = 28%)")]
    [Range(0f, 1f)] public float gachaSpawnChance = 0.28f;
    [Tooltip("Jeda waktu minimal antar kotak gacha agar tidak spam")]
    public float gachaCooldown = 4.0f;

    private readonly List<ObstaclePattern> activePatterns = new List<ObstaclePattern>();
    private readonly Queue<Row> rowQueue = new Queue<Row>();
    private readonly Queue<int> recentPatterns = new Queue<int>();
    private readonly List<int> candidateBuffer = new List<int>();
    private readonly List<float> weightBuffer = new List<float>();
    private readonly Row emptyRow = new Row();
    private readonly float[] laneLoad = new float[LaneCount] { 1f / 3f, 1f / 3f, 1f / 3f };

    private float distanceAccumulator;
    private float lastGachaTime = -99f;
    private bool toiletSpawned = false;
    private bool firstPatternQueued = false;

    // =====================================================
    // LIFECYCLE
    // =====================================================
    private void Awake()
    {
        RebuildActivePatterns();
        PreparePatterns();
    }

    private void Start()
    {
        if (syncLaneDistanceWithPlayer && PlayerController.Instance != null)
        {
            laneDistance = PlayerController.Instance.laneDistance;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        PlayerController player = PlayerController.Instance;
        if (gm == null || player == null || gm.currentState != GameState.Playing) return;

        // Mendekati garis akhir: berhenti spawn rintangan dan spawn Toilet
        if (gm.remainingDistance <= gm.toiletSpawnThreshold)
        {
            rowQueue.Clear();
            if (!toiletSpawned) SpawnToiletFinish();
            return;
        }

        // Landasan kosong sebelum toilet (rintangan terakhir tidak menumpuk di depan toilet)
        if (gm.remainingDistance <= gm.toiletSpawnThreshold + finalClearZone)
        {
            rowQueue.Clear();
            return;
        }

        // Spawn berbasis JARAK TEMPUH: satu baris setiap rowSpacing meter
        distanceAccumulator += player.currentRunSpeed * Time.deltaTime;
        while (distanceAccumulator >= rowSpacing)
        {
            distanceAccumulator -= rowSpacing;

            if (rowQueue.Count == 0) RefillQueue(gm);
            Row row = rowQueue.Dequeue();

            // sisa jarak = baris ini sebenarnya "lahir" sedikit lebih awal, geser mundur agar jarak antar baris tetap presisi
            SpawnRow(row, spawnDistanceZ - distanceAccumulator);
        }
    }

    // =====================================================
    // PATTERN SELECTION
    // =====================================================
    private void RebuildActivePatterns()
    {
        activePatterns.Clear();
        if (useBuiltInPatterns) activePatterns.AddRange(CreateDefaultPatterns());
        if (customPatterns != null)
        {
            for (int i = 0; i < customPatterns.Count; i++)
            {
                if (customPatterns[i] != null) activePatterns.Add(customPatterns[i]);
            }
        }
    }

    private float GetDifficulty(GameManager gm)
    {
        float progress = 0f;
        if (gm.initialDistance > 0.01f) progress = 1f - gm.remainingDistance / gm.initialDistance;
        progress = Mathf.Clamp01(progress);
        return Mathf.Clamp01(difficultyCurve.Evaluate(progress));
    }

    private void RefillQueue(GameManager gm)
    {
        float difficulty = GetDifficulty(gm);

        int gapRows;
        if (!firstPatternQueued)
        {
            gapRows = Mathf.Max(0, introGapRows);
        }
        else
        {
            gapRows = Mathf.RoundToInt(Mathf.Lerp(startGapRows, endGapRows, difficulty));
            gapRows = Mathf.Max(1, gapRows);
        }
        for (int i = 0; i < gapRows; i++) rowQueue.Enqueue(emptyRow);

        int index = PickPatternIndex(difficulty);
        if (index < 0)
        {
            // Tidak ada pola valid sama sekali: tetap isi satu baris kosong supaya loop tidak macet
            rowQueue.Enqueue(emptyRow);
            return;
        }

        firstPatternQueued = true;
        recentPatterns.Enqueue(index);
        while (recentPatterns.Count > Mathf.Max(0, avoidRepeatLast)) recentPatterns.Dequeue();

        ObstaclePattern pattern = activePatterns[index];
        bool mirror = ChooseMirror(pattern.laneProfile);

        // Perbarui "ingatan" beban jalur dengan profil varian yang benar-benar dipakai
        for (int l = 0; l < LaneCount; l++)
        {
            float v = mirror ? pattern.laneProfile[LaneCount - 1 - l] : pattern.laneProfile[l];
            laneLoad[l] = laneLoad[l] * laneLoadDecay + v * (1f - laneLoadDecay);
        }

        EnqueuePattern(pattern, mirror, difficulty);
    }

    private float Dot(float[] profile, bool mirrored)
    {
        float dot = 0f;
        for (int l = 0; l < LaneCount; l++)
        {
            float v = mirrored ? profile[LaneCount - 1 - l] : profile[l];
            dot += v * laneLoad[l];
        }
        return dot;
    }

    private bool ChooseMirror(float[] profile)
    {
        if (!randomMirror) return false;

        float dotNormal = Dot(profile, false);
        float dotMirror = Dot(profile, true);

        // Pilih orientasi yang menekan jalur yang paling jarang dipaksa belakangan ini (sisakan 20% acak)
        if (balanceStrength > 0f && Mathf.Abs(dotNormal - dotMirror) > 0.02f && Random.value > 0.2f)
        {
            return dotMirror < dotNormal;
        }
        return Random.value > 0.5f;
    }

    private float BalanceMultiplier(float[] profile)
    {
        if (balanceStrength <= 0f) return 1f;

        float dot = Dot(profile, false);
        if (randomMirror) dot = Mathf.Min(dot, Dot(profile, true)); // orientasi terbaik yang nanti akan dipilih

        // dot * 3 = 1 saat beban merata. >1 berarti pola ini menekan jalur yang sudah kelewat sering dipaksa.
        float m = 1f - balanceStrength * (dot * LaneCount - 1f);
        return Mathf.Clamp(m, 0.2f, 1.6f);
    }

    private int PickPatternIndex(float difficulty)
    {
        candidateBuffer.Clear();
        weightBuffer.Clear();

        // Lulus 0: sesuai kesulitan & tidak baru dipakai. Lulus 1: abaikan anti-ulang. Lulus 2: pola valid apa saja.
        for (int pass = 0; pass < 3 && candidateBuffer.Count == 0; pass++)
        {
            for (int i = 0; i < activePatterns.Count; i++)
            {
                ObstaclePattern p = activePatterns[i];
                if (p == null || !p.isValid || p.weight <= 0f) continue;
                if (pass < 2 && (difficulty < p.minDifficulty || difficulty > p.maxDifficulty)) continue;
                if (pass == 0 && recentPatterns.Contains(i)) continue;

                candidateBuffer.Add(i);
                weightBuffer.Add(p.weight * BalanceMultiplier(p.laneProfile));
            }
        }
        if (candidateBuffer.Count == 0) return -1;

        float total = 0f;
        for (int i = 0; i < weightBuffer.Count; i++) total += weightBuffer[i];

        float roll = Random.value * total;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            roll -= weightBuffer[i];
            if (roll <= 0f) return candidateBuffer[i];
        }
        return candidateBuffer[candidateBuffer.Count - 1];
    }

    private Cell ResolveCell(Cell cell, float difficulty)
    {
        switch (cell)
        {
            case Cell.RandomBarrier:
                return Random.value > 0.5f ? Cell.Low : Cell.High;

            case Cell.RandomBlock:
                if (allowMovingObstacles && difficulty >= randomBlockMovingMinDifficulty && Random.value < randomBlockMovingChance)
                    return Cell.Moving;
                return Cell.Full;

            case Cell.Moving:
                return allowMovingObstacles ? Cell.Moving : Cell.Full;

            default:
                return cell;
        }
    }

    private void EnqueuePattern(ObstaclePattern pattern, bool mirror, float difficulty)
    {
        int n = pattern.cells.Length;
        Row[] rows = new Row[n];
        bool[][] safe = new bool[n][];

        for (int r = 0; r < n; r++)
        {
            rows[r] = new Row();
            safe[r] = (bool[])pattern.safeLanes[r].Clone();

            for (int l = 0; l < LaneCount; l++)
            {
                rows[r].lanes[l] = ResolveCell(pattern.cells[r][l], difficulty);
            }

            if (mirror)
            {
                System.Array.Reverse(rows[r].lanes);
                System.Array.Reverse(safe[r]);
            }
        }

        TryPlaceGacha(rows, safe);

        for (int r = 0; r < n; r++) rowQueue.Enqueue(rows[r]);
    }

    // =====================================================
    // GACHA ("koin") DI JALUR AMAN
    // =====================================================
    private void TryPlaceGacha(Row[] rows, bool[][] safe)
    {
        if (gachaPickupPrefab == null) return;
        if (Time.time - lastGachaTime < gachaCooldown) return;
        if (Random.value > gachaSpawnChance) return;

        int n = rows.Length;
        int[] path = new int[n];

        int lane = PickSafeLane(safe[0], -1);
        if (lane < 0) return;
        path[0] = lane;
        for (int r = 1; r < n; r++)
        {
            lane = PickSafeLane(safe[r], lane);
            if (lane < 0) return;
            path[r] = lane;
        }

        int gachaRow = Random.Range(0, n);
        int gachaLane = path[gachaRow];

        float y;
        switch (rows[gachaRow].lanes[gachaLane])
        {
            case Cell.Low: y = gachaAirY; break;    // hadiah karena melompat
            case Cell.High: y = gachaUnderY; break; // hadiah karena slide
            default: y = gachaGroundY; break;
        }

        rows[gachaRow].gachaLane = gachaLane;
        rows[gachaRow].gachaY = y;
        lastGachaTime = Time.time;
    }

    private int PickSafeLane(bool[] safeRow, int fromLane)
    {
        int count = 0;
        int chosen = -1;
        for (int l = 0; l < LaneCount; l++)
        {
            if (!safeRow[l]) continue;
            if (fromLane >= 0 && Mathf.Abs(l - fromLane) > maxLaneShiftPerRow) continue;

            count++;
            // reservoir sampling: tiap kandidat punya peluang sama tanpa alokasi list
            if (Random.Range(0, count) == 0) chosen = l;
        }
        return chosen;
    }

    // =====================================================
    // SPAWNING
    // =====================================================
    private void SpawnRow(Row row, float z)
    {
        // 1. Cek apakah ada rintangan High (kain) di baris ini
        bool hasHighObstacle = false;
        for (int l = 0; l < LaneCount; l++)
        {
            if (row.lanes[l] == Cell.High)
            {
                hasHighObstacle = true;
                break;
            }
        }

        // 2. Jika ada kain, spawn 1 tiang gawang melintang tepat di tengah jalan (X = 0)
        if (hasHighObstacle && highObstacleFramePrefab != null)
        {
            GameObject frame = Instantiate(highObstacleFramePrefab, new Vector3(0f, highFrameY, z), Quaternion.identity);
            if (frame.GetComponent<TreadmillMover>() == null)
            {
                TreadmillMover mover = frame.AddComponent<TreadmillMover>();
                mover.destroyZPosition = -15f;
            }
        }

        // 3. Spawn rintangan per jalur (kain akan spawn di jalur yang sesuai)
        for (int lane = 0; lane < LaneCount; lane++)
        {
            float x = (lane - 1) * laneDistance;
            switch (row.lanes[lane])
            {
                case Cell.Full: SpawnObstacle(fullObstaclePrefab, x, fullY, z, ObstacleType.FullBlock, false); break;
                case Cell.Moving: SpawnObstacle(movingObstaclePrefab != null ? movingObstaclePrefab : fullObstaclePrefab, x, fullY, z, ObstacleType.FullBlock, true); break;
                case Cell.Low: SpawnObstacle(lowObstaclePrefab, x, lowY, z, ObstacleType.Low_JumpOver, false); break;
                case Cell.High: SpawnObstacle(highObstaclePrefab, x, highY, z, ObstacleType.High_SlideUnder, false); break;
            }
        }

        if (row.gachaLane >= 0 && gachaPickupPrefab != null)
        {
            float gx = (row.gachaLane - 1) * laneDistance;
            Instantiate(gachaPickupPrefab, new Vector3(gx, row.gachaY, z), Quaternion.identity);
        }
    }

    private void SpawnObstacle(GameObject prefab, float x, float y, float z, ObstacleType type, bool moving)
    {
        if (prefab == null) return;

        // Kereta bergerak muncul lebih jauh agar tiba di player bersamaan dengan barisnya (lihat MovingObstacle)
        float spawnZ = moving ? z * (1f + movingApproachFactor) : z;
        GameObject go = Instantiate(prefab, new Vector3(x, y, spawnZ), Quaternion.identity);

        Obstacle obstacle = go.GetComponent<Obstacle>();
        if (obstacle != null) obstacle.obstacleType = type;

        if (moving)
        {
            MovingObstacle mover = go.GetComponent<MovingObstacle>();
            if (mover == null) mover = go.AddComponent<MovingObstacle>();
            mover.approachFactor = movingApproachFactor;
        }

        ApplyColliderForgiveness(go);
    }

    private void ApplyColliderForgiveness(GameObject go)
    {
        if (obstacleWidthForgiveness <= 0f) return;

        BoxCollider[] boxes = go.GetComponentsInChildren<BoxCollider>();
        for (int i = 0; i < boxes.Length; i++)
        {
            Vector3 size = boxes[i].size;
            size.x *= 1f - obstacleWidthForgiveness;
            boxes[i].size = size;
        }
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

    // =====================================================
    // PARSING & VALIDASI POLA
    // =====================================================
    private static bool IsBlocking(Cell cell)
    {
        return cell == Cell.Full || cell == Cell.Moving || cell == Cell.RandomBlock;
    }

    private void PreparePatterns()
    {
        int validCount = 0;
        for (int i = 0; i < activePatterns.Count; i++)
        {
            ObstaclePattern p = activePatterns[i];
            p.isValid = false;

            string error;
            if (!TryParse(p, out error))
            {
                Debug.LogWarning("[ObstacleSpawner] Pola '" + p.name + "' dilewati: " + error);
                continue;
            }

            p.safeLanes = ComputeSafeLanes(p.cells);
            bool hasSafePath = false;
            for (int l = 0; l < LaneCount; l++) if (p.safeLanes[0][l]) hasSafePath = true;

            if (!hasSafePath)
            {
                Debug.LogWarning("[ObstacleSpawner] Pola '" + p.name + "' dilewati: tidak ada jalur aman (mustahil dilewati).");
                continue;
            }

            p.laneProfile = ComputeLaneProfile(p.safeLanes);
            p.isValid = true;
            validCount++;
        }

        Debug.Log("[ObstacleSpawner] " + validCount + "/" + activePatterns.Count + " pola valid.");
    }

    private static bool TryParse(ObstaclePattern p, out string error)
    {
        error = null;
        p.cells = null;

        string source = p.rows ?? "";
        string[] lines = source.Split(new char[] { '\n', '\r', '|', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        List<Cell[]> parsed = new List<Cell[]>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Replace(" ", "").Replace("\t", "");
            if (line.Length == 0) continue;

            if (line.Length != LaneCount)
            {
                error = "baris \"" + line + "\" harus tepat " + LaneCount + " karakter.";
                return false;
            }

            Cell[] row = new Cell[LaneCount];
            for (int l = 0; l < LaneCount; l++)
            {
                switch (line[l])
                {
                    case '.': case '-': case '_': case '0': row[l] = Cell.Empty; break;
                    case 'F': case 'f': case 'X': case 'x': row[l] = Cell.Full; break;
                    case 'M': case 'm': row[l] = Cell.Moving; break;
                    case 'L': case 'l': row[l] = Cell.Low; break;
                    case 'H': case 'h': row[l] = Cell.High; break;
                    case '?': row[l] = Cell.RandomBarrier; break;
                    case '*': row[l] = Cell.RandomBlock; break;
                    default:
                        error = "karakter '" + line[l] + "' tidak dikenal (pakai . F M L H ? *).";
                        return false;
                }
            }
            parsed.Add(row);
        }

        if (parsed.Count == 0)
        {
            error = "pola kosong.";
            return false;
        }

        p.cells = parsed.ToArray();
        return true;
    }

    /// <summary>
    /// Menandai jalur mana di tiap baris yang masih bisa diselesaikan sampai baris terakhir
    /// dengan batas pindah jalur maxLaneShiftPerRow per baris. Hanya blok (F/M) yang memblokir
    /// (L bisa dilompati, H bisa di-slide).
    /// </summary>
    private bool[][] ComputeSafeLanes(Cell[][] cells)
    {
        int n = cells.Length;
        bool[][] reach = new bool[n][];
        bool[][] safe = new bool[n][];

        // Maju: jalur mana yang bisa dicapai
        for (int r = 0; r < n; r++)
        {
            reach[r] = new bool[LaneCount];
            safe[r] = new bool[LaneCount];
            for (int l = 0; l < LaneCount; l++)
            {
                if (IsBlocking(cells[r][l])) continue;

                if (r == 0)
                {
                    reach[r][l] = true;
                    continue;
                }
                for (int prev = 0; prev < LaneCount; prev++)
                {
                    if (reach[r - 1][prev] && Mathf.Abs(prev - l) <= maxLaneShiftPerRow)
                    {
                        reach[r][l] = true;
                        break;
                    }
                }
            }
        }

        // Mundur: jalur mana yang bisa lanjut sampai baris terakhir
        for (int l = 0; l < LaneCount; l++) safe[n - 1][l] = reach[n - 1][l];
        for (int r = n - 2; r >= 0; r--)
        {
            for (int l = 0; l < LaneCount; l++)
            {
                if (!reach[r][l]) continue;
                for (int next = 0; next < LaneCount; next++)
                {
                    if (safe[r + 1][next] && Mathf.Abs(next - l) <= maxLaneShiftPerRow)
                    {
                        safe[r][l] = true;
                        break;
                    }
                }
            }
        }
        return safe;
    }

    /// <summary>
    /// Profil beban jalur: di jalur mana pola ini "memaksa" player berada.
    /// Baris dengan 1 jalur aman memaksa penuh jalur itu, baris dengan 2 jalur aman memaksa setengah,
    /// baris dengan 3 jalur aman tidak memaksa apa pun. Hasil dinormalisasi (jumlah = 1).
    /// </summary>
    private static float[] ComputeLaneProfile(bool[][] safe)
    {
        float[] profile = new float[LaneCount];
        float total = 0f;

        for (int r = 0; r < safe.Length; r++)
        {
            int count = 0;
            for (int l = 0; l < LaneCount; l++) if (safe[r][l]) count++;
            if (count == 0 || count == LaneCount) continue;

            float constraint = (LaneCount - count) / (float)(LaneCount - 1);
            for (int l = 0; l < LaneCount; l++)
            {
                if (!safe[r][l]) continue;
                float w = constraint / count;
                profile[l] += w;
                total += w;
            }
        }

        for (int l = 0; l < LaneCount; l++)
        {
            profile[l] = total > 0.0001f ? profile[l] / total : 1f / LaneCount;
        }
        return profile;
    }

    // =====================================================
    // DEFAULT PATTERN LIBRARY
    // =====================================================
    [ContextMenu("Copy Built-In Patterns To Custom List")]
    private void CopyBuiltInToCustom()
    {
        customPatterns = CreateDefaultPatterns();
        useBuiltInPatterns = false;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[ObstacleSpawner] " + customPatterns.Count + " pola bawaan disalin ke Custom Patterns dan 'Use Built In Patterns' dimatikan. Sekarang bisa diedit bebas.");
    }

    [ContextMenu("Validate Patterns")]
    private void ValidatePatternsFromMenu()
    {
        RebuildActivePatterns();
        PreparePatterns();
    }

    private static ObstaclePattern P(string name, float minDifficulty, float weight, params string[] rows)
    {
        ObstaclePattern p = new ObstaclePattern();
        p.name = name;
        p.minDifficulty = minDifficulty;
        p.weight = weight;
        p.rows = string.Join("\n", rows);
        return p;
    }

    private static List<ObstaclePattern> CreateDefaultPatterns()
    {
        List<ObstaclePattern> list = new List<ObstaclePattern>();

        // ---------- TIER 1: pembuka, mudah (difficulty 0.0+) ----------
        list.Add(P("Left Wall",        0.0f, 1.0f, "F.."));
        list.Add(P("Center Pillar",    0.0f, 0.6f, ".F."));
        list.Add(P("Double Wall",      0.0f, 1.0f, "FF."));
        list.Add(P("Low Wall",         0.0f, 0.8f, "LLL"));
        list.Add(P("High Sign",        0.0f, 0.8f, "HHH"));
        list.Add(P("Side Hurdles",     0.0f, 1.0f, "L.L"));
        list.Add(P("Side Ducks",       0.0f, 1.0f, "H.H"));
        list.Add(P("Mixed Hurdles",    0.0f, 0.8f, "L.H"));
        list.Add(P("Wall And Hurdle",  0.0f, 1.0f, "F.L"));
        list.Add(P("Wall And Duck",    0.0f, 1.0f, "F.H"));
        list.Add(P("Lone Barrier",     0.0f, 0.6f, "?.."));
        list.Add(P("Side Walls",       0.1f, 0.8f, "F.F"));
        list.Add(P("Center Hurdle",    0.1f, 0.7f, "FLF"));
        list.Add(P("Center Duck",      0.1f, 0.7f, "FHF"));

        // ---------- TIER 2: menengah (difficulty 0.2+) ----------
        list.Add(P("Easy Weave",       0.2f, 1.0f, "FF.", "...", ".FF"));
        list.Add(P("Jump Then Duck",   0.2f, 1.0f, "LLL", "...", "HHH"));
        list.Add(P("Duck Then Jump",   0.2f, 1.0f, "HHH", "...", "LLL"));
        list.Add(P("Staggered Hurdles",0.2f, 0.9f, "LL.", "..L"));
        list.Add(P("Edge Run",         0.2f, 1.0f, ".FF", ".FF", "?FF"));
        list.Add(P("Triple Action",    0.2f, 1.0f, "LHF"));
        list.Add(P("Double Jump",      0.2f, 0.6f, "FLF", "...", "FLF"));
        list.Add(P("Train Wall",       0.2f, 1.0f, "*F."));
        list.Add(P("Train Corridor",   0.3f, 0.8f, "*.*"));
        list.Add(P("Oncoming Single",  0.25f, 1.0f, "M.."));
        list.Add(P("Oncoming Pair",    0.3f, 1.0f, "MM."));
        list.Add(P("Train And Hurdle", 0.3f, 1.0f, "M.L"));

        // ---------- TIER 3: sulit (difficulty 0.45+) ----------
        list.Add(P("Staircase",        0.45f, 1.0f, "FF.", "F.F", ".FF"));
        list.Add(P("Staircase Hurdle", 0.45f, 1.0f, "FF.", ".LF", ".FF"));
        list.Add(P("Dodge And Duck",   0.45f, 1.0f, "LHF", "...", "F.H"));
        list.Add(P("Hurdle Slalom",    0.45f, 1.0f, "FL.", "L.F"));
        list.Add(P("Gauntlet",         0.45f, 0.6f, "FHF", "...", "FLF", "...", "FHF"));
        list.Add(P("Oncoming Weave",   0.5f, 1.0f, "MM.", "...", ".MM"));
        list.Add(P("Train Sandwich",   0.5f, 0.8f, "M.M"));
        list.Add(P("Edge Duck",        0.45f, 1.0f, ".FF", "HFF"));
        list.Add(P("Edge Hurdle Run",  0.45f, 1.0f, "FF.", "FFL"));

        // ---------- TIER 4: puncak kesulitan (difficulty 0.7+) ----------
        list.Add(P("Tight Weave",      0.7f, 1.0f, "FF.", "F.F", ".FF", "F.F", "FF."));
        list.Add(P("Action Chain",     0.7f, 1.0f, "LLL", "HHH", "LLL"));
        list.Add(P("Zigzag Hurdles",   0.7f, 0.9f, "FFL", "FL.", "L.F", "FL.", "FFL"));
        list.Add(P("Oncoming Zigzag",  0.7f, 1.0f, "MM.", "M.M", ".MM"));
        list.Add(P("Edge Gauntlet",    0.7f, 1.0f, ".FF", "HFF", ".FF", "LFF"));
        list.Add(P("Side Swap Trains", 0.7f, 1.0f, "MM.", "...", "...", ".MM"));

        return list;
    }

    // =====================================================
    // GIZMO
    // =====================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        for (int lane = 0; lane < LaneCount; lane++)
        {
            float x = (lane - 1) * laneDistance;
            Gizmos.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, 0f, spawnDistanceZ));
            Gizmos.DrawWireCube(new Vector3(x, 0.5f, spawnDistanceZ), new Vector3(laneDistance * 0.8f, 1f, 0.2f));
        }
    }
}