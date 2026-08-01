/// <summary>
/// The spell a character casts when they earn a trigger: a tetris (4-line
/// clear) or clearing a row containing a gold mana cell. Offense fires at the
/// opponent's board; Heal mends the caster's own.
/// </summary>
public enum MagicAbility
{
    /// <summary>Carves two adjacent columns out of the enemy stack — deep wells that want vertical I-pieces.</summary>
    Lightning,

    /// <summary>Blasts a 2-4-4-2 diamond crater into the enemy stack — jagged, overhang-heavy damage.</summary>
    Starburst,

    /// <summary>Cancels incoming garbage and dissolves garbage rows already on the caster's board.</summary>
    Heal
}

public static class MagicAbilityInfo
{
    public static string DisplayName(MagicAbility ability)
    {
        return ability switch
        {
            MagicAbility.Lightning => "LIGHTNING",
            MagicAbility.Starburst => "STARBURST",
            _ => "MENDING LIGHT"
        };
    }
}
