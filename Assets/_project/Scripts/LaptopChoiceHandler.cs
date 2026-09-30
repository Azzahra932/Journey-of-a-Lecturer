using UnityEngine;

public class LaptopChoiceHandler : MonoBehaviour
{
    [Header("Referensi Box Pilihan")]
    public GameObject box1; // Seret box1 ke sini
    public GameObject box2; // Seret box2 ke sini

    [Header("Referensi Sistem Waktu")]
    public GameTimeManager timeManager;

    void Start()
    {
        // Pastikan box1 dan box2 mati/tidak muncul di awal game
        if (box1 != null) box1.SetActive(false);
        if (box2 != null) box2.SetActive(false);
    }

    // Fungsi untuk memunculkan pilihan saat laptop diklik/ditekan E
    public void OpenChoices()
    {
        if (box1 != null) box1.SetActive(true);
        if (box2 != null) box2.SetActive(true);
    }

    // Fungsi saat box1 (Atas) diklik -> Jujur, waktu lebih lama
    public void ChooseHonestOption()
    {
        Debug.Log("Memilih: Menyusun materi secara mandiri (Jujur)");

        if (timeManager != null)
        {
            timeManager.AdvanceTime();
            timeManager.AdvanceTime();
        }

        CloseChoices();
    }

    // Fungsi saat box2 (Bawah) diklik -> Pakai AI, cepat, integritas -10%
    public void ChooseAIOption()
    {
        Debug.Log("Memilih: Menggunakan AI Generator (Cepat, Integritas -10%)");

        if (timeManager != null)
        {
            timeManager.AdvanceTime();
        }

        ReduceIntegritas(10f);
        CloseChoices();
    }

    void ReduceIntegritas(float amount)
    {
        Debug.Log($"Integritas berkurang sebesar {amount}%");
    }

    void CloseChoices()
    {
        if (box1 != null) box1.SetActive(false);
        if (box2 != null) box2.SetActive(false);
    }
}