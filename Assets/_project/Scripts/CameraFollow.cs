using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target & Smoothness")]
    public Transform target; // Drag karakter pemain (TPdiam) ke sini
    public float smoothSpeed = 10f; // Kecepatan kamera mengikuti
    public Vector3 offset = new Vector3(0, 0, -10); // Mengunci Z di -10

    [Header("Camera Bounds (Batas Map)")]
    public bool useBounds = true; // Centang di Inspector untuk mengaktifkan batas
    public float minX; // Batas paling kiri yang boleh dilihat kamera
    public float maxX; // Batas paling kanan yang boleh dilihat kamera
    public float minY; // Batas paling bawah yang boleh dilihat kamera
    public float maxY; // Batas paling atas yang boleh dilihat kamera

    void LateUpdate()
    {
        if (target != null)
        {
            // 1. Hitung posisi target yang ingin dituju
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, 0) + offset;

            // 2. Jika fitur batas aktif, kunci posisi X dan Y kamera di dalam rentang batas
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            }

            // 3. Pergerakan halus ke posisi yang sudah dibatasi
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }
}