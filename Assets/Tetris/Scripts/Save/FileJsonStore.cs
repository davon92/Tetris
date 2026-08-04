using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Stores one JSON document per key under
/// <c>Application.persistentDataPath/saves</c>. Writes land in a temporary
/// file first and only then replace the real one, so a crash or a pulled power
/// cable can never leave a half-written save behind.
/// </summary>
public sealed class FileJsonStore : IJsonStore
{
    private const string FolderName = "saves";
    private const string Extension = ".json";
    private const string TempExtension = ".tmp";

    private readonly string root;

    public FileJsonStore()
        : this(Path.Combine(Application.persistentDataPath, FolderName))
    {
    }

    public FileJsonStore(string root)
    {
        this.root = root;
    }

    public string Root => root;

    public bool Exists(string key)
    {
        return IsValidKey(key) && File.Exists(PathFor(key));
    }

    public bool TryRead(string key, out string json)
    {
        json = string.Empty;
        if (!IsValidKey(key))
            return false;

        string path = PathFor(key);
        try
        {
            if (!File.Exists(path))
                return false;

            json = File.ReadAllText(path);
            return !string.IsNullOrWhiteSpace(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not read '{path}': {exception.Message}");
            return false;
        }
    }

    public bool Write(string key, string json)
    {
        if (!IsValidKey(key))
            return false;

        string path = PathFor(key);
        string tempPath = path + TempExtension;
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(tempPath, json ?? string.Empty);

            if (File.Exists(path))
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not write '{path}': {exception.Message}");
            TryDeleteFile(tempPath);
            return false;
        }
    }

    public bool Delete(string key)
    {
        if (!IsValidKey(key))
            return false;

        string path = PathFor(key);
        if (!File.Exists(path))
            return false;

        return TryDeleteFile(path);
    }

    private string PathFor(string key)
    {
        return Path.Combine(root, key + Extension);
    }

    /// <summary>
    /// Keys come from game code rather than from players, but they end up in a
    /// path, so anything that could escape the save folder is rejected.
    /// </summary>
    private static bool IsValidKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) &&
               key.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    private static bool TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not delete '{path}': {exception.Message}");
            return false;
        }
    }
}
