using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Sprite activeSprite;

    private bool activated = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (activated || !collision.CompareTag("Player")) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        activated = true;
        player.SetRespawn(transform.position + new Vector3(0f, 0.3f, 0f));

        if (spriteRenderer != null && activeSprite != null)
        {
            spriteRenderer.sprite = activeSprite;
        }

        AudioManager.Play(Sfx.Powerup);
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("Checkpoint guardado");
        }
    }
}
