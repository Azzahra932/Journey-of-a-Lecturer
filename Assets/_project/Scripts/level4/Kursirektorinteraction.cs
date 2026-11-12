using UnityEngine;
using UnityEngine.Events;

// Kursi khusus ruang rektor (Level 4 Scene 4). Berdiri sendiri, TIDAK memakai InteractiveDesk,
// jadi tidak ada hubungannya dengan evaluasi / bimbingan / script kursi dosen lainnya.
//   - Dekat kursi, tekan Space: MC duduk (gambar MC biasa disembunyikan, gambar duduk "Sit" ditampilkan)
//   - Saat duduk: berdiri dengan WASD / tombol panah (Space TIDAK membuat MC berdiri, jadi aman dipakai untuk menutup dialog)
//   - Adegan bisa mengunci MC di kursi lewat TerkunciEksternal
// Pasang script ini di objek kursi rektor (atau objek kosong di dekatnya), lalu isi Sprite Duduk dengan objek gambar MC duduk.
public class KursiRektorInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode tombolDuduk = KeyCode.Space;     // Space untuk duduk
    public float jarakArea = 1.2f;                  // Jarak jangkauan area kursi (lingkaran biru terlihat di Scene saat objek ini dipilih)
    public Transform titikArea;                     // Kosong = posisi objek ini
    public bool bolehDipakai = true;                // false = kursi tidak bisa diduduki (mis. sebelum adegan siap)

    [Header("Karakter")]
    public GameObject playerCharacter;              // TPdiam (kosong = dicari otomatis)
    public GameObject spriteDuduk;                  // Objek "Sit": gambar MC duduk di kursi rektor
    public Sprite spriteDudukBaru;                  // Opsional: gambar duduk yang dipasang ke objek Sprite Duduk saat MC duduk (mis. sprite MC level 4)

    [Header("Pengaturan Berdiri")]
    public Transform titikBerdiri;                        // Opsional: MC berdiri TEPAT di titik ini
    public Vector2 offsetBerdiri = new Vector2(0f, -1f);  // Kalau Titik Berdiri kosong: relatif dari Sprite Duduk
    public float jedaSetelahDuduk = 0.3f;                 // Cegah langsung berdiri tepat setelah duduk

    [Header("Petunjuk (Opsional)")]
    public GameObject petunjukTekanSpace;           // UI petunjuk "Tekan [Space]" untuk duduk

    [Header("Event (Opsional)")]
    public UnityEvent saatDuduk;
    public UnityEvent saatBerdiri;

    [HideInInspector] public bool sedangDuduk = false;

    // Sama seperti InteractiveDesk dan KursiPerpusInteraction: dipakai script adegan
    public bool SedangDuduk { get { return sedangDuduk; } }

    // Diset script lain (adegan) untuk mengunci MC di kursi, supaya tidak bisa berdiri selama adegan
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
                Debug.LogWarning("[Kursi Rektor] Objek pemain (TPdiam) tidak ditemukan, kursi tidak bisa dipakai. Drag TPdiam ke slot Player Character.");
            }
            return;
        }

        // Sedang duduk: berdiri dengan WASD / tombol panah (tidak bisa saat adegan mengunci MC)
        if (sedangDuduk)
        {
            if (petunjukTekanSpace != null) petunjukTekanSpace.SetActive(false);

            if (TombolGerakDitekan()
                && Time.time - waktuDuduk >= jedaSetelahDuduk
                && !TerkunciEksternal)
            {
                Berdiri();
            }
            return;
        }

        float jarak = Vector2.Distance(titikArea.position, playerCharacter.transform.position);
        bool dekat = bolehDipakai && jarak <= jarakArea;

        if (petunjukTekanSpace != null) petunjukTekanSpace.SetActive(dekat);

        // Diagnosis: kalau tombol duduk ditekan tapi tidak bisa duduk, Console menjelaskan alasannya
        if (!dekat && Input.GetKeyDown(tombolDuduk) && jarak <= jarakArea * 4f)
        {
            if (!bolehDipakai)
                Debug.Log("[Kursi Rektor] Tombol duduk ditekan, tapi kursi belum bisa dipakai (Boleh Dipakai belum dicentang).");
            else
                Debug.Log("[Kursi Rektor] Tombol duduk ditekan, tapi MC terlalu jauh dari kursi (jarak " + jarak.ToString("0.00") + ", batas " + jarakArea + ").");
        }

        if (dekat && Input.GetKeyDown(tombolDuduk))
            Duduk();
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

        // Pemain biasa dimatikan, diganti gambar duduk
        playerCharacter.SetActive(false);

        if (spriteDuduk != null)
        {
            if (spriteDudukBaru != null)
            {
                SpriteRenderer sr = spriteDuduk.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = spriteDudukBaru;
            }

            spriteDuduk.SetActive(true);
        }

        saatDuduk.Invoke();

        Debug.Log("[Kursi Rektor] Pemain duduk.");
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

        saatBerdiri.Invoke();

        Debug.Log("[Kursi Rektor] Pemain berdiri.");
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