using UnityEngine;

public class PulseProjectile : MonoBehaviour
{
    public float speed = 17f;
    public int direction = 1;
    public int weaponLevel = 3;
    public float maxLifetime = 1.6f;

    private float timer = 0f;

    private void Update()
    {
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        timer += Time.deltaTime;
        if (timer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.HitByWeapon(weaponLevel, 1);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.34f;

    private int currentWeaponLevel = 0;
    private bool unlockedPistol = false;
    private string currentWeaponName = "Sin arma";
    private int currentAmmo = 0;
    private int maxAmmo = 40;
    private float cooldownTimer = 0f;

    private PlayerController controller;
    private Animator animator;
    private SpriteRenderer heldGunRenderer;
    private float gunRecoilTimer = 0f;

    private static Sprite cachedBulletSprite;
    private static Sprite cachedPistolSprite;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        CreateHeldGunObject();
    }

    private void CreateHeldGunObject()
    {
        Transform existing = transform.Find("PistolaEquipada");
        if (existing != null)
        {
            heldGunRenderer = existing.GetComponent<SpriteRenderer>();
        }
        else
        {
            GameObject gunGo = new GameObject("PistolaEquipada");
            gunGo.transform.SetParent(transform, false);
            // Exactamente en la coordenada del puño de Theo: X = 0.21f, Y = 0.43f, escala 1:1 con los píxeles del personaje
            gunGo.transform.localPosition = new Vector3(0.21f, 0.43f, 0f);
            gunGo.transform.localScale = Vector3.one;
            heldGunRenderer = gunGo.AddComponent<SpriteRenderer>();
            heldGunRenderer.sprite = GetPistolHandSprite();
            heldGunRenderer.sortingLayerName = "Characters";
            heldGunRenderer.sortingOrder = 9;
        }
        heldGunRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Mantener la pistola pegada a la mano de Theo (incluso al voltear, agacharse o deslizarse)
        if (heldGunRenderer != null)
        {
            bool showGun = (currentWeaponLevel >= 3 && currentAmmo > 0);
            if (heldGunRenderer.gameObject.activeSelf != showGun)
            {
                heldGunRenderer.gameObject.SetActive(showGun);
            }

            if (showGun)
            {
                int dir = controller != null ? controller.FacingDirection : 1;
                bool low = controller != null && (controller.IsCrouching || controller.IsSliding);
                float recoilOffset = gunRecoilTimer > 0f ? -0.04f * dir : 0f;
                if (gunRecoilTimer > 0f) gunRecoilTimer -= Time.deltaTime;

                heldGunRenderer.flipX = (dir < 0);
                float handX = dir * 0.205f + recoilOffset;
                float handY = low ? 0.24f : 0.43f;
                heldGunRenderer.transform.localPosition = new Vector3(handX, handY, 0f);
            }
        }

        if (NPCDialogue.IsDialogueActive) return;

        if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && cooldownTimer <= 0f)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        if (currentWeaponLevel <= 0)
        {
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.ShowToast("Aún no tienes arma equipada. Búscala en el nivel.");
            }
            return;
        }

        cooldownTimer = attackCooldown;

        // Nivel 3: Pistola de Pulsos Eléctricos
        if (currentWeaponLevel >= 3 && currentAmmo > 0)
        {
            currentAmmo--;
            gunRecoilTimer = 0.09f;
            AudioManager.Play(Sfx.Attack);
            SpawnPulseBullet();

            // Si se gastó la última bala, guarda la pistola y pasa al Hacha de respaldo hasta recoger otro cargador
            if (currentAmmo <= 0)
            {
                currentWeaponLevel = 2;
                currentWeaponName = "Hacha de respaldo (Sin balas)";
                if (heldGunRenderer != null) heldGunRenderer.gameObject.SetActive(false);
                if (GameHUDManager.Instance != null)
                {
                    GameHUDManager.Instance.ShowToast("¡Sin balas! Usas el hacha hasta recoger otro cargador de pulsos.");
                }
            }

            UpdateWeaponHUD();
            return;
        }

