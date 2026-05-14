using UnityEngine;

// Simple state-machine AI: approach → attack → jump to avoid
public class AIBrain : MonoBehaviour
{
    public float reactionInterval = 0.25f;
    public float preferredDistance = 1.5f;
    public float jumpChance = 0.15f;

    FighterController fighter;
    FighterHealth     health;
    float             nextDecision;

    void Awake()
    {
        fighter = GetComponent<FighterController>();
        health  = GetComponent<FighterHealth>();
        fighter.isPlayerControlled = false;
    }

    void Update()
    {
        if (health.IsDead || fighter.opponent == null) return;
        if (Time.time < nextDecision) return;
        nextDecision = Time.time + reactionInterval;

        float dx   = fighter.opponent.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        float combatDist = fighter.equippedWeapon != null
            ? fighter.equippedWeapon.CombatRange
            : preferredDistance;

        if (dist > combatDist + 0.4f)
        {
            fighter.aiMoveInput = Mathf.Sign(dx) * 0.85f;
        }
        else if (dist < combatDist - 0.5f)
        {
            fighter.aiMoveInput = -Mathf.Sign(dx) * 0.5f;
        }
        else
        {
            fighter.aiMoveInput = 0f;
            if (fighter.equippedWeapon != null)
                fighter.equippedWeapon.TryAttack();
            else
            {
                if (Random.value < 0.55f) fighter.TryAttack(fighter.punchDamage, fighter.punchRange);
                else                      fighter.TryAttack(fighter.kickDamage,  fighter.kickRange);
            }
        }

        if (Random.value < jumpChance)
            fighter.TryJump();
    }
}
