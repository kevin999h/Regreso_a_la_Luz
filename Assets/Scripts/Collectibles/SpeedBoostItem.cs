using UnityEngine;

public class SpeedBoostItem : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 1.6f;
    [SerializeField] private float duration = 5f;

    private bool isUsed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || isUsed) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        isUsed = true;
        player.ApplySpeedBoost(speedMultiplier, duration);

        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("¡TURBO! Velocidad extra por " + duration + " s");
        }

        Destroy(gameObject);
    }
}
