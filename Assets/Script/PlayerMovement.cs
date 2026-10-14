using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;

    public float jumpForce = 3f;
    public float moveSpeed = 3f;

    private float horizontalMovement;
    private bool isGrounded;

    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.25f, 0.25f);
    public LayerMask groundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null)
            Debug.LogError("No se encontró Rigidbody2D.");

        if (animator == null)
            Debug.LogError("No se encontró Animator.");
    }

    void Update()
    {
        if (rb == null || animator == null)
            return;

        UpdateGroundedState();

        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
        animator.SetBool("IsGrounded", isGrounded);

        CheckFlipDirection();

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
        if (context.performed && isGrounded && rb != null)
        {
            animator.SetTrigger("Jump");

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            groundCheck.position,
            groundCheckSize
        );
    }

    void CheckFlipDirection()
    {
        if (horizontalMovement == 0)
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