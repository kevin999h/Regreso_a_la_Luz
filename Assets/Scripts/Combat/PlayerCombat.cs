using UnityEngine;

public class PulseProjectile : MonoBehaviour
{
    public float speed = 17f;
    public int direction = 1;
    public int weaponLevel = 3;
    public int damage = 1;
    public float maxLifetime = 1.6f;

    private float timer = 0f;

    private void Update()
    {
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        timer += Time.deltaTime;
        if (timer >= maxLifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.HitByWeapon(weaponLevel, damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger) Destroy(gameObject);
    }
}

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.32f;

    private int currentWeaponLevel = 0;
    private bool unlockedPistol = false;
    private string currentWeaponName = "Sin arma";
    private int currentAmmo = 0;
    private int maxAmmo = 40;
    private float cooldownTimer = 0f;

    private PlayerController controller;
    private Animator animator;
    private SpriteRenderer heldWeaponRenderer;
    private float gunRecoilTimer = 0f;
    private float meleeHideTimer = 0f;

    private static Sprite cachedBulletSprite;
    private static Sprite cachedPlasmaWaveSprite;
    private static Sprite cachedPistolSprite;
    private static Sprite cachedAxe1Sprite;
    private static Sprite cachedAxe2Sprite;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        CreateHeldWeaponObject();
    }

    private void CreateHeldWeaponObject()
    {
        Transform existing = transform.Find("ArmaEquipada");
        if (existing != null)
        {
            heldWeaponRenderer = existing.GetComponent<SpriteRenderer>();
        }
        else
        {
            GameObject weaponGo = new GameObject("ArmaEquipada");
            weaponGo.transform.SetParent(transform, false);
            weaponGo.transform.localPosition = new Vector3(0.21f, 0.44f, 0f);
            weaponGo.transform.localScale = Vector3.one;
            heldWeaponRenderer = weaponGo.AddComponent<SpriteRenderer>();
            heldWeaponRenderer.sortingLayerName = "Characters";
            heldWeaponRenderer.sortingOrder = 8;
        }
        heldWeaponRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (meleeHideTimer > 0f) meleeHideTimer -= Time.deltaTime;

        UpdateHeldWeaponVisual();

        if (NPCDialogue.IsDialogueActive || InteractableObject.IsEducationalPanelActive) return;
        if (GameHUDManager.Instance != null && GameHUDManager.Instance.IsGameOver) return;

        if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && cooldownTimer <= 0f)
        {
            PerformAttack();
        }
    }

    private void UpdateHeldWeaponVisual()
    {
        if (heldWeaponRenderer == null) return;

        bool hasWeapon = currentWeaponLevel > 0;
        bool hideDuringSwing = (currentWeaponLevel < 3 && meleeHideTimer > 0f);
        bool show = hasWeapon && !hideDuringSwing;

        if (heldWeaponRenderer.gameObject.activeSelf != show)
        {
            heldWeaponRenderer.gameObject.SetActive(show);
        }
        if (!show) return;

        int dir = controller != null ? controller.FacingDirection : 1;
        bool low = controller != null && (controller.IsCrouching || controller.IsSliding);
        float recoilOffset = (currentWeaponLevel >= 3 && gunRecoilTimer > 0f) ? -0.05f * dir : 0f;
        if (gunRecoilTimer > 0f) gunRecoilTimer -= Time.deltaTime;

        if (currentWeaponLevel >= 3 && currentAmmo > 0)
        {
            heldWeaponRenderer.sprite = GetPistolHandSprite();
            heldWeaponRenderer.flipX = (dir < 0);
            heldWeaponRenderer.transform.localPosition = new Vector3(dir * 0.21f + recoilOffset, low ? 0.25f : 0.45f, 0f);
        }
        else if (currentWeaponLevel == 2)
        {
            heldWeaponRenderer.sprite = GetAxeSprite(true);
            heldWeaponRenderer.flipX = (dir < 0);
            heldWeaponRenderer.transform.localPosition = new Vector3(dir * 0.21f, low ? 0.26f : 0.46f, 0f);
        }
        else
        {
            heldWeaponRenderer.sprite = GetAxeSprite(false);
            heldWeaponRenderer.flipX = (dir < 0);
            heldWeaponRenderer.transform.localPosition = new Vector3(dir * 0.21f, low ? 0.26f : 0.46f, 0f);
        }
    }

    private void PerformAttack()
    {
        if (currentWeaponLevel <= 0)
        {
            if (GameHUDManager.Instance != null)
                GameHUDManager.Instance.ShowToast("Aún no tienes arma equipada. Búscala en el nivel.");
            return;
        }

        cooldownTimer = attackCooldown;

        if (currentWeaponLevel >= 3 && currentAmmo > 0)
        {
            currentAmmo--;
            gunRecoilTimer = 0.09f;
            AudioManager.Play(Sfx.Attack);
            SpawnPulseBullet();

            if (currentAmmo <= 0)
            {
                currentWeaponLevel = 2;
                currentWeaponName = "Hacha de respaldo (Sin balas)";
                if (GameHUDManager.Instance != null)
                    GameHUDManager.Instance.ShowToast("¡Sin balas! Usas el hacha hasta recoger otro cargador.");
            }

            UpdateWeaponHUD();
            return;
        }

        meleeHideTimer = 0.28f;
        if (animator != null) animator.SetTrigger("Attack");
        AudioManager.Play(Sfx.Attack);

        float range = currentWeaponLevel == 1 ? 2.6f : 3.8f;
        int dir = controller != null ? controller.FacingDirection : 1;
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * range * 0.45f, 0.6f);

        bool hitClose = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range * 0.65f);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = hits[i].GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.HitByWeapon(currentWeaponLevel, 1);
                hitClose = true;
            }
        }

        // El Hacha de plasma (Nivel 2) lanza además una Onda de Plasma eléctrica a distancia para vencer al jefe sin arriesgarse cuerpo a cuerpo
        if (currentWeaponLevel == 2 && !hitClose)
        {
            SpawnPlasmaAxeWave();
        }
    }

    private void SpawnPlasmaAxeWave()
    {
        int dir = controller != null ? controller.FacingDirection : 1;
        bool low = controller != null && (controller.IsCrouching || controller.IsSliding);
        float spawnY = low ? 0.42f : 0.72f;

        GameObject wave = new GameObject("OndaPlasmaHacha");
        wave.transform.position = transform.position + new Vector3(dir * 0.85f, spawnY, 0f);
        wave.transform.localScale = new Vector3(1.5f, 1.45f, 1f);

        SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlasmaWaveSprite();
        sr.flipX = (dir < 0);
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 12;

        CircleCollider2D col = wave.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        Rigidbody2D brb = wave.AddComponent<Rigidbody2D>();
        brb.bodyType = RigidbodyType2D.Kinematic;

        PulseProjectile proj = wave.AddComponent<PulseProjectile>();
        proj.direction = dir;
        proj.speed = 13.5f;
        proj.weaponLevel = 2;
        proj.damage = 1;
        proj.maxLifetime = 0.68f;
    }

    private void SpawnPulseBullet()
    {
        int dir = controller != null ? controller.FacingDirection : 1;
        bool low = controller != null && (controller.IsCrouching || controller.IsSliding);
        float spawnY = low ? 0.38f : 0.68f;

        GameObject bullet = new GameObject("PulsoElectrico");
        bullet.transform.position = transform.position + new Vector3(dir * 0.68f, spawnY, 0f);
        bullet.transform.localScale = new Vector3(1.4f, 0.7f, 1f);

        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = GetBulletSprite();
        sr.color = new Color(0.25f, 1f, 0.98f, 1f);
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 12;

        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        Rigidbody2D brb = bullet.AddComponent<Rigidbody2D>();
        brb.bodyType = RigidbodyType2D.Kinematic;

        PulseProjectile proj = bullet.AddComponent<PulseProjectile>();
        proj.direction = dir;
        proj.speed = 17f;
        proj.weaponLevel = 3;
        proj.damage = 1;
    }

    private static Sprite GetPlasmaWaveSprite()
    {
        if (cachedPlasmaWaveSprite != null) return cachedPlasmaWaveSprite;
        Texture2D tex = new Texture2D(16, 20, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, Color.clear);

        Color outer = new Color(0.15f, 0.92f, 1f, 0.92f);
        Color core = new Color(1f, 0.96f, 0.55f, 1f);
        for (int y = 2; y <= 17; y++)
        {
            float dy = (y - 9.5f) / 8.0f;
            int arcX = Mathf.RoundToInt(11f - dy * dy * 7f);
            for (int dx = -2; dx <= 2; dx++)
            {
                int px = arcX + dx;
                if (px >= 0 && px < 16)
                    tex.SetPixel(px, y, Mathf.Abs(dx) <= 1 ? core : outer);
            }
        }
        tex.Apply();
        cachedPlasmaWaveSprite = Sprite.Create(tex, new Rect(0, 0, 16, 20), new Vector2(0.5f, 0.5f), 20f);
        return cachedPlasmaWaveSprite;
    }

    private static Sprite GetPistolHandSprite()
    {
        if (cachedPistolSprite != null) return cachedPistolSprite;
        Texture2D tex = new Texture2D(18, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 18; x++)
                tex.SetPixel(x, y, Color.clear);

        Color darkMetal = new Color(0.12f, 0.16f, 0.24f, 1f);
        Color midMetal = new Color(0.42f, 0.52f, 0.66f, 1f);
        Color grip = new Color(0.26f, 0.18f, 0.14f, 1f);
        Color cyan = new Color(0.15f, 0.98f, 1f, 1f);
        Color gold = new Color(1f, 0.88f, 0.25f, 1f);

        for (int y = 3; y <= 7; y++)
            for (int x = 3; x <= 5; x++)
                tex.SetPixel(x, y, grip);

        for (int y = 7; y <= 11; y++)
            for (int x = 3; x <= 14; x++)
                tex.SetPixel(x, y, (y == 7 || y == 11 || x == 3 || x == 14) ? darkMetal : midMetal);

        for (int y = 8; y <= 10; y++)
            for (int x = 6; x <= 12; x++)
                tex.SetPixel(x, y, cyan);

        for (int y = 8; y <= 10; y++)
            for (int x = 15; x <= 16; x++)
                tex.SetPixel(x, y, gold);

        tex.Apply();
        cachedPistolSprite = Sprite.Create(tex, new Rect(0, 0, 18, 16), new Vector2(0.22f, 0.45f), 32f);
        return cachedPistolSprite;
    }

    private static Sprite GetAxeSprite(bool plasma)
    {
        if (plasma && cachedAxe2Sprite != null) return cachedAxe2Sprite;
        if (!plasma && cachedAxe1Sprite != null) return cachedAxe1Sprite;

        Texture2D tex = new Texture2D(12, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 12; x++)
                tex.SetPixel(x, y, Color.clear);

        Color handle = new Color(0.45f, 0.28f, 0.14f, 1f);
        Color blade = plasma ? new Color(0.2f, 0.95f, 1f, 1f) : new Color(0.75f, 0.8f, 0.85f, 1f);
        Color edge = plasma ? new Color(1f, 0.92f, 0.35f, 1f) : new Color(0.45f, 0.5f, 0.58f, 1f);

        for (int y = 1; y <= 13; y++) tex.SetPixel(4, y, handle);
        for (int y = 9; y <= 14; y++)
            for (int x = 5; x <= 8; x++)
                tex.SetPixel(x, y, (x == 8) ? edge : blade);

        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, 12, 16), new Vector2(0.35f, 0.45f), 32f);
        if (plasma) cachedAxe2Sprite = s; else cachedAxe1Sprite = s;
        return s;
    }

    private static Sprite GetBulletSprite()
    {
        if (cachedBulletSprite != null) return cachedBulletSprite;
        Texture2D tex = new Texture2D(16, 8, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, (y >= 2 && y <= 5 && x >= 2 && x <= 14) ? Color.white : new Color(0.2f, 0.95f, 1f, 0.85f));
        tex.Apply();
        cachedBulletSprite = Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0.5f), 16f);
        return cachedBulletSprite;
    }

    public void EquipWeapon(int weaponLevel, string weaponName)
    {
        if (weaponLevel == 4)
        {
            unlockedPistol = true;
            currentWeaponLevel = 3;
            currentWeaponName = "Pistola de Pulsos";
            currentAmmo = Mathf.Min(maxAmmo, currentAmmo + 20);
            UpdateWeaponHUD();
            return;
        }

        if (weaponLevel >= currentWeaponLevel)
        {
            currentWeaponLevel = weaponLevel;
            currentWeaponName = weaponName;
        }

        if (weaponLevel >= 3)
        {
            unlockedPistol = true;
            maxAmmo = 40;
            currentAmmo = maxAmmo;
        }

        UpdateWeaponHUD();
    }

    public void RefillMinimumAmmoOnRespawn()
    {
        if (unlockedPistol && currentAmmo < 18)
        {
            currentWeaponLevel = 3;
            currentWeaponName = "Pistola de Pulsos";
            currentAmmo = 20;
            UpdateWeaponHUD();
        }
    }

    private void UpdateWeaponHUD()
    {
        if (GameHUDManager.Instance == null) return;
        if (currentWeaponLevel >= 3)
            GameHUDManager.Instance.SetWeaponText("Pistola de Pulsos  |  Balas: " + currentAmmo + " / " + maxAmmo);
        else if (currentWeaponLevel == 2)
            GameHUDManager.Instance.SetWeaponText("Hacha de plasma (Lanza ondas eléctricas con J / clic)");
        else
            GameHUDManager.Instance.SetWeaponText(currentWeaponName);
    }
}