        // Ataque cuerpo a cuerpo con Hacha (Nivel 1 o 2)
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        AudioManager.Play(Sfx.Attack);
        float range = currentWeaponLevel == 1 ? 2.5f : 3.3f;
        int dir = controller != null ? controller.FacingDirection : 1;
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * range * 0.45f, 0.6f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range * 0.65f);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = hits[i].GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.HitByWeapon(currentWeaponLevel, 1);
            }
        }
    }

    private void SpawnPulseBullet()
    {
        int dir = controller != null ? controller.FacingDirection : 1;
        bool low = controller != null && (controller.IsCrouching || controller.IsSliding);
        float spawnY = low ? 0.40f : 0.66f;

        GameObject bullet = new GameObject("PulsoElectrico");
        bullet.transform.position = transform.position + new Vector3(dir * 0.65f, spawnY, 0f);
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
    }

    // Sprite compacto de 16x16 píxeles (32 PPU) que se funde con el puño de Theo y tapa exactamente el hacha del sprite original
    private static Sprite GetPistolHandSprite()
    {
        if (cachedPistolSprite != null) return cachedPistolSprite;
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        Color skin = new Color(240f / 255f, 195f / 255f, 150f / 255f, 1f);
        Color darkMetal = new Color(0.14f, 0.18f, 0.26f, 1f);
        Color midMetal = new Color(0.36f, 0.44f, 0.56f, 1f);
        Color gripBrown = new Color(0.32f, 0.20f, 0.14f, 1f);
        Color cyanCore = new Color(0.15f, 0.98f, 1f, 1f);
        Color goldTip = new Color(1f, 0.88f, 0.25f, 1f);

        // Puño / dedos de Theo sujetando la pistola (x: 4..5, y: 4..7)
        for (int y = 4; y <= 7; y++)
            for (int x = 4; x <= 5; x++)
                tex.SetPixel(x, y, skin);

        // Empuñadura pegada a la mano (x: 5..7, y: 2..6)
        for (int y = 2; y <= 6; y++)
            for (int x = 5; x <= 7; x++)
                tex.SetPixel(x, y, gripBrown);

        // Cuerpo superior de la pistola que cubre el filo del hacha (x: 5..12, y: 7..11)
        for (int y = 7; y <= 11; y++)
        {
            for (int x = 5; x <= 12; x++)
            {
                bool border = (y == 7 || y == 11 || x == 5);
                tex.SetPixel(x, y, border ? darkMetal : midMetal);
            }
        }

        // Mira / tapa superior para cubrir los píxeles altos del hacha (x: 6..9, y: 12)
        for (int x = 6; x <= 9; x++)
            tex.SetPixel(x, 12, darkMetal);

        // Bobina de pulso cian brillante (x: 7..11, y: 9..10)
        for (int y = 9; y <= 10; y++)
            for (int x = 7; x <= 11; x++)
                tex.SetPixel(x, y, cyanCore);

        // Punta del cañón dorada (x: 13..14, y: 8..10)
        for (int y = 8; y <= 10; y++)
            for (int x = 13; x <= 14; x++)
                tex.SetPixel(x, y, goldTip);

        tex.Apply();
        // Pivote en (0.35, 0.5) para que quede exactamente sobre la mano y el hacha de Theo
        cachedPistolSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.35f, 0.5f), 32f);
        return cachedPistolSprite;
    }

    private static Sprite GetBulletSprite()
    {
        if (cachedBulletSprite != null) return cachedBulletSprite;
        Texture2D tex = new Texture2D(16, 8, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool core = (y >= 2 && y <= 5 && x >= 2 && x <= 14);
                tex.SetPixel(x, y, core ? Color.white : new Color(0.2f, 0.95f, 1f, 0.85f));
            }
        }
        tex.Apply();
        cachedBulletSprite = Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0.5f), 16f);
        return cachedBulletSprite;
    }

    public void EquipWeapon(int weaponLevel, string weaponName)
    {
        // Cargador de munición (weaponLevel == 4)
        if (weaponLevel == 4)
        {
            unlockedPistol = true;
            currentWeaponLevel = 3;
            currentWeaponName = "Pistola de Pulsos";
            currentAmmo = Mathf.Min(maxAmmo, currentAmmo + 20);
            if (heldGunRenderer != null) heldGunRenderer.gameObject.SetActive(true);
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
            if (heldGunRenderer != null) heldGunRenderer.gameObject.SetActive(true);
        }
        else
        {
            if (heldGunRenderer != null) heldGunRenderer.gameObject.SetActive(false);
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
            if (heldGunRenderer != null) heldGunRenderer.gameObject.SetActive(true);
            UpdateWeaponHUD();
        }
    }

    private void UpdateWeaponHUD()
    {
        if (GameHUDManager.Instance == null) return;

        if (currentWeaponLevel >= 3)
        {
            GameHUDManager.Instance.SetWeaponText("Pistola de Pulsos  |  Balas: " + currentAmmo + " / " + maxAmmo);
        }
        else
        {
            GameHUDManager.Instance.SetWeaponText(currentWeaponName);
        }
    }
}
