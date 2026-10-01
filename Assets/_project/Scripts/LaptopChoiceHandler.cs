using UnityEngine;

public class LaptopChoiceHandler : MonoBehaviour
{
    [Header("Referensi Box Pilihan")]
    public GameObject box1;
    public GameObject box2;

    [Header("Referensi Sistem Waktu")]
    public GameTimeManager timeManager;

    [Header("Referensi Interaksi Laptop")]
    public LaptopInteraction laptopInteraction;

    void Start()
    {
        if (box1 != null) box1.SetActive(false);
        if (box2 != null) box2.SetActive(false);
    }

    public void OpenChoices()
    {
        if (box1 != null) box1.SetActive(true);
        if (box2 != null) box2.SetActive(true);
    }

    public void ChooseHonestOption()
    {
        Debug.Log("Memilih: Menyusun materi secara mandiri (Jujur)");


        SelesaikanPilihan(20.0f);
    }

    public void ChooseAIOption()
    {
        Debug.Log("Memilih: Menggunakan AI Generator (Cepat, Integritas -10%)");

        // Memanggil fungsi pengurangan integritas dari manager khusus
        if (IntegritasManager.Instance != null)
        {
            IntegritasManager.Instance.ReduceIntegritas(10f);
        }
        else
        {
            Debug.LogWarning("[LaptopChoiceHandler] IntegritasManager.Instance tidak ditemukan di Scene!");
        }

        SelesaikanPilihan(3.0f);
    }

    void SelesaikanPilihan(float duration)
    {
        CloseChoices();

        if (laptopInteraction != null)
        {
            laptopInteraction.MulaiPekerjaanDariPilihan(duration);
        }
    }

    void CloseChoices()
    {
        if (box1 != null) box1.SetActive(false);
        if (box2 != null) box2.SetActive(false);
    }
}