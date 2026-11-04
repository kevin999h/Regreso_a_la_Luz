using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.45f;

    private PlayerController controller;
    private Animator animator;
    private int weaponLevel = 0;
    private float cooldownTimer = 0f;
    private float hitTimer = -1f;

    public int WeaponLevel
    {
        get { return weaponLevel; }
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    public void EquipWeapon(int level, string weaponName)
    {
        if (level <= weaponLevel) return;

        weaponLevel = level;
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.SetWeapon(weaponName);
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (hitTimer >= 0f)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer < 0f)
            {
                DoHit();
                hitTimer = -1f;
            }
        }

        bool pressed = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        if (!pressed || NPCDialogue.IsDialogueActive) return;

        if (weaponLevel == 0)
        {
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.ShowToast("No tienes arma. Búscala en el nivel.");
            }
            return;
        }

        if (cooldownTimer > 0f) return;

        cooldownTimer = attackCooldown;
        hitTimer = 0.1f;
        AudioManager.Play(Sfx.Attack);
        if (animator != null) animator.SetTrigger("Attack");
    }

    private void DoHit()
    {
        float reach = weaponLevel >= 2 ? 3f : 2.2f;
        int damage = weaponLevel >= 2 ? 3 : 1;
        float dir = controller != null && !controller.FacingRight ? -1f : 1f;

        Vector2 center = new Vector2(transform.position.x + dir * reach * 0.5f, transform.position.y + 0.8f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(reach, 1.4f), 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = hits[i].GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, weaponLevel);
            }
        }
    }
}
