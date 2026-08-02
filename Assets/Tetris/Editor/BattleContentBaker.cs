using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes the built-in roster out as real assets so it can be tuned in the
/// Inspector. The game runs off the built-ins until this is used, so baking is
/// a one-time step a designer takes when they want to start balancing —
/// nothing breaks if it is never run.
/// </summary>
public static class BattleContentBaker
{
    // Only the library sits in Resources — anything under Resources is force
    // included in the build, and the library already references the rest.
    private const string ResourcesPath = "Assets/Tetris/Resources";
    private const string ContentPath = "Assets/Tetris/Battle";
    private const string AbilitiesPath = ContentPath + "/Abilities";
    private const string CharactersPath = ContentPath + "/Characters";

    [MenuItem("Tetris/Bake Default Battle Content", priority = 100)]
    public static void Bake()
    {
        if (AssetDatabase.LoadAssetAtPath<BattleCharacterLibrary>(LibraryPath()) != null &&
            !EditorUtility.DisplayDialog(
                "Bake Default Battle Content",
                "A character library already exists. Baking overwrites every character " +
                "and ability asset with the built-in defaults, discarding your tuning.\n\n" +
                "Continue?",
                "Overwrite",
                "Cancel"))
        {
            return;
        }

        EnsureFolder(ResourcesPath);
        EnsureFolder(ContentPath);
        EnsureFolder(AbilitiesPath);
        EnsureFolder(CharactersPath);

        // Baking from the same builder the runtime falls back to keeps the
        // assets and the built-ins from drifting apart.
        BattleCharacter[] defaults = BattleCharacterRoster.BuildDefaults();
        Dictionary<MagicAbilityDefinition, MagicAbilityDefinition> abilityAssets = new();
        List<BattleCharacter> characterAssets = new();

        foreach (BattleCharacter source in defaults)
        {
            BattleCharacter character = BattleCharacter.Create(
                source.Id,
                source.DisplayName,
                source.Title,
                source.Portrait,
                source.Accent,
                source.UnlockedByDefault,
                source.StartingGarbage,
                source.ManaCapacity,
                BakeAbility(source.OffensiveAbility, abilityAssets),
                BakeAbility(source.DefensiveAbility, abilityAssets));

            characterAssets.Add(Write(character, $"{CharactersPath}/{source.Id}.asset"));
        }

        BattleCharacterLibrary library = ScriptableObject.CreateInstance<BattleCharacterLibrary>();
        library.SetCharacters(characterAssets.ToArray());
        Write(library, LibraryPath());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BattleCharacterRoster.Invalidate();

        Debug.Log(
            $"Baked {characterAssets.Count} characters and {abilityAssets.Count} abilities " +
            $"under {ResourcesPath}. Tune them in the Inspector — the game reads them on play.");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<BattleCharacterLibrary>(LibraryPath());
    }

    /// <summary>
    /// Characters share ability instances, so each distinct spell is written
    /// once and every character that used it points at the same asset —
    /// rebalancing a spell then hits every fighter that carries it.
    /// </summary>
    private static MagicAbilityDefinition BakeAbility(
        MagicAbilityDefinition source,
        Dictionary<MagicAbilityDefinition, MagicAbilityDefinition> written)
    {
        if (source == null)
            return null;

        if (written.TryGetValue(source, out MagicAbilityDefinition existing))
            return existing;

        MagicAbilityDefinition asset = Write(
            Object.Instantiate(source), $"{AbilitiesPath}/{source.name}.asset");
        written[source] = asset;
        return asset;
    }

    private static T Write<T>(T asset, string path) where T : ScriptableObject
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static string LibraryPath()
    {
        return $"{ResourcesPath}/{BattleCharacterLibrary.ResourceName}.asset";
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
