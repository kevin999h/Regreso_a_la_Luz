using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float patrolRange = 3f;
    [SerializeField] private int requiredWeaponLevel = 1;
    [SerializeField] private bool isBoss = false;
    [SerializeField] private string displayName = "Virus";

    private int health;
    private float startX;
    private int direction = 1;
    private float baseScaleX;
    private float flashTimer;
    private bool dead;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        health = maxHealth;
        startX = transform.position.x;
        baseScaleX = Mathf.Abs(transform.localScale.x);
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        pos.x += direction * moveSpeed * Time.deltaTime;
        if (pos.x > startX + patrolRange)
        {
            pos.x = startX + patrolRange;
            direction = -1;
        }
        else if (pos.x < startX - patrolRange)
        {
            pos.x = startX - patrolRange;
            direction = 1;
        }
        transform.position = pos;

        Vector3 scale = transform.localScale;
        scale.x = baseScaleX * (direction > 0 ? 1f : -1f);
        transform.localScale = scale;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }
    }

    public void TakeDamage(int damage, int weaponLevel)
    {
        if (weaponLevel < requiredWeaponLevel)
        {
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.ShowToast("Tu arma no le hace daño. Busca una mejor.");
            }
            return;
        }

        health -= damage;
        AudioManager.Play(Sfx.Hit);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f, 1f);
            flashTimer = 0.12f;
        }

        if (health <= 0)
        {
            Die();
        }
        else if (isBoss && GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast(displayName + ": " + health + " / " + maxHealth);
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        if (GameHUDManager.Instance != null)
        {
            if (isBoss) GameHUDManager.Instance.OnBossDefeated();
            else GameHUDManager.Instance.AddKill();
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleContact(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleContact(collision);
    }

    private void HandleContact(Collider2D collision)
    {
        if (dead || !collision.CompareTag("Player")) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        Rigidbody2D body = collision.attachedRigidbody;
        Collider2D mine = GetComponent<Collider2D>();
        bool falling = body != null && body.velocity.y < -0.5f;
        bool above = mine != null && collision.bounds.min.y > mine.bounds.center.y;

        if (!isBoss && falling && above)
        {
            player.Bounce(11f);
            AudioManager.Play(Sfx.Hit);
            Die();
            return;
        }

        player.Respawn();
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("¡Te atrapó " + displayName + "! Vuelves al último checkpoint");
        }
    }
}
