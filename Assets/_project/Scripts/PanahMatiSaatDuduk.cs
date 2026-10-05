using UnityEngine;

/// <summary>
/// Pasang LANGSUNG di objek panah (mis. panahKeKursiDosen).
/// Selama panah tampil, script ini mengecek apakah player sedang duduk.
/// Begitu player duduk, panah dimatikan.
/// </summary>
public class PanahMatiSaatDuduk : MonoBehaviour
{
    [Header("Player (TPdiam yang punya PlayerMovement)")]
    public PlayerMovement player;     // kosong = dicari otomatis

    void OnEnable()
    {
        if (player == null) player = FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (player == null) return;

        if (player.isSitting)
        {
            Debug.Log("[PanahMatiSaatDuduk] Player duduk, panah " + gameObject.name + " dimatikan.");
            gameObject.SetActive(false);
        }
    }
}