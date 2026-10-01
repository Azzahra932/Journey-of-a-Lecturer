using UnityEngine;

public class NpcFloating : MonoBehaviour
{
    [Header("Pengaturan Gerakan Naik Turun")]
    public float speed = 5.0f;      // Kecepatan gerakan naik-turun
    public float height = 0.1f;     // Jarak/tinggi pergeseran (buat kecil saja agar halus)

    private Vector3 startPos;

    void Start()
    {
        // Simpan posisi awal NPC saat game dimulai
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Menggunakan fungsi Sin untuk membuat gerakan naik turun yang mulus
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;

        // Terapkan posisi baru pada objek NPC
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}