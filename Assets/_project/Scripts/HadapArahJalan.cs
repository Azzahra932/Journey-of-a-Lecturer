using UnityEngine;

// Membalik gambar (flipX) sesuai arah jalan horizontal, jadi saat pergi cukup memakai
// gambar yang sama dengan saat datang. Tidak bergantung pada script yang menggerakkan NPC.
public class HadapArahJalan : MonoBehaviour
{
    [Header("Gambar")]
    public SpriteRenderer spriteRenderer;       // Kosong = ambil dari objek ini atau anaknya
    public bool gambarMenghadapKanan = true;    // Gambar aslinya (saat datang) menghadap kanan? Kalau menghadap kiri, matikan centang

    [Header("Pengaturan")]
    public float batasGerak = 0.002f;           // Gerak horizontal di bawah ini dianggap diam
    public bool kembaliNormalSaatDiam = true;   // Saat diam, gambar dikembalikan ke arah aslinya (tidak terbalik)
    public float jedaDiam = 0.2f;               // Lama diam (detik) sebelum dikembalikan

    private float xSebelumnya;
    private float waktuTerakhirGerak;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        xSebelumnya = transform.position.x;
        waktuTerakhirGerak = Time.time;
    }

    void LateUpdate()
    {
        if (spriteRenderer == null || Time.timeScale == 0f) return;

        float dx = transform.position.x - xSebelumnya;
        xSebelumnya = transform.position.x;

        if (Mathf.Abs(dx) > batasGerak)
        {
            waktuTerakhirGerak = Time.time;
            bool menujuKanan = dx > 0f;

            // Gambar menghadap kanan: balik saat jalan ke kiri. Gambar menghadap kiri: balik saat jalan ke kanan.
            spriteRenderer.flipX = gambarMenghadapKanan ? !menujuKanan : menujuKanan;
        }
        else if (kembaliNormalSaatDiam && Time.time - waktuTerakhirGerak > jedaDiam)
        {
            spriteRenderer.flipX = false;
        }
    }
}