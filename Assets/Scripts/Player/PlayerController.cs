using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento y Salto")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool isTopDown = false;
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Deslizamiento y Agachado (Tecla S)")]
    [SerializeField] private float slideSpeedMultiplier = 1.7f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private Vector2 standingSize = new Vector2(0.45f, 0.8f);
    [SerializeField] private Vector2 standingOffset = new Vector2(0f, 0.45f);
    [SerializeField] private Vector2 crouchSize = new Vector2(0.45f, 0.4f);
    [SerializeField] private Vector2 crouchOffset = new Vector2(0f, 0.25f);
    [SerializeField] private float fallLimitY = -12f;

    [Header("Vidas")]
    [SerializeField] private int maxLives = 3;

    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private Animator animator;
    private SpriteRenderer sr;

    private float moveInput;
    private int facingDir = 1;
    private bool isGrounded = true;
    private bool isSliding = false;
    private bool isCrouching = false;
    private float slideTimer = 0f;

    private float boostMultiplier = 1f;
    private float boostTimer = 0f;

    private Vector3 respawnPosition;
    private int currentLives;
    private float invulnerableTimer = 0f;

    public int FacingDirection => facingDir;
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        respawnPosition = transform.position;
        currentLives = maxLives;
    }

    private void Start()
    {
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.UpdateLives(currentLives, maxLives);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
        }

        if (NPCDialogue.IsDialogueActive)
        {
            moveInput = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            UpdateAnimator();
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput > 0.01f)
        {
            facingDir = 1;
            if (sr != null) sr.flipX = false;
        }
        else if (moveInput < -0.01f)
        {
            facingDir = -1;
            if (sr != null) sr.flipX = true;
        }

        CheckGrounded();

        bool ceilingBlocked = HasCeilingAbove();

        // Salto con ESPACIO, W o Flecha Arriba (bloqueado si hay techo bajo encima para no atravesar la estructura)
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded && !ceilingBlocked)
        {
            isSliding = false;
            isCrouching = false;
            ApplyCollider(false);
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
            AudioManager.Play(Sfx.Jump);
        }

        bool holdDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool pressDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        if (pressDown && isGrounded && Mathf.Abs(moveInput) > 0.1f && !isSliding)
        {
            isSliding = true;
            slideTimer = slideDuration;
            ApplyCollider(true);
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f || !holdDown)
            {
                isSliding = false;
                if (holdDown || ceilingBlocked)
                {
                    isCrouching = true;
                    ApplyCollider(true);
                }
                else
                {
                    isCrouching = false;
                    ApplyCollider(false);
                }
            }
        }
        else
        {
            if ((holdDown && isGrounded) || ceilingBlocked)
            {
                isCrouching = true;
                ApplyCollider(true);
            }
            else if (isCrouching)
            {
                if (!ceilingBlocked)
                {
                    isCrouching = false;
                    ApplyCollider(false);
                }
            }
        }

        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.SetBoostRemaining(boostTimer);
            }
            if (boostTimer <= 0f)
            {
                boostMultiplier = 1f;
                if (GameHUDManager.Instance != null)
                {
                    GameHUDManager.Instance.SetBoostRemaining(0f);
                }
            }
        }

        if (transform.position.y < fallLimitY)
        {
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.ShowToast("¡Caíste al vacío! Regresas al último checkpoint");
            }
            Respawn();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (NPCDialogue.IsDialogueActive) return;

        float speed = moveSpeed * boostMultiplier;
        if (isSliding)
        {
            float dir = Mathf.Abs(moveInput) > 0.01f ? Mathf.Sign(moveInput) : facingDir;
            rb.velocity = new Vector2(dir * moveSpeed * slideSpeedMultiplier * boostMultiplier, rb.velocity.y);
        }
        else if (isCrouching)
        {
            rb.velocity = new Vector2(moveInput * speed * crouchSpeedMultiplier, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(moveInput * speed, rb.velocity.y);
        }
    }

    private void CheckGrounded()
    {
        Vector2 pos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, checkRadius, groundLayer);
        bool groundedNow = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].gameObject != gameObject && !hits[i].isTrigger)
            {
                groundedNow = true;
                break;
            }
        }
        isGrounded = groundedNow;
    }

    // Verifica toda la caja superior donde estaría la cabeza de Theo si se pone de pie (evita atravesar túneles/estructuras al soltar S)
    private bool HasCeilingAbove()
    {
        float scaleY = Mathf.Abs(transform.localScale.y);
        float scaleX = Mathf.Abs(transform.localScale.x);
        Vector2 boxCenter = (Vector2)transform.position + new Vector2(0f, 0.58f * scaleY);
        Vector2 boxSize = new Vector2(0.36f * scaleX, 0.42f * scaleY);
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].gameObject != gameObject && !hits[i].isTrigger)
            {
                return true;
            }
        }
        return false;
    }

    private void ApplyCollider(bool low)
    {
        if (col == null) return;
        col.size = low ? crouchSize : standingSize;
        col.offset = low ? crouchOffset : standingOffset;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsSliding", isSliding);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsBoosted", boostTimer > 0f);
    }

    public void Bounce(float bounceVelocity = 11.5f)
    {
        if (rb == null) return;
        rb.velocity = new Vector2(rb.velocity.x, bounceVelocity);
        isGrounded = false;
        AudioManager.Play(Sfx.Jump);
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        boostMultiplier = multiplier;
        boostTimer = duration;
        if (GameHUDManager.Instance != null)
        {
            GameHUDManager.Instance.SetBoostRemaining(boostTimer);
        }
    }

    public void SetRespawn(Vector3 newRespawn)
    {
        respawnPosition = newRespawn;
    }

    public void Respawn()
    {
        if (invulnerableTimer > 0f && transform.position.y >= fallLimitY) return;
        invulnerableTimer = 0.6f;

        AudioManager.Play(Sfx.Hurt);
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        if (GameHUDManager.Instance != null && GameHUDManager.Instance.IsCombatLevel)
        {
            currentLives--;
            GameHUDManager.Instance.UpdateLives(currentLives, maxLives);

            if (currentLives <= 0)
            {
                currentLives = maxLives;
                GameHUDManager.Instance.UpdateLives(currentLives, maxLives);
                GameHUDManager.Instance.ShowToast("¡Te quedaste sin vidas! Reiniciando en punto de control...");
            }
        }

        // Si tiene la pistola y reaparece con pocas balas, le recargamos mínimo 18 balas para que nunca quede bloqueado
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.RefillMinimumAmmoOnRespawn();
        }

        isSliding = false;
        isCrouching = false;
        ApplyCollider(false);
        rb.velocity = Vector2.zero;
        transform.position = respawnPosition;
    }
}
