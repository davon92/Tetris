using UnityEngine;

public enum BattleCharacterPortrait
{
    Lyra,
    Bram,
    Locked
}

/// <summary>
/// One playable fighter. Identity, the handicap they carry into a match, the
/// size of their mana pool, and the two spells they bring — all authored, so
/// balancing is Inspector work rather than a code change.
/// </summary>
[CreateAssetMenu(
    menuName = "Tetris/Battle Character",
    fileName = "Character",
    order = 10)]
public sealed class BattleCharacter : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key used by save data and story scripts. Never rename it once it ships.")]
    [SerializeField] private string id = "new-character";

    [SerializeField] private string displayName = "???";
    [SerializeField] private string title = "CHALLENGER";
    [SerializeField] private BattleCharacterPortrait portrait = BattleCharacterPortrait.Locked;

    [Tooltip("Drives every plate, border and bar tint for this fighter's seat.")]
    [SerializeField] private Color accent = new Color(0.56f, 0.48f, 0.78f);

    [SerializeField] private bool unlockedByDefault;

    [Header("Vitals")]
    [Tooltip(
        "Garbage rows this fighter is buried under at the opening bell. Health " +
        "is read off board fill, so starting garbage is exactly a starting " +
        "health penalty — and it is recoverable, since clearing it wins it back.")]
    [SerializeField, Range(0, 8)] private int startingGarbage;

    [Header("Magic")]
    [Tooltip(
        "Size of the mana pool. Spell costs are absolute, so a smaller pool " +
        "means fewer clears to afford the same spell — this is the dial for a " +
        "fighter who casts often.")]
    [SerializeField, Min(10)] private int manaCapacity = 100;

    [Tooltip("Fired at the opponent. RB on a pad, F on the keyboard.")]
    [SerializeField] private MagicAbilityDefinition offensiveAbility;

    [Tooltip("Lands on this fighter's own board. LB on a pad, C on the keyboard.")]
    [SerializeField] private MagicAbilityDefinition defensiveAbility;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => displayName;
    public string Title => title;
    public BattleCharacterPortrait Portrait => portrait;
    public Color Accent => accent;
    public bool UnlockedByDefault => unlockedByDefault;
    public int StartingGarbage => Mathf.Max(0, startingGarbage);
    public int ManaCapacity => Mathf.Max(10, manaCapacity);
    public MagicAbilityDefinition OffensiveAbility => offensiveAbility;
    public MagicAbilityDefinition DefensiveAbility => defensiveAbility;

    /// <summary>
    /// Builds a fighter in memory. Both the no-assets fallback and the editor
    /// baker go through here so the shipped defaults have one definition.
    /// </summary>
    public static BattleCharacter Create(
        string id,
        string displayName,
        string title,
        BattleCharacterPortrait portrait,
        Color accent,
        bool unlockedByDefault,
        int startingGarbage,
        int manaCapacity,
        MagicAbilityDefinition offensive,
        MagicAbilityDefinition defensive)
    {
        BattleCharacter character = CreateInstance<BattleCharacter>();
        character.name = displayName;
        character.id = id;
        character.displayName = displayName;
        character.title = title;
        character.portrait = portrait;
        character.accent = accent;
        character.unlockedByDefault = unlockedByDefault;
        character.startingGarbage = startingGarbage;
        character.manaCapacity = manaCapacity;
        character.offensiveAbility = offensive;
        character.defensiveAbility = defensive;
        return character;
    }
}
