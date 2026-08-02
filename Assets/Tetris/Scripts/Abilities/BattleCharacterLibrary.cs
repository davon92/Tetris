using UnityEngine;

/// <summary>
/// The roster, in select order. Dropping this asset at
/// <c>Assets/Tetris/Resources/BattleCharacterLibrary.asset</c> is all the
/// wiring there is — <see cref="BattleCharacterRoster"/> finds it by name, and
/// falls back to the built-in fighters when it is absent so the game always
/// runs.
/// </summary>
[CreateAssetMenu(
    menuName = "Tetris/Battle Character Library",
    fileName = "BattleCharacterLibrary",
    order = 0)]
public sealed class BattleCharacterLibrary : ScriptableObject
{
    /// <summary>The name the roster loads this asset by. Renaming the file breaks the lookup.</summary>
    public const string ResourceName = "BattleCharacterLibrary";

    [Tooltip("Select-screen order. Every entry must be filled — empty slots are skipped.")]
    [SerializeField] private BattleCharacter[] characters = new BattleCharacter[0];

    public BattleCharacter[] Characters => characters;

    public void SetCharacters(BattleCharacter[] value)
    {
        characters = value ?? new BattleCharacter[0];
    }
}
