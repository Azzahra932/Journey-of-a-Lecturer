using UnityEngine;

public class CoffeeMachine : MonoBehaviour
{
    [Header("Referensi Energy Bar")]
    public EnergyBar energyBar;

    private bool isPlayerNearby = false;

    private void Update()
    {
        // Jika pemain berada di dekat mesin kopi DAN menekan Space
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.Space))
        {
            RefillEnergy();
        }
    }

    private void RefillEnergy()
    {
        if (energyBar != null)
        {
            // Isi energi sampai penuh (100)
            energyBar.RestoreEnergy(energyBar.maxEnergy);
            Debug.Log("Energi terisi penuh!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}