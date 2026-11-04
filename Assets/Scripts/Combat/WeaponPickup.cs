using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private int weaponLevel = 1;
    [SerializeField] private string weaponName = "Hacha de mantenimiento";

    private bool taken = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (taken || !collision.CompareTag("Player")) return;

        PlayerCombat combat = collision.GetComponent<PlayerCombat>();
        if (combat == null) return;

        taken = true;
        combat.EquipWeapon(weaponLevel, weaponName);
        AudioManager.Play(Sfx.Powerup);
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.ShowToast("¡Conseguiste: " + weaponName + "! Ataca con J o clic");
        }
        Destroy(gameObject);
    }
}
