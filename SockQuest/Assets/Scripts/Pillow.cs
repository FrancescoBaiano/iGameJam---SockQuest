using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Pillow : MonoBehaviour
{
    [Header("Rimbalzo")]
    [SerializeField] private float bounceForce = 20f;

    private Collider2D pillowCollider;

    private void Awake()
    {
        pillowCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Reagisce solo al player, non a qualsiasi Rigidbody2D che ci sbatte contro
        Player player = collision.gameObject.GetComponent<Player>();
        if (player == null || player.GroundCheck == null) return;

        if (!IsLandingOnTop(player)) return;

        Rigidbody2D rb = collision.rigidbody;
        if (rb == null) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
    }

    private bool IsLandingOnTop(Player player)
    {
        // Misuro la distanza dal punto di contatto più vicino sulla
        // superficie del cuscino
        Vector2 groundCheckPos = player.GroundCheck.position;
        Vector2 closestPoint = pillowCollider.ClosestPoint(groundCheckPos);
        float distance = Vector2.Distance(closestPoint, groundCheckPos);

        return distance <= player.GroundCheckRadius;
    }
}