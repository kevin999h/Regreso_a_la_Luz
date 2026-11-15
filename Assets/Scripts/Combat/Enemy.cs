using UnityEngine;

public class BossShockwave : MonoBehaviour
{
    public float speed = 9.5f;
    public int direction = -1;
    public float lifetime = 2.4f;
    private float timer = 0f;

    private void Update()
    {
        if (NPCDialogue.IsDialogueActive) return;
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (GameHUDManager.Instance != null)
                {
                    GameHUDManager.Instance.ShowToast("¡Te impactó una descarga del Jefe! Pierdes 1 vida");
                }
                pc.Respawn();
            }
            Destroy(gameObject);
        }
    }
}

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float patrolRange = 3f;
    [SerializeField] private int requiredWeaponLevel = 1;
    [SerializeField] private bool isBoss = false;
    [SerializeField] private string displayName = "un virus";

    [Header("Modo Agresivo / Tryhard")]
    [SerializeField] private bool aggressiveChase = false;
    [SerializeField] private float aggroRange = 9.5f;
    [SerializeField] private float chaseSpeedMultiplier = 2.1f;

    private int currentHealth;
    private Vector3 startPos;
    private int dir = 1;
    private SpriteRenderer sr;
    private float flashTimer = 0f;
    private Color baseColor = Color.white;
    private bool dead = false;

    private Transform playerTarget;
    private float baseY;
    private float hopTimer = 0f;
    private float bossShotTimer = 1.8f;
    private float bossDashTimer = 2.5f;
    private float dashActiveTimer = 0f;
    private static Sprite cachedShockSprite;

    public bool IsBoss => isBoss;
    public int RequiredWeaponLevel => requiredWeaponLevel;
    public string DisplayName => displayName;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        startPos = transform.position;
        baseY = transform.position.y;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        // Si estamos en Nivel 3, todos los virus son ultra agresivos y rápidos por defecto
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Nivel3" && !isBoss)
        {
            aggressiveChase = true;
            if (moveSpeed < 3.0f) moveSpeed = 3.2f;
            aggroRange = 10.5f;
            chaseSpeedMultiplier = 1.65f;
            if (maxHealth < 3)
            {
                maxHealth = 3;
                currentHealth = 3;
            }
        }

        // Jefes Tryhard: Nivel 2 (14 golpes + embestida + disparos) y Nivel 3 (22 golpes + furia + ráfagas)
        if (isBoss)
        {
            aggressiveChase = true;
            aggroRange = 13f;
            if (sceneName == "Nivel2" && maxHealth < 14)
            {
                maxHealth = 14;
                currentHealth = 14;
                moveSpeed = 2.5f;
                chaseSpeedMultiplier = 1.6f;
            }
            else if (sceneName == "Nivel3" && maxHealth < 22)
            {
                maxHealth = 22;
                currentHealth = 22;
                moveSpeed = 3.1f;
                chaseSpeedMultiplier = 1.75f;
            }
        }
    }

    private void Update()
    {
        if (dead || NPCDialogue.IsDialogueActive) return;

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        bool chasing = false;
        float distToPlayer = 999f;
        if (playerTarget != null)
        {
            distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (aggressiveChase && distToPlayer <= aggroRange)
            {
                chasing = true;
            }
        }

        float currentSpeed = moveSpeed;
        bool enraged = isBoss && (currentHealth <= maxHealth * 0.5f);
        if (enraged)
        {
            currentSpeed *= 1.35f;
        }

        if (chasing && playerTarget != null)
        {
            float dx = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f)
            {
                dir = dx > 0f ? 1 : -1;
                if (sr != null) sr.flipX = (dir < 0);
            }

            float maxTether = isBoss ? (patrolRange + 4.5f) : (patrolRange + 6.5f);
            float nextX = transform.position.x + dir * currentSpeed * chaseSpeedMultiplier * (dashActiveTimer > 0f ? 1.85f : 1f) * Time.deltaTime;
            if (Mathf.Abs(nextX - startPos.x) <= maxTether)
            {
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }

            // Salto agresivo / embestida vertical para cazar al jugador
            hopTimer += Time.deltaTime * (isBoss ? 5.5f : 7.5f);
            float hopHeight = isBoss ? (enraged ? 0.85f : 0.55f) : 0.38f;
            float hopY = Mathf.Abs(Mathf.Sin(hopTimer)) * hopHeight;
            transform.position = new Vector3(transform.position.x, baseY + hopY, transform.position.z);

            // Ataques especiales Tryhard de los Jefes Finales
            if (isBoss)
            {
                if (dashActiveTimer > 0f)
                {
                    dashActiveTimer -= Time.deltaTime;
                }
                else
                {
                    bossDashTimer -= Time.deltaTime;
                    if (bossDashTimer <= 0f)
                    {
                        dashActiveTimer = 0.55f;
                        bossDashTimer = enraged ? 1.8f : 2.8f;
                        AudioManager.Play(Sfx.Attack);
                    }
                }

                bossShotTimer -= Time.deltaTime;
                float shotCooldown = (requiredWeaponLevel >= 3) ? (enraged ? 1.05f : 1.55f) : (enraged ? 1.45f : 2.1f);
                if (bossShotTimer <= 0f && distToPlayer <= 12.5f)
                {
                    bossShotTimer = shotCooldown;
                    FireBossShockwave(dir, enraged);
                }
            }
        }
        else if (patrolRange > 0.1f)
        {
            transform.position = new Vector3(transform.position.x + dir * currentSpeed * Time.deltaTime, baseY, transform.position.z);
            float offset = transform.position.x - startPos.x;
            if (offset > patrolRange)
            {
                dir = -1;
                if (sr != null) sr.flipX = true;
            }
            else if (offset < -patrolRange)
            {
                dir = 1;
                if (sr != null) sr.flipX = false;
            }
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && sr != null)
            {
                sr.color = enraged ? new Color(1f, 0.55f, 0.55f, 1f) : baseColor;
            }
        }
    }

    private void FireBossShockwave(int direction, bool enraged)
    {
        GameObject wave = new GameObject("DescargaJefe");
        wave.transform.position = transform.position + new Vector3(direction * 1.1f, 0.7f, 0f);
        wave.transform.localScale = new Vector3(1.5f, 0.9f, 1f);

        SpriteRenderer wsr = wave.AddComponent<SpriteRenderer>();
        wsr.sprite = GetShockSprite();
        wsr.color = enraged ? new Color(1f, 0.2f, 0.35f, 1f) : new Color(1f, 0.55f, 0.15f, 1f);
        wsr.sortingLayerName = "Characters";
        wsr.sortingOrder = 11;

        CircleCollider2D col = wave.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;

        Rigidbody2D rb = wave.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BossShockwave bs = wave.AddComponent<BossShockwave>();
        bs.direction = direction;
        bs.speed = (requiredWeaponLevel >= 3 ? 10.5f : 8.5f) * (enraged ? 1.25f : 1f);
    }

    private static Sprite GetShockSprite()
    {
        if (cachedShockSprite != null) return cachedShockSprite;
        Texture2D tex = new Texture2D(16, 10, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool core = (y >= 3 && y <= 6 && x >= 3 && x <= 12);
                tex.SetPixel(x, y, core ? new Color(1f, 0.95f, 0.5f, 1f) : new Color(1f, 0.25f, 0.15f, 0.9f));
            }
        }
        tex.Apply();
        cachedShockSprite = Sprite.Create(tex, new Rect(0, 0, 16, 10), new Vector2(0.5f, 0.5f), 16f);
        return cachedShockSprite;
    }

    public void HitByWeapon(int weaponLevel, int damage = 1)
    {
        if (dead) return;

        if (weaponLevel < requiredWeaponLevel)
        {
            AudioManager.Play(Sfx.Click);
            if (GameHUDManager.Instance != null)
            {
                string needed = requiredWeaponLevel >= 3 ? "la Pistola de Pulsos" : "el Hacha de plasma";
                GameHUDManager.Instance.ShowToast("¡" + displayName + " resiste! Necesitas " + needed);
            }
            return;
        }

        currentHealth -= damage;
        AudioManager.Play(Sfx.Hit);

        if (sr != null)
        {
            sr.color = new Color(1f, 0.25f, 0.25f, 1f);
            flashTimer = 0.14f;
        }

        if (currentHealth <= 0)
        {
            Eliminate();
        }
        else if (isBoss && GameHUDManager.Instance != null)
        {
            string rageTag = (currentHealth <= maxHealth * 0.5f) ? " ¡MODO FURIA!" : "";
            GameHUDManager.Instance.ShowToast(displayName + " — Vida: " + currentHealth + " / " + maxHealth + rageTag);
        }
    }

    private void Eliminate()
    {
        if (dead) return;
        dead = true;

        if (isBoss)
        {
            AudioManager.Play(Sfx.Complete);
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.RegisterBossDefeated(displayName);
            }
        }
        else
        {
            AudioManager.Play(Sfx.Collect);
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.RegisterEnemyKill();
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (dead || !collision.CompareTag("Player")) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        Rigidbody2D prb = collision.GetComponent<Rigidbody2D>();
        if (player == null) return;

        // Pisotón (Stomp): solo contra virus normales en Nivel 2; en Nivel 3 los virus tienen espinas de sobrecarga salvo que caigas muy rápido desde arriba
        bool fallingAbove = prb != null && prb.velocity.y <= -1.0f && (collision.transform.position.y > transform.position.y + 0.45f);
        if (!isBoss && fallingAbove)
        {
            player.Bounce(11.5f);
            HitByWeapon(requiredWeaponLevel, 2);
            return;
        }

        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("¡Te alcanzó " + displayName + "! Vuelves al checkpoint");
        }
        player.Respawn();
    }
}
