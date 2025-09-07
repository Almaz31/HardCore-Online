using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private HeroMovement movement; 
    private Collider2D col;
    [SerializeField] private float riseSpeed = 3f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<HeroMovement>();
        col = GetComponent<Collider2D>();
    }

    public void Die()
    {
        if (!IsOwner) return;

        Debug.Log($"{gameObject.name} has died!");

        if (movement != null) movement.enabled = false;
        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0.5f;
            spriteRenderer.color = c;
        }

        StartCoroutine(RiseUp());
    }

    private IEnumerator RiseUp()
    {
        yield return new WaitForSeconds(1f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.up * riseSpeed;
        }
    }
}
