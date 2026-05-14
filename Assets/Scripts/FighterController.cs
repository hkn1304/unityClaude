using UnityEngine;
using UnityEngine.InputSystem;

public class FighterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Input (Player only)")]
    public KeyCode leftKey  = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey  = KeyCode.W;
    public KeyCode punchKey = KeyCode.J;
    public KeyCode kickKey  = KeyCode.K;

    [Header("Combat")]
    public float punchDamage    = 10f;
    public float punchRange     = 1.8f;
    public float kickDamage     = 18f;
    public float kickRange      = 2.2f;
    public float attackCooldown = 0.4f;

    [HideInInspector] public Transform opponent;
    [HideInInspector] public bool isPlayerControlled = true;
    [HideInInspector] public bool inputFrozen        = false;
    [HideInInspector] public float aiMoveInput;
    [HideInInspector] public Weapon equippedWeapon;

    Rigidbody2D   rb;
    FighterHealth health;
    float         lastAttackTime;
    bool          grounded;

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        health = GetComponent<FighterHealth>();
    }

    void Update()
    {
        if (health == null || health.IsDead) return;
        FaceOpponent();
        if (isPlayerControlled && !inputFrozen) ReadPlayerInput();
    }

    void FixedUpdate()
    {
        if (health == null || health.IsDead) return;
        float h = inputFrozen ? 0f : (isPlayerControlled ? GetHorizontalInput() : aiMoveInput);
        rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
    }

    float GetHorizontalInput()
    {
        float h = 0f;
        if (KeyHeld(leftKey))  h -= 1f;
        if (KeyHeld(rightKey)) h += 1f;
        return h;
    }

    void ReadPlayerInput()
    {
        if (KeyDown(jumpKey) && grounded)
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (KeyDown(punchKey))
        {
            if (equippedWeapon != null) equippedWeapon.TryAttack();
            else { TryAttack(punchDamage, punchRange); StartCoroutine(AttackAnim("ArmR", 0.1f)); }
        }
        if (KeyDown(kickKey))
        {
            if (equippedWeapon == null) { TryAttack(kickDamage, kickRange); StartCoroutine(AttackAnim("LegR", -0.25f)); }
        }
    }

    System.Collections.IEnumerator AttackAnim(string limbName, float yOffset)
    {
        var limb = transform.Find(limbName);
        if (limb == null) yield break;

        var sr       = limb.GetComponent<SpriteRenderer>();
        var origPos  = limb.localPosition;
        var origColor = sr ? sr.color : Color.white;

        float facing = transform.localScale.x;
        limb.localPosition = origPos + new Vector3(facing * 0.45f, yOffset, 0f);
        if (sr) sr.color = Color.yellow;

        yield return new WaitForSeconds(0.1f);

        limb.localPosition = origPos;
        if (sr) sr.color = origColor;
    }

    public void TryAttack(float damage, float range)
    {
        if (opponent == null) return;
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (Vector2.Distance(transform.position, opponent.position) > range) return;

        lastAttackTime = Time.time;
        opponent.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
    }

    public void TryJump()
    {
        if (grounded) rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void FaceOpponent()
    {
        if (opponent == null) return;
        float dir = opponent.position.x - transform.position.x;
        transform.localScale = new Vector3(dir >= 0 ? 1f : -1f, 1f, 1f);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground")) grounded = true;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground")) grounded = false;
    }

    // ── New Input System helpers ──────────────────────────────────────────────

    static bool KeyHeld(KeyCode kc)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        var k = Map(kc);
        return k != Key.None && kb[k].isPressed;
    }

    static bool KeyDown(KeyCode kc)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        var k = Map(kc);
        return k != Key.None && kb[k].wasPressedThisFrame;
    }

    static Key Map(KeyCode kc) => kc switch
    {
        KeyCode.A          => Key.A,
        KeyCode.B          => Key.B,
        KeyCode.C          => Key.C,
        KeyCode.D          => Key.D,
        KeyCode.E          => Key.E,
        KeyCode.F          => Key.F,
        KeyCode.G          => Key.G,
        KeyCode.H          => Key.H,
        KeyCode.I          => Key.I,
        KeyCode.J          => Key.J,
        KeyCode.K          => Key.K,
        KeyCode.L          => Key.L,
        KeyCode.M          => Key.M,
        KeyCode.N          => Key.N,
        KeyCode.O          => Key.O,
        KeyCode.P          => Key.P,
        KeyCode.Q          => Key.Q,
        KeyCode.R          => Key.R,
        KeyCode.S          => Key.S,
        KeyCode.T          => Key.T,
        KeyCode.U          => Key.U,
        KeyCode.V          => Key.V,
        KeyCode.W          => Key.W,
        KeyCode.X          => Key.X,
        KeyCode.Y          => Key.Y,
        KeyCode.Z          => Key.Z,
        KeyCode.Space      => Key.Space,
        KeyCode.Return     => Key.Enter,
        KeyCode.UpArrow    => Key.UpArrow,
        KeyCode.DownArrow  => Key.DownArrow,
        KeyCode.LeftArrow  => Key.LeftArrow,
        KeyCode.RightArrow => Key.RightArrow,
        KeyCode.Comma      => Key.Comma,
        KeyCode.Period     => Key.Period,
        KeyCode.Slash      => Key.Slash,
        KeyCode.Semicolon  => Key.Semicolon,
        KeyCode.Quote      => Key.Quote,
        KeyCode.LeftShift  => Key.LeftShift,
        KeyCode.RightShift => Key.RightShift,
        KeyCode.LeftControl=> Key.LeftCtrl,
        KeyCode.RightControl=>Key.RightCtrl,
        _                  => Key.None
    };
}
