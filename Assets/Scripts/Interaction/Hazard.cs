using UnityEngine;

public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        player.Respawn();
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("¡Chispas! Vuelves al último checkpoint");
        }
    }
}
