// Editor klasörüne koy
using UnityEditor;
using UnityEngine;

public static class MaterialUserDataStore
{
    public static bool TryLoad<T>(Material mat, out T data) where T : new()
    {
        data = new T();
        if (!mat) return false;

        var path = AssetDatabase.GetAssetPath(mat);
        if (string.IsNullOrEmpty(path)) return false;

        var importer = AssetImporter.GetAtPath(path);
        if (importer == null || string.IsNullOrEmpty(importer.userData)) return false;

        // JSON -> struct/class
        try
        {
            data = JsonUtility.FromJson<T>(importer.userData);
            return true;
        }
        catch { return false; }
    }

    public static void Save<T>(Material mat, T data)
    {
        if (!mat) return;

        var path = AssetDatabase.GetAssetPath(mat);
        if (string.IsNullOrEmpty(path)) return;

        var importer = AssetImporter.GetAtPath(path);
        if (importer == null) return;

        // struct/class -> JSON
        importer.userData = JsonUtility.ToJson(data);

        // kalıcılaştır
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(path);
        AssetDatabase.SaveAssets();
    }
}