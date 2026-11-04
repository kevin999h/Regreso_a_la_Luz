using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelFade : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;

    private CanvasGroup group;
    private float timer;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        timer = 0f;
        group.alpha = 0f;
    }

    private void Update()
    {
        if (group.alpha >= 1f) return;

        timer += Time.unscaledDeltaTime;
        group.alpha = Mathf.Clamp01(timer / duration);
    }
}
