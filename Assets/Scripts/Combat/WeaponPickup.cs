using System.Collections;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private int weaponLevel = 1;
    [SerializeField] private string weaponName = "Hacha de mantenimiento";

    private bool taken = false;
    private SpriteRenderer sr;
    private Collider2D col;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

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
            if (weaponLevel == 4)
                GameHUDManager.Instance.ShowToast("¡Recargaste +20 balas de pulso!");
            else
                GameHUDManager.Instance.ShowToast("¡Conseguiste: " + weaponName + "! Ataca con J o clic");
        }

        // Si es un cargador de munición (nivel 4), reaparece a los 10 segundos para que nunca te quedes sin balas
        if (weaponLevel == 4)
        {
            StartCoroutine(RespawnAmmoRoutine(10f));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator RespawnAmmoRoutine(float delay)
    {
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(delay);
        taken = false;
        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;
    }
}
