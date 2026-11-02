using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Level 2 - Laptop perpustakaan (menggantikan buku setelah Scene 4).
// Terbuka setelah Scene 4 selesai (dipanggil otomatis dari AdeganLevel2Scene4, atau lewat BukaAktivitasBerulang()).
// Alur: MC duduk di kursi perpustakaan -> ikon E -> tekan E -> dua box pilihan (Jujur / Curang)
//       -> bar hijau berputar -> poin -> reward -> energi berkurang -> cooldown (layar laptop redup + baterai).
public class LaptopPerpusInteraction : MonoBehaviour
{
    [Header("Status")]
    public bool terbuka = false;                // Baru bisa dipakai setelah Scene 4 selesai

    [Header("Syarat Duduk")]
    public KursiPerpusInteraction kursiPerpus;  // MC harus duduk di kursi perpustakaan
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Ikon E")]
    public GameObject petunjukTekanE;           // Ikon E di atas laptop (boleh objek yang sama dengan Ikon E di adegan Scene 4)

    [Header("Pilihan Jujur / Curang")]
    public GameObject boxPilihan1;              // Box pilihan jujur
    public GameObject boxPilihan2;              // Box pilihan curang
    public Button tombolJujur;                  // Button di box jujur (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolCurang;                 // Button di box curang (jangan isi On Click di Inspector, dipasang lewat script)
    public float durasiJujur = 10f;             // Lama bar berputar kalau jujur (detik)
    public float durasiCurang = 5f;             // Lama bar berputar kalau curang (detik)
    public float penguranganIntegritasCurang = 10f;   // Integritas berkurang kalau curang

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)

    [Header("Hasil")]
    public int jumlahPoin = 10;                 // Poin tiap selesai
    public int indexReward = 5;                 // Nomor reward di RewardManager (5 = Karya Ilmiah). -1 = tanpa reward
    public float biayaEnergi = 15f;             // Energi yang berkurang tiap selesai
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Cooldown Baterai")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Kosong = ambil dari objek ini
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    private bool sedangBekerja = false;
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;
    private float waktuTerbuka = 0f;
    private int pilihan = 0;                    // 0 = belum memilih, 1 = jujur, 2 = curang

    public bool SedangSibuk { get { return sedangBekerja; } }

    void Start()
    {
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (boxPilihan1 != null) boxPilihan1.SetActive(false);
        if (boxPilihan2 != null) boxPilihan2.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);

        if (spriteLaptop == null) spriteLaptop = GetComponent<SpriteRenderer>();
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        if (tombolJujur != null) tombolJujur.onClick.AddListener(PilihJujur);
        if (tombolCurang != null) tombolCurang.onClick.AddListener(PilihCurang);
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolCurang != null) tombolCurang.onClick.RemoveListener(PilihCurang);
    }

    // Dipanggil setelah Scene 4 selesai
    public void BukaAktivitasBerulang()
    {
        terbuka = true;
        waktuTerbuka = Time.time;
        cooldownSelesaiPada = 0f;
        Debug.Log("[LaptopPerpus] Aktivitas karya ilmiah terbuka untuk dikerjakan berulang.");
    }

    public void PilihJujur()
    {
        if (pilihan == 0) pilihan = 1;
    }

    public void PilihCurang()
    {
        if (pilihan == 0) pilihan = 2;
    }

    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        // Tampilan cooldown: baterai terisi bertahap
        if (cooldownAktif)
        {
            float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);

            if (indikatorBaterai != null && durasiCooldown > 0f)
                indikatorBaterai.SetProgress(1f - (sisa / durasiCooldown));

            if (Time.time >= cooldownSelesaiPada) SelesaiCooldown();
        }

        if (!terbuka || sedangBekerja)
        {
            // Sebelum terbuka, ikon E dikelola adegan Scene 4 (boleh objek yang sama), jadi tidak disentuh di sini.
            // Ikon hanya dimatikan selama laptop sedang dipakai.
            if (sedangBekerja && petunjukTekanE != null) petunjukTekanE.SetActive(false);
            return;
        }

        bool duduk = (kursiPerpus == null) || kursiPerpus.sedangDuduk;
        bool bolehTekanE = Time.time - waktuTerbuka >= 0.5f;   // cegah tombol E dari adegan sebelumnya terbaca lagi

        // Selama cooldown, E tidak membuka pilihan
        if (Time.time < cooldownSelesaiPada)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

            if (duduk && bolehTekanE && Input.GetKeyDown(tombolInteraksi) && teksIstirahat != null)
            {
                teksIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
            return;
        }

        if (petunjukTekanE != null) petunjukTekanE.SetActive(duduk);

        if (duduk && bolehTekanE && Input.GetKeyDown(tombolInteraksi))
            StartCoroutine(Alur());
    }

    private IEnumerator Alur()
    {
        sedangBekerja = true;
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = true;

        // 1. Dua box pilihan (jujur / curang)
        pilihan = 0;
        if (boxPilihan1 != null) boxPilihan1.SetActive(true);
        if (boxPilihan2 != null) boxPilihan2.SetActive(true);

        while (pilihan == 0) yield return null;

        if (boxPilihan1 != null) boxPilihan1.SetActive(false);
        if (boxPilihan2 != null) boxPilihan2.SetActive(false);

        float durasi = durasiJujur;

        if (pilihan == 2)
        {
            durasi = durasiCurang;

            if (IntegritasManager.Instance != null)
                IntegritasManager.Instance.ReduceIntegritas(penguranganIntegritasCurang);
            else
                Debug.LogWarning("[LaptopPerpus] IntegritasManager.Instance tidak ditemukan di Scene!");
        }

        // 2. Bar hijau berputar
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;

        float timer = 0f;
        while (timer < durasi)
        {
            timer += Time.deltaTime;
            if (circularProgressBar != null) circularProgressBar.fillAmount = timer / durasi;
            yield return null;
        }

        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        // 3. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[LaptopPerpus] Karya ilmiah selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[LaptopPerpus] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 4. Reward muncul sendiri, lalu hilang sendiri
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // 5. Energi berkurang, lalu cek apakah habis
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 6. Cooldown: layar laptop redup + baterai
        MulaiCooldown();

        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = false;
        sedangBekerja = false;
    }

    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (indikatorBaterai != null) indikatorBaterai.SetProgress(0f);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(true);

        if (teksCooldown != null)
        {
            teksCooldown.text = isiTeksCooldown;
            teksCooldown.gameObject.SetActive(true);
        }

        if (spriteLaptop != null) spriteLaptop.color = warnaCooldown;
    }

    private void SelesaiCooldown()
    {
        cooldownAktif = false;

        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
    }
}