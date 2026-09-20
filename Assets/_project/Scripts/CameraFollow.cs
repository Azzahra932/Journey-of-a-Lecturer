using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Drag karakter pemain (TPdiam) ke sini
    public float smoothSpeed = 10f; // Kecepatan kamera mengikuti
    public Vector3 offset = new Vector3(0, 0, -10); // Mengunci Z di -10

    void LateUpdate()
    {
        if (target != null)
        {
            // Ambil posisi pemain + jaga Z tetap -10
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, 0) + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }
}