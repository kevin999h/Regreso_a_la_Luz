using UnityEngine;

public class PlayerAnimationMenu : MonoBehaviour
{
    private Animator animator;
    private bool abierto = true;
    private string actual = "Stop";
    private readonly string[] etiquetas = { "Stop", "Walk", "Run", "Attack", "Hurt", "Slide", "Crawl", "Death" };
    private readonly string[] estados = { "Player_Idle", "Player_Walk", "Player_Run", "Player_Attack", "Player_Hurt", "Player_Slide", "Player_Crawl", "Player_Death" };

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        for (int i = 0; i < estados.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Reproducir(i);
            }
        }
    }

    private void Reproducir(int indice)
    {
        actual = etiquetas[indice];
        animator.Play(estados[indice], 0, 0f);
    }

    private void OnGUI()
    {
        float escala = Screen.height / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
        string titulo = abierto ? "Animaciones [-]" : "Animaciones [+]";
        if (GUI.Button(new Rect(10, 10, 150, 30), titulo))
        {
            abierto = !abierto;
        }
        GUI.Label(new Rect(170, 16, 250, 24), "Actual: " + actual);
        if (!abierto)
        {
            return;
        }
        for (int i = 0; i < etiquetas.Length; i++)
        {
            string texto = (i + 1) + "  " + etiquetas[i];
            if (GUI.Button(new Rect(10, 44 + i * 32, 150, 28), texto))
            {
                Reproducir(i);
            }
        }
    }
}
