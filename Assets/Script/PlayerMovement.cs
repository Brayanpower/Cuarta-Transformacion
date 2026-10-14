using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.25f, 0.05f);
    public LayerMask groundLayer;

    [Header("Juice (Squash & Stretch)")]
    public bool enableSquashAndStretch = true;
    public float squashStretchSpeed = 10f;

    private float horizontalMovement;
    private bool isGrounded;
    private bool wasGrounded;

    // Coyote time & jump buffering
    private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null)
            Debug.LogError("No se encontró Rigidbody2D en " + gameObject.name);

        if (animator == null)
            Debug.LogError("No se encontró Animator en " + gameObject.name);

        originalScale = transform.localScale;
        if (Mathf.Approximately(originalScale.x, 0f))
            originalScale = Vector3.one;
    }

    void Update()
    {
        if (rb == null || animator == null)
            return;

        UpdateGroundedState();

        // Coyote Time
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
            if (coyoteTimeCounter > 0f)
            {
                ExecuteJump();
                jumpBufferCounter = 0f;
            }
        }

        // Landing feedback (Squash)
        if (!wasGrounded && isGrounded && enableSquashAndStretch)
        {
            float dir = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(dir * Mathf.Abs(originalScale.x) * 1.25f, originalScale.y * 0.75f, originalScale.z);
        }
        wasGrounded = isGrounded;

        // Smooth recovery to original scale
        if (enableSquashAndStretch)
        {
            float targetDir = Mathf.Sign(transform.localScale.x);
            Vector3 targetScale = new Vector3(targetDir * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * squashStretchSpeed);
        }

        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
        animator.SetBool("IsGrounded", isGrounded);

        CheckFlipDirection();
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        rb.linearVelocity = new Vector2(
            horizontalMovement * moveSpeed,
            rb.linearVelocity.y
        );
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else if (context.canceled && rb != null && rb.linearVelocity.y > 0f)
        {
            // Salto variable: soltar el botón corta el salto
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void ExecuteJump()
    {
        if (rb == null)
            return;

        coyoteTimeCounter = 0f;
        animator.SetTrigger("Jump");

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        // Estiramiento al saltar
        if (enableSquashAndStretch)
        {
            float dir = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(dir * Mathf.Abs(originalScale.x) * 0.8f, originalScale.y * 1.3f, originalScale.z);
        }
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (context.performed && animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void TakeDamage()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    public void Revive()
    {
        if (animator != null)
        {
            animator.SetTrigger("Revive");
        }
    }

    void UpdateGroundedState()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(
            groundCheck.position,
            groundCheckSize
        );
    }

    void CheckFlipDirection()
    {
        if (Mathf.Abs(horizontalMovement) < 0.01f)
            return;

        bool movingRight = horizontalMovement > 0;
        bool facingRight = transform.localScale.x > 0;

        if (movingRight != facingRight)
        {
            Vector3 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;
        }
    }
}