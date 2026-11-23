using UnityEngine;

public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        if (GameHUDManager.Instance != null)
        {
            string msg = GameHUDManager.Instance.IsCombatLevel
                ? "¡Chispas eléctricas! Pierdes 1 vida"
                : "¡Chispas! Vuelves al último checkpoint";
            GameHUDManager.Instance.ShowToast(msg);
        }
        player.TakeDamageOrRespawn(false);
    }
}
