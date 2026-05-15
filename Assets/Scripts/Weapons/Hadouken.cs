using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Hadouken : MonoBehaviour
{
    public float     damage;
    public Transform ownerRoot;

    Transform arc1, arc2;
    float     spin;

    void Awake()
    {
        arc1 = transform.Find("Arc1");
        arc2 = transform.Find("Arc2");
    }

    void Update()
    {
        spin += Time.deltaTime * 360f;
        if (arc1) arc1.localEulerAngles = new Vector3(0f, 0f,  spin * 1.5f);
        if (arc2) arc2.localEulerAngles = new Vector3(0f, 0f, -spin * 2.1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        var fc = other.GetComponentInParent<FighterController>();
        if (fc != null)
        {
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            SpawnBurst();
            Destroy(gameObject);
            return;
        }
        if (other.CompareTag("Ground")) { SpawnBurst(); Destroy(gameObject); }
    }

    void SpawnBurst()
    {
        var refSR = GetComponentInChildren<SpriteRenderer>();
        Sprite spr = refSR ? refSR.sprite : null;
        for (int i = 0; i < 8; i++)
        {
            var p  = new GameObject("HBurst");
            p.transform.position   = transform.position;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
            var sr = p.AddComponent<SpriteRenderer>();
            if (spr) sr.sprite = spr;
            sr.color        = Color.Lerp(new Color(1f, 0.55f, 0f), Color.white, Random.value * 0.5f);
            sr.sortingOrder = 7;
            var prb = p.AddComponent<Rigidbody2D>();
            prb.gravityScale = 0.2f;
            float a = i * (360f / 8f) * Mathf.Deg2Rad;
            prb.linearVelocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.5f, 6f);
            Destroy(p, 0.45f);
        }
    }
}
