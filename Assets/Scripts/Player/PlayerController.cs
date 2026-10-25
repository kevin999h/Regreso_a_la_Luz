using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool isTopDown = false;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Agacharse y deslizarse")]
    [SerializeField] private float slideSpeedMultiplier = 1.7f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private Vector2 standingSize = new Vector2(0.45f, 0.8f);
    [SerializeField] private Vector2 standingOffset = new Vector2(0f, 0.45f);
    [SerializeField] private Vector2 crouchSize = new Vector2(0.45f, 0.4f);
    [SerializeField] private Vector2 crouchOffset = new Vector2(0f, 0.25f);

    [Header("Reaparición")]
    [SerializeField] private float fallLimitY = -12f;

    private Rigidbody2D rb;
    private Animator animator;
    private CapsuleCollider2D capsule;
    private Vector2 moveInput;
    private bool facingRight = true;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSliding;
    private float slideTimer;
    private float slideDirection;
    private float boostTimer;
    private float boostMultiplier = 1f;
    private Vector3 respawnPoint;

    public float BoostRemaining
    {
        get { return boostTimer; }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();
        respawnPoint = transform.position;
    }

    private void Update()
    {
        bool locked = NPCDialogue.IsDialogueActive;
        float horizontal = locked ? 0f : Input.GetAxisRaw("Horizontal");
        float vertical = (isTopDown && !locked) ? Input.GetAxisRaw("Vertical") : 0f;
        moveInput = new Vector2(horizontal, vertical).normalized;

        if (horizontal > 0 && !facingRight) Flip();
        else if (horizontal < 0 && facingRight) Flip();

        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                boostTimer = 0f;
                boostMultiplier = 1f;
            }
        }

        if (!isTopDown)
        {
            HandlePlatformerActions(horizontal, locked);
        }

        UpdateAnimator(horizontal);

        if (transform.position.y < fallLimitY)
        {
            Respawn();
        }
    }

    private void HandlePlatformerActions(float horizontal, bool locked)
    {
        isGrounded = groundCheck != null && HasSolid(Physics2D.OverlapCircleAll(groundCheck.position, checkRadius, groundLayer));

        bool downHeld = !locked && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));
        bool downPressed = !locked && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow));

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                isSliding = false;
            }
        }
        else if (downPressed && isGrounded && !isCrouching && Mathf.Abs(horizontal) > 0.1f)
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideDirection = Mathf.Sign(horizontal);
        }

        bool wantsCrouch = isGrounded && (downHeld || isSliding);
        if (wantsCrouch)
        {
            if (!isCrouching)
            {
                SetCrouchCollider(true);
            }
            isCrouching = true;
        }
        else if (isCrouching && !HasCeilingAbove())
        {
            SetCrouchCollider(false);
            isCrouching = false;
        }

        if (!locked && Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    private void FixedUpdate()
    {
        if (isTopDown)
        {
            rb.velocity = moveInput * moveSpeed;
            return;
        }

        float velocityX;
        if (isSliding)
        {
            velocityX = slideDirection * moveSpeed * slideSpeedMultiplier;
        }
        else if (isCrouching)
        {
            velocityX = moveInput.x * moveSpeed * crouchSpeedMultiplier;
        }
        else
        {
            velocityX = moveInput.x * moveSpeed * boostMultiplier;
        }

        rb.velocity = new Vector2(velocityX, rb.velocity.y);
    }

    private void UpdateAnimator(float horizontal)
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(horizontal));
        animator.SetBool("IsGrounded", isTopDown || isGrounded);
        animator.SetBool("IsSliding", isSliding);
        animator.SetBool("IsCrouching", isCrouching && !isSliding);
        animator.SetBool("IsBoosted", boostTimer > 0f);
    }

    private void SetCrouchCollider(bool crouch)
    {
        if (capsule == null) return;
        capsule.size = crouch ? crouchSize : standingSize;
        capsule.offset = crouch ? crouchOffset : standingOffset;
    }

    private bool HasCeilingAbove()
    {
        float scaleX = Mathf.Abs(transform.localScale.x);
        float scaleY = Mathf.Abs(transform.localScale.y);
        float crouchTop = (crouchOffset.y + crouchSize.y * 0.5f) * scaleY;
        float standTop = (standingOffset.y + standingSize.y * 0.5f) * scaleY;
        float height = standTop - crouchTop - 0.05f;
        if (height <= 0f) return false;

        Vector2 center = new Vector2(transform.position.x, transform.position.y + crouchTop + 0.025f + height * 0.5f);
        Vector2 size = new Vector2(standingSize.x * scaleX * 0.9f, height);
        return HasSolid(Physics2D.OverlapBoxAll(center, size, 0f, groundLayer));
    }

    private bool HasSolid(Collider2D[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].isTrigger) continue;
            if (hits[i].attachedRigidbody == rb) continue;
            return true;
        }
        return false;
    }

    private void Respawn()
    {
        transform.position = respawnPoint;
        rb.velocity = Vector2.zero;
        isSliding = false;
        isCrouching = false;
        SetCrouchCollider(false);
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        boostMultiplier = multiplier;
        boostTimer = duration;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
