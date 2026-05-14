using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fighters")]
    public FighterController player1;
    public FighterController player2;

    [Header("Spawn Points")]
    public Vector3 p1Spawn = new Vector3(-3f, -1.1f, 0f);
    public Vector3 p2Spawn = new Vector3( 3f, -1.1f, 0f);

    [Header("UI")]
    public HealthBar       p1HealthBar;
    public HealthBar       p2HealthBar;
    public TextMeshProUGUI roundInfoText;
    public TextMeshProUGUI winnerText;
    public WeaponSelectUI  weaponSelectUI;
    public ModeSelectUI    modeSelectUI;

    [Header("Settings")]
    public float resetDelay = 2.5f;
    public bool  p2IsAI     = false;

    int  p1Wins, p2Wins;
    bool roundOver;

    void Awake() => Instance = this;

    void Start()
    {
        player1.GetComponent<FighterHealth>().onHealthChanged.AddListener(v => p1HealthBar?.SetHealth(v));
        player2.GetComponent<FighterHealth>().onHealthChanged.AddListener(v => p2HealthBar?.SetHealth(v));

        FreezeAll();

        if (modeSelectUI != null)
            modeSelectUI.Show(OnModeChosen);
        else
            OnModeChosen(p2IsAI);           // fallback: use Inspector value
    }

    void OnModeChosen(bool ai)
    {
        p2IsAI = ai;

        if (p2IsAI)
        {
            if (player2.GetComponent<AIBrain>() == null)
                player2.gameObject.AddComponent<AIBrain>();
            player2.isPlayerControlled = false;
        }
        else
        {
            var brain = player2.GetComponent<AIBrain>();
            if (brain != null) Destroy(brain);
            player2.isPlayerControlled = true;
        }

        ShowWeaponSelect();
    }

    // ── Weapon selection ─────────────────────────────────────────────────────

    void ShowWeaponSelect()
    {
        FreezeAll();
        if (weaponSelectUI != null)
            weaponSelectUI.Show(p2IsAI, OnWeaponsChosen);
        else
            OnWeaponsChosen(WeaponType.Sword, WeaponType.Bow);
    }

    void OnWeaponsChosen(WeaponType p1Type, WeaponType p2Type)
    {
        weaponSelectUI?.Hide();
        WeaponFactory.Equip(player1, p1Type);
        WeaponFactory.Equip(player2, p2Type);
        UnfreezeAll();
        StartRound();
    }

    void FreezeAll()
    {
        player1.inputFrozen = true;
        player2.inputFrozen = true;
    }

    void UnfreezeAll()
    {
        player1.inputFrozen = false;
        player2.inputFrozen = false;
    }

    // ── Round logic ──────────────────────────────────────────────────────────

    void StartRound()
    {
        roundOver = false;

        player1.transform.position = p1Spawn;
        player2.transform.position = p2Spawn;

        player1.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player2.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        player1.GetComponent<FighterHealth>().ResetHealth();
        player2.GetComponent<FighterHealth>().ResetHealth();

        player1.opponent = player2.transform;
        player2.opponent = player1.transform;

        if (winnerText) winnerText.gameObject.SetActive(false);
        UpdateRoundText();
    }

    public void OnFighterDied(FighterHealth loser)
    {
        if (roundOver) return;
        roundOver = true;

        bool p1Lost = loser.gameObject == player1.gameObject;
        if (p1Lost) p2Wins++; else p1Wins++;

        string winner = p1Lost ? "Player 2" : "Player 1";
        if (winnerText)
        {
            winnerText.text = $"{winner} Wins!";
            winnerText.gameObject.SetActive(true);
        }
        UpdateRoundText();
        FreezeAll();
        Invoke(nameof(ShowWeaponSelect), resetDelay);
    }

    void UpdateRoundText()
    {
        if (roundInfoText)
            roundInfoText.text = $"P1: {p1Wins}   P2: {p2Wins}";
    }
}
