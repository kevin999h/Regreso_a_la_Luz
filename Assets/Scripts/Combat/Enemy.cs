using UnityEngine;

public class BossShockwave : MonoBehaviour
{
    public float speed = 7.2f;
    public int direction = -1;
    public float lifetime = 2.4f;
    private float timer = 0f;

    private void Update()
    {
        if (NPCDialogue.IsDialogueActive || InteractableObject.IsEducationalPanelActive) return;
        if (GameHUDManager.Instance != null && GameHUDManager.Instance.IsGameOver) return;

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
                pc.TakeDamageOrRespawn(false);
            }
            Destroy(gameObject);
        }
    }
}

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private float moveSpeed = 1.75f;
    [SerializeField] private float patrolRange = 3f;
    [SerializeField] private int requiredWeaponLevel = 1;
    [SerializeField] private bool isBoss = false;
    [SerializeField] private string displayName = "un virus";

    [Header("Persecución (solo Jefes)")]
    [SerializeField] private bool aggressiveChase = false;
    [SerializeField] private float aggroRange = 10.5f;
    [SerializeField] private float chaseSpeedMultiplier = 1.25f;

    private int currentHealth;
    private Vector3 startPos;
    private int dir = 1;
    private SpriteRenderer sr;
    private float flashTimer = 0f;
    private float hitStunTimer = 0f;
    private Color baseColor = Color.white;
    private bool dead = false;
    private bool canShootFireballs = false;
    private bool canDash = false;

    private Transform playerTarget;
    private float baseY;
    private float hopTimer = 0f;
    private float bossShotTimer = 2.4f;
    private float bossDashTimer = 3.6f;
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
        if (playerObj != null) playerTarget = playerObj.transform;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // En Nivel 3 los virus tienen comportamiento equilibrado (tal cual quedó perfecto)
        if (sceneName == "Nivel3" && !isBoss)
        {
            aggressiveChase = false;
            moveSpeed = 1.9f;
            maxHealth = 2;
            currentHealth = 2;
        }

        if (isBoss)
        {
            aggressiveChase = true;
            if (sceneName == "Nivel2")
            {
                // Jefe Nivel 2 justo y manejable: 6 golpes, SIN bolas de fuego, SIN embestidas rápidas y con pausa al recibir golpe
                maxHealth = 6;
                currentHealth = 6;
                moveSpeed = 1.45f;
                aggroRange = 9.5f;
                chaseSpeedMultiplier = 1.05f;
                canShootFireballs = false;
                canDash = false;
            }
            else if (sceneName == "Nivel3")
            {
                // Jefe Nivel 3 (sin cambios, perfecto para la Pistola de Pulsos)
                maxHealth = 13;
                currentHealth = 13;
                moveSpeed = 2.25f;
                aggroRange = 11.5f;
                chaseSpeedMultiplier = 1.3f;
                canShootFireballs = true;
                canDash = true;
            }
        }
    }

    private void Update()
    {
        if (dead || NPCDialogue.IsDialogueActive || InteractableObject.IsEducationalPanelActive) return;
        if (GameHUDManager.Instance != null && GameHUDManager.Instance.IsGameOver) return;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            bool enragedFlash = isBoss && canShootFireballs && (currentHealth <= maxHealth * 0.45f);
            if (flashTimer <= 0f && sr != null)
            {
                sr.color = enragedFlash ? new Color(1f, 0.65f, 0.65f, 1f) : baseColor;
            }
        }

        // Breve pausa de aturdimiento al recibir golpe (especialmente útil contra el jefe de Nivel 2)
        if (hitStunTimer > 0f)
        {
            hitStunTimer -= Time.deltaTime;
            return;
        }

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
        bool enraged = isBoss && canShootFireballs && (currentHealth <= maxHealth * 0.45f);
        if (enraged) currentSpeed *= 1.18f;

        if (chasing && playerTarget != null)
        {
            float dx = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.25f)
            {
                dir = dx > 0f ? 1 : -1;
                if (sr != null) sr.flipX = (dir < 0);
            }

            float maxTether = patrolRange + 4.0f;
            float dashMult = (canDash && dashActiveTimer > 0f) ? 1.45f : 1f;
            float nextX = transform.position.x + dir * currentSpeed * chaseSpeedMultiplier * dashMult * Time.deltaTime;
            if (Mathf.Abs(nextX - startPos.x) <= maxTether)
            {
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }

            hopTimer += Time.deltaTime * (canDash ? 4.2f : 2.6f);
            float hopHeight = canDash ? (enraged ? 0.55f : 0.35f) : 0.18f;
            float hopY = Mathf.Abs(Mathf.Sin(hopTimer)) * hopHeight;
            transform.position = new Vector3(transform.position.x, baseY + hopY, transform.position.z);

            if (isBoss && canDash)
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
                        dashActiveTimer = 0.42f;
                        bossDashTimer = enraged ? 2.8f : 3.8f;
                        AudioManager.Play(Sfx.Attack);
                    }
                }
            }

            if (isBoss && canShootFireballs)
            {
                bossShotTimer -= Time.deltaTime;
                float shotCooldown = enraged ? 1.95f : 2.55f;
                if (bossShotTimer <= 0f && distToPlayer <= 11.5f)
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
    }

    private void FireBossShockwave(int direction, bool enraged)
    {
        GameObject wave = new GameObject("DescargaJefe");
        wave.transform.position = transform.position + new Vector3(direction * 1.1f, 0.65f, 0f);
        wave.transform.localScale = new Vector3(1.35f, 0.85f, 1f);

        SpriteRenderer wsr = wave.AddComponent<SpriteRenderer>();
        wsr.sprite = GetShockSprite();
        wsr.color = enraged ? new Color(1f, 0.25f, 0.35f, 1f) : new Color(1f, 0.6f, 0.18f, 1f);
        wsr.sortingLayerName = "Characters";
        wsr.sortingOrder = 11;

        CircleCollider2D col = wave.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.26f;

        Rigidbody2D rb = wave.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BossShockwave bs = wave.AddComponent<BossShockwave>();
        bs.direction = direction;
        bs.speed = 8.2f * (enraged ? 1.15f : 1f);
    }

    private static Sprite GetShockSprite()
    {
        if (cachedShockSprite != null) return cachedShockSprite;
        Texture2D tex = new Texture2D(16, 10, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, (y >= 3 && y <= 6 && x >= 3 && x <= 12) ? new Color(1f, 0.95f, 0.5f, 1f) : new Color(1f, 0.25f, 0.15f, 0.9f));
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
            flashTimer = 0.16f;
        }

        // En Nivel 2, el jefe retrocede un poco y queda aturdido 0.45s al recibir cada golpe
        if (isBoss && !canShootFireballs)
        {
            hitStunTimer = 0.45f;
            if (playerTarget != null)
            {
                float pushDir = (transform.position.x >= playerTarget.position.x) ? 1f : -1f;
                float pushedX = Mathf.Clamp(transform.position.x + pushDir * 0.65f, startPos.x - patrolRange - 2f, startPos.x + patrolRange + 2f);
                transform.position = new Vector3(pushedX, baseY, transform.position.z);
            }
        }

        if (currentHealth <= 0)
        {
            Eliminate();
        }
        else if (isBoss && GameHUDManager.Instance != null)
        {
            string rageTag = (canShootFireballs && currentHealth <= maxHealth * 0.45f) ? " ¡SOBRECARGA!" : "";
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
            if (GameHUDManager.Instance != null) GameHUDManager.Instance.RegisterBossDefeated(displayName);
        }
        else
        {
            AudioManager.Play(Sfx.Collect);
            if (GameHUDManager.Instance != null) GameHUDManager.Instance.RegisterEnemyKill();
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (dead || !collision.CompareTag("Player")) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        Rigidbody2D prb = collision.GetComponent<Rigidbody2D>();
        if (player == null) return;

        bool fallingAbove = prb != null && prb.velocity.y <= -0.8f && (collision.transform.position.y > transform.position.y + 0.35f);
        if (!isBoss && fallingAbove)
        {
            player.Bounce(11.5f);
            HitByWeapon(requiredWeaponLevel, 2);
            return;
        }

        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("¡Te alcanzó " + displayName + "! Pierdes 1 vida");
        }
        player.TakeDamageOrRespawn(false);
    }
}
