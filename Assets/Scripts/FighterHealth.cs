using UnityEngine;
using UnityEngine.Events;

public class FighterHealth : MonoBehaviour
{
    public float maxHealth      = 250f;
    public float knockbackForce = 6f;

    public float     HealthPercent  => currentHealth / maxHealth;
    public bool      IsDead         { get; private set; }
    public bool      isInvincible;

    public UnityEvent<float> onHealthChanged;  // 0-1 percent
    public UnityEvent        onDeath;

    float        currentHealth;
    Rigidbody2D  rb;

    void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector2 sourcePos)
    {
        if (IsDead || isInvincible) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onHealthChanged?.Invoke(HealthPercent);

        Vector2 dir = ((Vector2)transform.position - sourcePos).normalized;
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(HitFlash());

        if (currentHealth <= 0f)
        {
            IsDead = true;
            onDeath?.Invoke();
            GameManager.Instance?.OnFighterDied(this);
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        var renderers  = GetComponentsInChildren<SpriteRenderer>();
        var origColors = System.Array.ConvertAll(renderers, r => r.color);
        foreach (var r in renderers) r.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = origColors[i];
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead        = false;
        onHealthChanged?.Invoke(1f);
    }
}
