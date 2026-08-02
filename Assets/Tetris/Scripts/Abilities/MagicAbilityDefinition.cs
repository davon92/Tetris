using UnityEngine;

/// <summary>
/// What a spell physically does to a board. The name a player sees is authored
/// per asset; this is only the shape of the effect.
/// </summary>
public enum MagicEffect
{
    /// <summary>Carves whole columns out of the stack — deep wells that want vertical I-pieces.</summary>
    CarveColumns,

    /// <summary>Blasts a tapered crater into the stack — jagged, overhang-heavy damage.</summary>
    Crater,

    /// <summary>Cancels incoming garbage and dissolves garbage rows already on the board.</summary>
    Mend
}

/// <summary>Which of a fighter's two spell slots an ability is authored for.</summary>
public enum MagicAbilitySlot
{
    /// <summary>Fired at the opponent's board.</summary>
    Offensive,

    /// <summary>Fired at the caster's own board.</summary>
    Defensive
}

/// <summary>
/// One tunable spell. Everything a designer balances lives here — cost and the
/// numbers behind the effect — so two fighters can share a spell asset and a
/// rebalance is one file, not a code change.
/// </summary>
[CreateAssetMenu(
    menuName = "Tetris/Magic Ability",
    fileName = "Ability",
    order = 20)]
public sealed class MagicAbilityDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Shown on the spell plate and the cast callout. Keep it short — the plate is 108px wide.")]
    [SerializeField] private string displayName = "LIGHTNING";

    [Tooltip("Offensive spells hit the opponent; defensive spells land on the caster.")]
    [SerializeField] private MagicAbilitySlot slot = MagicAbilitySlot.Offensive;

    [Tooltip("Colour for the callout and the board flash when this lands.")]
    [SerializeField] private Color accent = new Color(0.55f, 0.85f, 1f);

    [Header("Cost")]
    [Tooltip("Mana spent per cast. A fighter with a smaller mana pool reaches this sooner.")]
    [SerializeField, Min(1)] private int manaCost = 100;

    [Header("Effect")]
    [SerializeField] private MagicEffect effect = MagicEffect.CarveColumns;

    [Tooltip("Carve Columns: how many adjacent columns are emptied top to bottom.")]
    [SerializeField, Min(1)] private int columnCount = 2;

    [Tooltip("Crater: width of the blast at its widest row.")]
    [SerializeField, Min(2)] private int craterWidth = 4;

    [Tooltip("Crater: how many rows tall the blast is.")]
    [SerializeField, Min(1)] private int craterHeight = 4;

    [Tooltip("Crater: rows below the surface to sink the blast. Higher leaves more sealed overhangs.")]
    [SerializeField, Min(0)] private int craterDepth = 4;

    [Tooltip("Mend: garbage rows dissolved off the caster's own board.")]
    [SerializeField, Min(0)] private int mendRows = 2;

    [Tooltip("Mend: pending garbage cancelled before it ever lands.")]
    [SerializeField, Min(0)] private int garbageCancelled = 4;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name.ToUpperInvariant() : displayName;

    public MagicAbilitySlot Slot => slot;
    public Color Accent => accent;
    public int ManaCost => Mathf.Max(1, manaCost);
    public MagicEffect Effect => effect;
    public int ColumnCount => Mathf.Max(1, columnCount);
    public int CraterWidth => Mathf.Max(2, craterWidth);
    public int CraterHeight => Mathf.Max(1, craterHeight);
    public int CraterDepth => Mathf.Max(0, craterDepth);
    public int MendRows => Mathf.Max(0, mendRows);
    public int GarbageCancelled => Mathf.Max(0, garbageCancelled);

    /// <summary>
    /// Builds one of the stock spells in memory. Used as the fallback when no
    /// authored assets exist yet, and as the source the editor baker writes out,
    /// so the defaults are defined in exactly one place.
    /// </summary>
    public static MagicAbilityDefinition CreateBuiltIn(BuiltInAbility builtIn)
    {
        MagicAbilityDefinition ability = CreateInstance<MagicAbilityDefinition>();
        switch (builtIn)
        {
            case BuiltInAbility.Lightning:
                ability.name = "Lightning";
                ability.displayName = "LIGHTNING";
                ability.slot = MagicAbilitySlot.Offensive;
                ability.accent = new Color(0.55f, 0.85f, 1f);
                ability.effect = MagicEffect.CarveColumns;
                ability.manaCost = 100;
                ability.columnCount = 2;
                break;

            case BuiltInAbility.Starburst:
                ability.name = "Starburst";
                ability.displayName = "STARBURST";
                ability.slot = MagicAbilitySlot.Offensive;
                ability.accent = new Color(1f, 0.55f, 0.2f);
                ability.effect = MagicEffect.Crater;
                ability.manaCost = 100;
                ability.craterWidth = 4;
                ability.craterHeight = 4;
                ability.craterDepth = 4;
                break;

            default:
                ability.name = "Mending Light";
                ability.displayName = "MENDING LIGHT";
                ability.slot = MagicAbilitySlot.Defensive;
                ability.accent = new Color(0.45f, 1f, 0.7f);
                ability.effect = MagicEffect.Mend;
                ability.manaCost = 60;
                ability.mendRows = 2;
                ability.garbageCancelled = 4;
                break;
        }

        return ability;
    }
}

/// <summary>The spells the game ships with before a designer authors any.</summary>
public enum BuiltInAbility
{
    Lightning,
    Starburst,
    MendingLight
}
