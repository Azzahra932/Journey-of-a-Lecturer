using UnityEngine;

public class KursiPerpusInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode tombolDuduk = KeyCode.Space;     // Space untuk duduk
    public float jarakArea = 1.2f;                  // Jarak jangkauan area kursi
    public Transform titikArea;                     // Kosong = posisi objek ini

    [Header("Karakter")]
    public GameObject playerCharacter;              // TPdiam (kosong = dicari otomatis)
    public GameObject spriteDuduk;                  // Objek "Sit" (gambar pemain duduk di kursi perpus)

    [Header("Pengaturan Berdiri")]
    public Transform titikBerdiri;                        // Opsional: pemain berdiri TEPAT di titik ini
    public Vector2 offsetBerdiri = new Vector2(0f, -1f);  // Kalau titikBerdiri kosong: relatif dari spriteDuduk
    public float jedaSetelahDuduk = 0.3f;                 // Cegah langsung berdiri tepat setelah duduk

    [Header("Syarat")]
    public BukuInteraction buku;                    // Kursi baru bisa dipakai setelah buku terbuka (Scene 5)

    [Header("Level 2 (buku diganti laptop)")]
    public bool tanpaSyaratBukuDiLevel2 = true;     // true = di level 2 kursi bisa dipakai tanpa menunggu buku terbuka
    public int targetSkorLevel1 = 150;              // Target skor level 1. Target skor yang lebih besar dari ini berarti level 2 sudah dimulai

    [Header("Petunjuk (Opsional)")]
    public GameObject petunjukTekanSpace;           // UI petunjuk "Tekan [Space]" untuk duduk

    [HideInInspector] public bool sedangDuduk = false;

    // Diset script lain (mis. adegan Level 2) untuk mengunci MC di kursi, supaya tidak bisa berdiri selama adegan
    public bool TerkunciEksternal { get; set; }

    private float waktuDuduk = 0f;
    private TransisiKembaliKeAwal transisi;
    private bool peringatanPemainDitampilkan = false;

    void Start()
    {
        if (spriteDuduk != null) spriteDuduk.SetActive(false);
        if (petunjukTekanSpace != null) petunjukTekanSpace.SetActive(false);
        if (titikArea == null) titikArea = transform;

        if (playerCharacter == null)
        {
            GameObject playerObj = GameObject.Find("TPdiam");
            if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerCharacter = playerObj;
        }

        // Kalau energi habis saat duduk, pemain dikembalikan aktif setelah transisi selesai
        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();
        if (transisi != null) transisi.saatTransisiSelesai.AddListener(PaksaBerdiriSegera);
    }

    void OnDestroy()
    {
        if (transisi != null) transisi.saatTransisiSelesai.RemoveListener(PaksaBerdiriSegera);
    }

    // Lingkaran biru di tab Scene (saat objek ini dipilih): area MC bisa menekan tombol duduk
    void OnDrawGizmosSelected()
    {
        Transform acuan = (titikArea != null) ? titikArea : transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(acuan.position, jarakArea);
    }

    void Update()
    {
        if (playerCharacter == null)
        {
            if (!peringatanPemainDitampilkan)
            {
                peringatanPemainDitampilkan = true;
                Debug.LogWarning("[Kursi Perpus] Objek pemain (TPdiam) tidak ditemukan, kursi tidak bisa dipakai. Drag TPdiam ke slot Player Character.");
            }
            return;
        }

        // Sedang duduk: berdiri dengan WASD / tombol panah (tidak bisa saat memilih atau membaca)
        if (sedangDuduk)
        {
            if (petunjukTekanSpace != null) petunjukTekanSpace.SetActive(false);

            if (TombolGerakDitekan()
                && Time.time - waktuDuduk >= jedaSetelahDuduk
                && !TerkunciEksternal
                && (buku == null || !buku.SedangSibuk))
            {
                Berdiri();
            }
            return;
        }

        bool bisaDipakai = (buku == null) || buku.terbuka || (tanpaSyaratBukuDiLevel2 && Level2Aktif());
        float jarak = Vector2.Distance(titikArea.position, playerCharacter.transform.position);
        bool dekat = bisaDipakai && jarak <= jarakArea;

        if (petunjukTekanSpace != null) petunjukTekanSpace.SetActive(dekat);

        // Diagnosis: kalau tombol duduk ditekan tapi tidak bisa duduk, Console menjelaskan alasannya
        if (!dekat && Input.GetKeyDown(tombolDuduk))
        {
            if (!bisaDipakai)
                Debug.Log("[Kursi Perpus] Tombol duduk ditekan, tapi kursi belum bisa dipakai: aktivitas buku belum 'terbuka' (kursi baru bisa dipakai setelah buku terbuka).");
            else
                Debug.Log("[Kursi Perpus] Tombol duduk ditekan, tapi MC terlalu jauh dari kursi (jarak " + jarak.ToString("0.00") + ", batas " + jarakArea + ").");
        }

        if (dekat && Input.GetKeyDown(tombolDuduk))
            Duduk();
    }

    // Level 2 dianggap sudah dimulai kalau target skor sudah naik (150 -> 300) setelah pop-up Level Selesai
    bool Level2Aktif()
    {
        return ScoreManager.Instance != null && ScoreManager.Instance.targetScore > targetSkorLevel1;
    }

    bool TombolGerakDitekan()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    private void Duduk()
    {
        sedangDuduk = true;
        waktuDuduk = Time.time;

        if (petunjukTekanSpace != null) petunjukTekanSpace.SetActive(false);

        // Pemain biasa dimatikan, diganti sprite duduk
        playerCharacter.SetActive(false);
        if (spriteDuduk != null) spriteDuduk.SetActive(true);

        Debug.Log("[Kursi Perpus] Pemain duduk.");
    }

    private void Berdiri()
    {
        sedangDuduk = false;

        // Posisikan pemain berdiri di dekat kursi
        Vector3 pos = playerCharacter.transform.position;
        if (titikBerdiri != null)
            pos = new Vector3(titikBerdiri.position.x, titikBerdiri.position.y, pos.z);
        else if (spriteDuduk != null)
            pos = new Vector3(spriteDuduk.transform.position.x + offsetBerdiri.x,
                              spriteDuduk.transform.position.y + offsetBerdiri.y, pos.z);
        playerCharacter.transform.position = pos;

        if (spriteDuduk != null) spriteDuduk.SetActive(false);
        playerCharacter.SetActive(true);

        PlayerMovement pm = playerCharacter.GetComponent<PlayerMovement>();
        if (pm != null) pm.Berdiri();   // reset isSitting & sorting order

        Debug.Log("[Kursi Perpus] Pemain berdiri.");
    }

    // Dipanggil saat transisi (energi habis) selesai: pemain aktif lagi di posisi barunya
    public void PaksaBerdiriSegera()
    {
        if (!sedangDuduk) return;

        sedangDuduk = false;
        if (spriteDuduk != null) spriteDuduk.SetActive(false);

        if (playerCharacter != null)
        {
            playerCharacter.SetActive(true);

            PlayerMovement pm = playerCharacter.GetComponent<PlayerMovement>();
            if (pm != null) pm.Berdiri();
        }
    }
}