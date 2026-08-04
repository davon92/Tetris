using System.Collections.Generic;

/// <summary>
/// Key/value persistence for the game's JSON documents. Save slots and
/// statistics both go through this seam, so neither one knows about the file
/// system and both can be exercised in EditMode against
/// <see cref="MemoryJsonStore"/>.
/// </summary>
public interface IJsonStore
{
    bool Exists(string key);

    bool TryRead(string key, out string json);

    /// <summary>Returns false when the document could not be persisted.</summary>
    bool Write(string key, string json);

    bool Delete(string key);
}

/// <summary>
/// In-memory store for tests and for platforms where writing failed at
/// startup — the game keeps running, it just forgets between sessions.
/// </summary>
public sealed class MemoryJsonStore : IJsonStore
{
    private readonly Dictionary<string, string> documents = new Dictionary<string, string>();

    public bool Exists(string key)
    {
        return !string.IsNullOrEmpty(key) && documents.ContainsKey(key);
    }

    public bool TryRead(string key, out string json)
    {
        json = string.Empty;
        return !string.IsNullOrEmpty(key) && documents.TryGetValue(key, out json);
    }

    public bool Write(string key, string json)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        documents[key] = json ?? string.Empty;
        return true;
    }

    public bool Delete(string key)
    {
        return !string.IsNullOrEmpty(key) && documents.Remove(key);
    }
}
