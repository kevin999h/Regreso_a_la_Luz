using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("Identificación")]
    [SerializeField] private string itemName = "Tuerca";
    [SerializeField] private int itemValue = 1;

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isCollected)
        {
            isCollected = true;

            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.AddCollectible(itemValue, itemName);
            }

            Destroy(gameObject);
        }
    }
}
