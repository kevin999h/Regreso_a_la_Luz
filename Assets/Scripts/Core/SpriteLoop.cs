using UnityEngine;

public class SpriteLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 6f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int index;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer = 0f;
            index = (index + 1) % frames.Length;
            spriteRenderer.sprite = frames[index];
        }
    }
}
