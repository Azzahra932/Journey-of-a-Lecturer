using UnityEngine;

public class ChairInteraction : MonoBehaviour
{
    [Header("Pengaturan Kursi & Karakter")]
    public GameObject playerCharacter;   // Referensi ke GameObject Player utama
    public GameObject lecturerSitting;   // Referensi ke GameObject karakter dosen yang sedang duduk
    public GameObject promptUI;          // UI petunjuk (misal "Tekan E untuk Duduk")

    private bool playerIsClose = false;
    private bool isSitting = false;

    void Start()
    {
        // Pastikan saat mulai, dosen disembunyikan dan UI petunjuk dimatikan
        if (lecturerSitting != null) lecturerSitting.SetActive(false);
        if (promptUI != null) promptUI.SetActive(false);

        // Jika playerCharacter belum di-assign di Inspector, coba cari otomatis
        if (playerCharacter == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerCharacter = playerObj;
        }
    }

    void Update()
    {
        // Jika pemain dekat, belum duduk, dan menekan tombol 'E'
        if (playerIsClose && !isSitting && Input.GetKeyDown(KeyCode.E))
        {
            DudukDanGantiKarakter();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Deteksi jika Player memasuki area pemicu kursi
        if (other.gameObject == playerCharacter && !isSitting)
        {
            playerIsClose = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Deteksi jika Player meninggalkan area pemicu kursi
        if (other.gameObject == playerCharacter)
        {
            playerIsClose = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    void DudukDanGantiKarakter()
    {
        isSitting = true;
        if (promptUI != null) promptUI.SetActive(false);

        // 1. Sembunyikan karakter pemain utama
        if (playerCharacter != null)
        {
            playerCharacter.SetActive(false);
        }

        // 2. Aktifkan karakter dosen yang sedang duduk
        if (lecturerSitting != null)
        {
            // Posisikan dosen tepat di kursi (opsional, sebaiknya sudah diatur posisinya sebelumnya)
            // lecturerSitting.transform.position = transform.position; // Kalau titik duduknya sama persis dengan pusat kursi
            lecturerSitting.SetActive(true);
        }

        Debug.Log("[ChairInteraction] Karakter diganti dengan dosen yang sedang duduk.");

        // (Opsional) Di sini bisa ditambahkan logika lain, misalnya memulai dialog atau cutscene
    }
}