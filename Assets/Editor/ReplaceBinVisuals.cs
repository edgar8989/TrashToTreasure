#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Letakkan di Assets/Editor/ReplaceBinVisuals.cs
/// Pilih objek di Hierarchy (mis. Bin_Organik, atau induk RecyclingArea / ProcessingArea),
/// lalu pakai menu Tools > Replace Visual.
/// Script, collider, dan komponen lain di objek TIDAK diubah; hanya tampilannya.
/// </summary>
public static class ReplaceBinVisuals
{
    const string PrefabDir = "Assets/GeneratedTrash/Prefabs/";
    const float VisualScale = 1f; // ubah kalau model terlalu kecil/besar

    [MenuItem("Tools/Replace Visual/Auto (dari nama / induk)")]
    static void Auto()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go.name == "RecyclingArea" || go.name == "ProcessingArea")
            {
                foreach (Transform c in go.transform)
                    n += Process(c.gameObject, GuessPrefab(c.gameObject)) ? 1 : 0;
            }
            else n += Process(go, GuessPrefab(go)) ? 1 : 0;
        }
        Debug.Log("Replace Visual: " + n + " objek diganti.");
    }

    [MenuItem("Tools/Replace Visual/Pilihan -> Bin_Organik")]
    static void ToBinOrg() { ApplyToSelection("Bin_Organik"); }

    [MenuItem("Tools/Replace Visual/Pilihan -> Bin_Anorganik")]
    static void ToBinAnorg() { ApplyToSelection("Bin_Anorganik"); }

    [MenuItem("Tools/Replace Visual/Pilihan -> Mesin_Organik")]
    static void ToMesinOrg() { ApplyToSelection("Mesin_Organik"); }

    [MenuItem("Tools/Replace Visual/Pilihan -> Mesin_Anorganik")]
    static void ToMesinAnorg() { ApplyToSelection("Mesin_Anorganik"); }

    static void ApplyToSelection(string prefabName)
    {
        int n = 0;
        foreach (var go in Selection.gameObjects) n += Process(go, prefabName) ? 1 : 0;
        Debug.Log("Replace Visual: " + n + " objek diganti dengan " + prefabName);
    }

    static string GuessPrefab(GameObject go)
    {
        string lower = go.name.ToLower();
        bool anorg = lower.Contains("anorganik") || lower.Contains("anorganic") || lower.Contains("inorganic");
        bool org = !anorg && (lower.Contains("organik") || lower.Contains("organic"));
        if (!anorg && !org)
        {
            Debug.LogWarning("Tidak bisa menebak jenis (organik/anorganik) dari nama: " + go.name +
                             ". Pilih objeknya lalu pakai menu 'Pilihan -> ...'.");
            return null;
        }

        string parent = go.transform.parent != null ? go.transform.parent.name : "";
        bool machine = parent == "ProcessingArea"
            || lower.Contains("mesin") || lower.Contains("machine") || lower.Contains("daur")
            || lower.Contains("recycl") || lower.Contains("proses") || lower.Contains("process")
            || lower.Contains("kompos") || lower.Contains("shred");
        if (parent == "RecyclingArea") machine = false;

        string kind = machine ? "Mesin_" : "Bin_";
        return kind + (anorg ? "Anorganik" : "Organik");
    }

    static bool Process(GameObject target, string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + prefabName + ".prefab");
        if (prefab == null)
        {
            Debug.LogWarning("Prefab tidak ada: " + prefabName + ". Jalankan Tools > Generate Bins And Machines dulu.");
            return false;
        }

        // Hitung posisi dasar objek lama (sebelum renderer dimatikan)
        var rends = target.GetComponents<Renderer>();
        if (rends.Length == 0) rends = target.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            Debug.LogWarning("Tidak ada Renderer di " + target.name + ", dilewati.");
            return false;
        }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        Vector3 bottomWorld = new Vector3(b.center.x, b.min.y, b.center.z);

        Undo.RegisterFullObjectHierarchyUndo(target, "Replace Visual");

        // Hapus visual hasil penggantian sebelumnya (supaya bisa diulang)
        var old = new List<GameObject>();
        foreach (Transform c in target.transform)
            if (c.name.StartsWith("Visual_")) old.Add(c.gameObject);
        foreach (var o in old) Undo.DestroyObjectImmediate(o);

        // Matikan tampilan lama; script & collider tetap
        foreach (var r in target.GetComponents<Renderer>()) r.enabled = false;

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target.transform);
        visual.name = "Visual_" + prefabName;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * VisualScale;
        visual.transform.position = bottomWorld; // dasar model = dasar objek lama

        foreach (var c in visual.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        Undo.RegisterCreatedObjectUndo(visual, "Replace Visual");
        return true;
    }
}
#endif
