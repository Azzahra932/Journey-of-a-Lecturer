using UnityEngine;

public class PanahGerakKiriKanan : MonoBehaviour
{
    [Header("Pengaturan Gerakan")]
    public float speed = 3f;        // Kecepatan gerak bolak-balik
    public float distance = 15f;    // Jarak jangkauan geser (dalam piksel UI)

    private Vector3 startPos;

    void OnEnable()
    {
        // Menyimpan posisi awal panah saat pertama kali diaktifkan
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Menghitung pergerakan sinosoidal (bolak-balik halus ke kiri dan kanan)
        float newX = startPos.x + (Mathf.Sin(Time.time * speed) * distance);

        // Memperbarui posisi panah
        transform.localPosition = new Vector3(newX, startPos.y, startPos.z);
    }
}