using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The display-facing summary of one slot. The slot list never needs the full
/// save payload, so it reads these instead and the catalog only touches the
/// store when a slot is actually written or loaded.
/// </summary>
public readonly struct SaveSlotInfo
{
    private SaveSlotInfo(
        int index,
        bool isEmpty,
        bool isUsable,
        string chapterTitle,
        string speaker,
        string previewText,
        float playtimeSeconds,
        DateTime savedAtUtc)
    {
        Index = index;
        IsEmpty = isEmpty;
        IsUsable = isUsable;
        ChapterTitle = chapterTitle;
        Speaker = speaker;
        PreviewText = previewText;
        PlaytimeSeconds = playtimeSeconds;
        SavedAtUtc = savedAtUtc;
    }

    public int Index { get; }
    public bool IsEmpty { get; }

    /// <summary>False for a slot that exists but cannot be read back.</summary>
    public bool IsUsable { get; }

    public string ChapterTitle { get; }
    public string Speaker { get; }
    public string PreviewText { get; }
    public float PlaytimeSeconds { get; }
    public DateTime SavedAtUtc { get; }

    public string PlaytimeText => SaveSlotCatalog.FormatPlaytime(PlaytimeSeconds);

    /// <summary>Compact local timestamp; slot cards have no room for the year.</summary>
    public string SavedAtText =>
        SavedAtUtc == DateTime.MinValue
            ? string.Empty
            : SavedAtUtc.ToLocalTime().ToString("MM-dd  HH:mm");

    public static SaveSlotInfo Empty(int index)
    {
        return new SaveSlotInfo(
            index, true, false, string.Empty, string.Empty, string.Empty, 0f, DateTime.MinValue);
    }

    public static SaveSlotInfo Corrupt(int index)
    {
        return new SaveSlotInfo(
            index, false, false, "DAMAGED SAVE", string.Empty,
            "This slot cannot be read by this version.", 0f, DateTime.MinValue);
    }

    public static SaveSlotInfo From(int index, StorySaveData data)
    {
        return new SaveSlotInfo(
            index,
            false,
            true,
            data.chapterTitle,
            data.speaker,
            data.previewText,
            data.playtimeSeconds,
            data.SavedAtUtc);
    }
}

/// <summary>
/// The ten story save slots. Owns slot numbering and the summary cache; the
/// actual bytes live in an <see cref="IJsonStore"/>, which keeps the whole
/// class testable and free of file-system knowledge.
/// </summary>
public sealed class SaveSlotCatalog
{
    public const int SlotCount = 10;

    private const string KeyPrefix = "story-slot-";

    private readonly IJsonStore store;
    private readonly SaveSlotInfo[] slots = new SaveSlotInfo[SlotCount];

    public SaveSlotCatalog(IJsonStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        Refresh();
    }

    public IReadOnlyList<SaveSlotInfo> Slots => slots;

    public bool HasAnySave
    {
        get
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                    return true;
            }

            return false;
        }
    }

    public static bool IsValidSlot(int index)
    {
        return index >= 0 && index < SlotCount;
    }

    public SaveSlotInfo GetSlot(int index)
    {
        return IsValidSlot(index) ? slots[index] : SaveSlotInfo.Empty(index);
    }

    /// <summary>Re-reads every slot summary from the store.</summary>
    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i] = ReadInfo(i);
    }

    public bool Save(int index, StorySaveData data)
    {
        if (!IsValidSlot(index) || data == null)
            return false;

        if (!store.Write(KeyFor(index), JsonUtility.ToJson(data, true)))
            return false;

        slots[index] = SaveSlotInfo.From(index, data);
        return true;
    }

    public bool TryLoad(int index, out StorySaveData data)
    {
        data = null;
        if (!IsValidSlot(index) || !store.TryRead(KeyFor(index), out string json))
            return false;

        data = Deserialize(json);
        if (data != null)
            return true;

        slots[index] = SaveSlotInfo.Corrupt(index);
        return false;
    }

    public bool Delete(int index)
    {
        if (!IsValidSlot(index) || !store.Delete(KeyFor(index)))
            return false;

        slots[index] = SaveSlotInfo.Empty(index);
        return true;
    }

    /// <summary>Slot numbers read as 01–10 everywhere a player can see them.</summary>
    public static string SlotLabel(int index)
    {
        return (index + 1).ToString("00");
    }

    public static string FormatPlaytime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";
    }

    private SaveSlotInfo ReadInfo(int index)
    {
        string key = KeyFor(index);
        if (!store.Exists(key))
            return SaveSlotInfo.Empty(index);

        if (!store.TryRead(key, out string json))
            return SaveSlotInfo.Corrupt(index);

        StorySaveData data = Deserialize(json);
        return data != null ? SaveSlotInfo.From(index, data) : SaveSlotInfo.Corrupt(index);
    }

    private static StorySaveData Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            StorySaveData data = JsonUtility.FromJson<StorySaveData>(json);
            return data != null && data.IsUsable ? data : null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not parse a save slot: {exception.Message}");
            return null;
        }
    }

    /// <summary>The store key backing a slot. Public so migration tools and
    /// tests can reach the raw document without duplicating the format.</summary>
    public static string KeyFor(int index)
    {
        return KeyPrefix + index.ToString("00");
    }
}
