using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [Header("Pengaturan Gerakan")]
    public float amplitude = 0.5f; // Jarak naik-turun
    public float frequency = 5f;    // Kecepatan ayunan

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Efek naik-turun halus pakai gelombang sin
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}