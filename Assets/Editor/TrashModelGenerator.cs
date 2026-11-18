#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Letakkan file ini di Assets/Editor/TrashModelGenerator.cs
/// Lalu jalankan lewat menu: Tools > Generate Trash Models
/// Hasil: Assets/GeneratedTrash/Prefabs/*.prefab
/// </summary>
public static class TrashModelGenerator
{
    const string RootDir = "Assets/GeneratedTrash";
    const string MatDir = RootDir + "/Materials";
    const string PrefabDir = RootDir + "/Prefabs";

    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Generate Trash Models")]
    public static void GenerateAll()
    {
        matCache.Clear();
        EnsureFolder(RootDir);
        EnsureFolder(MatDir);
        EnsureFolder(PrefabDir);

        // Produk jatuh
        Save(BuildPot(), "pot");
        Save(BuildBuku(), "buku");
        Save(BuildWadahLogam(), "wadah logam");
        Save(BuildKompos(), "kompos");

        // Sampah
        Save(BuildBotol(), "Trash_Botol");
        Save(BuildKertas(), "Trash_Kertas");
        Save(BuildKaleng(), "Trash_Kaleng");
        Save(BuildSisaMakanan(), "Trash_Sisa_Makanan");
        Save(BuildDaunKering(), "Trash_Daun_Kering");
        Save(BuildGelas(), "Trash_Gelas");
        Save(BuildKardus(), "Trash_Kardus");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Trash models generated di " + PrefabDir);
    }

    // ---------------------------------------------------------------- MODELS

    static GameObject BuildPot()
    {
        var r = NewRoot("pot");
        var clay = Mat("Terracotta", "#C8693C");
        var soil = Mat("Soil", "#4A2F1D");
        var green = Mat("PlantGreen", "#4CAF50");
        Cyl(r, "Base", 0, 0, 0, .07f, .04f, clay);
        Cyl(r, "Mid", 0, .04f, 0, .085f, .07f, clay);
        Cyl(r, "Rim", 0, .11f, 0, .095f, .025f, clay);
        Cyl(r, "Soil", 0, .125f, 0, .085f, .012f, soil);
        Cyl(r, "Stem", 0, .135f, 0, .004f, .08f, green);
        Sph(r, "LeafL", new Vector3(-.03f, .205f, 0), new Vector3(.07f, .02f, .04f), new Vector3(0, 0, 30), green);
        Sph(r, "LeafR", new Vector3(.03f, .2f, 0), new Vector3(.07f, .02f, .04f), new Vector3(0, 0, -30), green);
        Sph(r, "LeafTop", new Vector3(0, .225f, 0), new Vector3(.04f, .05f, .04f), Vector3.zero, green);
        return r;
    }

    static GameObject BuildBuku()
    {
        var r = NewRoot("buku");
        var cover = Mat("BookCover", "#2F5DA8");
        var pages = Mat("Pages", "#F3EBD3");
        var label = Mat("BookLabel", "#F2C94C");
        Box(r, "Back", new Vector3(0, .0025f, 0), new Vector3(.16f, .005f, .22f), Vector3.zero, cover);
        Box(r, "Front", new Vector3(0, .0275f, 0), new Vector3(.16f, .005f, .22f), Vector3.zero, cover);
        Box(r, "Spine", new Vector3(-.0775f, .015f, 0), new Vector3(.005f, .03f, .22f), Vector3.zero, cover);
        Box(r, "Pages", new Vector3(.005f, .015f, 0), new Vector3(.15f, .02f, .21f), Vector3.zero, pages);
        Box(r, "Label", new Vector3(0, .0302f, 0), new Vector3(.1f, .001f, .05f), Vector3.zero, label);
        return r;
    }

    static GameObject BuildWadahLogam()
    {
        var r = NewRoot("wadah logam");
        var metal = Mat("Metal", "#9AA3AB", 0.8f, 0.6f);
        var rim = Mat("MetalRim", "#C3CAD0", 0.8f, 0.7f);
        var dark = Mat("MetalInside", "#3A3F44");
        Cyl(r, "Base", 0, 0, 0, .07f, .04f, metal);
        Cyl(r, "Mid", 0, .04f, 0, .08f, .05f, metal);
        Cyl(r, "Top", 0, .09f, 0, .09f, .04f, metal);
        Cyl(r, "Rim", 0, .13f, 0, .095f, .01f, rim);
        Cyl(r, "Inside", 0, .139f, 0, .08f, .002f, dark);
        Box(r, "PostL", new Vector3(-.095f, .165f, 0), new Vector3(.008f, .07f, .008f), Vector3.zero, rim);
        Box(r, "PostR", new Vector3(.095f, .165f, 0), new Vector3(.008f, .07f, .008f), Vector3.zero, rim);
        Box(r, "Handle", new Vector3(0, .2f, 0), new Vector3(.198f, .008f, .008f), Vector3.zero, rim);
        return r;
    }

    static GameObject BuildKompos()
    {
        var r = NewRoot("kompos");
        var wood = Mat("Wood", "#A9743F");
        var soil = Mat("CompostSoil", "#3B2616");
        var green = Mat("PlantGreen", "#4CAF50");
        var peel = Mat("OrangePeel", "#F2994A");
        Box(r, "Floor", new Vector3(0, .005f, 0), new Vector3(.22f, .01f, .16f), Vector3.zero, wood);
        Box(r, "Front", new Vector3(0, .05f, .075f), new Vector3(.22f, .1f, .01f), Vector3.zero, wood);
        Box(r, "Back", new Vector3(0, .05f, -.075f), new Vector3(.22f, .1f, .01f), Vector3.zero, wood);
        Box(r, "Left", new Vector3(-.105f, .05f, 0), new Vector3(.01f, .1f, .14f), Vector3.zero, wood);
        Box(r, "Right", new Vector3(.105f, .05f, 0), new Vector3(.01f, .1f, .14f), Vector3.zero, wood);
        Sph(r, "Mound", new Vector3(0, .085f, 0), new Vector3(.2f, .08f, .13f), Vector3.zero, soil);
        Cyl(r, "Sprout", .03f, .1f, 0, .004f, .05f, green);
        Sph(r, "SproutLeafL", new Vector3(.015f, .155f, 0), new Vector3(.04f, .012f, .025f), new Vector3(0, 0, 25), green);
        Sph(r, "SproutLeafR", new Vector3(.045f, .155f, 0), new Vector3(.04f, .012f, .025f), new Vector3(0, 0, -25), green);
        Box(r, "Peel", new Vector3(-.04f, .122f, .02f), new Vector3(.04f, .004f, .02f), new Vector3(0, 25, 10), peel);
        return r;
    }

    static GameObject BuildBotol()
    {
        var r = NewRoot("Trash_Botol");
        var plastic = Mat("PlasticBlue", "#8EC9E8");
        var cap = Mat("CapBlue", "#2D7DD2");
        var white = Mat("LabelWhite", "#F5F5F5");
        Cyl(r, "Body", 0, 0, 0, .035f, .12f, plastic);
        Sph(r, "Shoulder", new Vector3(0, .125f, 0), new Vector3(.07f, .05f, .07f), Vector3.zero, plastic);
        Cyl(r, "Neck", 0, .14f, 0, .012f, .03f, plastic);
        Cyl(r, "Cap", 0, .17f, 0, .015f, .012f, cap);
        Cyl(r, "Label", 0, .03f, 0, .0356f, .05f, white);
        return r;
    }

    static GameObject BuildKertas()
    {
        var r = NewRoot("Trash_Kertas");
        var paper = Mat("Paper", "#F2F0E6");
        var paper2 = Mat("PaperGrey", "#D8D5C8");
        Box(r, "Sheet", new Vector3(.03f, .003f, .02f), new Vector3(.16f, .003f, .22f), new Vector3(0, 20, 0), paper2);
        Box(r, "Ball1", new Vector3(0, .05f, 0), new Vector3(.08f, .08f, .08f), new Vector3(20, 35, 15), paper);
        Box(r, "Ball2", new Vector3(0, .05f, 0), new Vector3(.08f, .08f, .08f), new Vector3(50, 10, 40), paper2);
        Box(r, "Ball3", new Vector3(0, .05f, 0), new Vector3(.075f, .075f, .075f), new Vector3(10, 60, 70), paper);
        Sph(r, "Core", new Vector3(0, .05f, 0), new Vector3(.08f, .08f, .08f), Vector3.zero, paper);
        return r;
    }

    static GameObject BuildKaleng()
    {
        var r = NewRoot("Trash_Kaleng");
        var silver = Mat("CanSilver", "#B8C0C8", 0.8f, 0.6f);
        var red = Mat("CanRed", "#D63B3B");
        var white = Mat("LabelWhite", "#F5F5F5");
        var dark = Mat("CanTop", "#7C858D", 0.8f, 0.5f);
        Cyl(r, "Body", 0, 0, 0, .04f, .11f, silver);
        Cyl(r, "Label", 0, .02f, 0, .0415f, .065f, red);
        Cyl(r, "Stripe", 0, .045f, 0, .0418f, .012f, white);
        Cyl(r, "Top", 0, .11f, 0, .034f, .008f, dark);
        Box(r, "Tab", new Vector3(0, .119f, 0), new Vector3(.018f, .002f, .01f), Vector3.zero, silver);
        return r;
    }

    static GameObject BuildSisaMakanan()
    {
        var r = NewRoot("Trash_Sisa_Makanan");
        var flesh = Mat("AppleFlesh", "#EEDFA8");
        var skin = Mat("AppleSkin", "#C0392B");
        var brown = Mat("SeedBrown", "#4E2E1A");
        var peel = Mat("BananaPeel", "#F2D13C");
        Sph(r, "Bottom", new Vector3(0, .025f, 0), new Vector3(.07f, .03f, .07f), Vector3.zero, flesh);
        Sph(r, "BottomSkin", new Vector3(0, .015f, 0), new Vector3(.072f, .02f, .072f), Vector3.zero, skin);
        Sph(r, "Top", new Vector3(0, .075f, 0), new Vector3(.07f, .03f, .07f), Vector3.zero, flesh);
        Sph(r, "TopSkin", new Vector3(0, .085f, 0), new Vector3(.072f, .02f, .072f), Vector3.zero, skin);
        Cyl(r, "Core", 0, .02f, 0, .014f, .07f, flesh);
        Box(r, "Stalk", new Vector3(0, .105f, 0), new Vector3(.005f, .03f, .005f), new Vector3(0, 0, 10), brown);
        Sph(r, "Seed", new Vector3(.012f, .05f, .01f), new Vector3(.01f, .015f, .008f), Vector3.zero, brown);
        Box(r, "Peel1", new Vector3(.09f, .008f, .02f), new Vector3(.1f, .008f, .035f), new Vector3(0, 30, 8), peel);
        Box(r, "Peel2", new Vector3(.09f, .008f, -.025f), new Vector3(.09f, .008f, .035f), new Vector3(0, -25, -8), peel);
        return r;
    }

    static GameObject BuildDaunKering()
    {
        var r = NewRoot("Trash_Daun_Kering");
        var c1 = Mat("LeafOrange", "#C9792B");
        var c2 = Mat("LeafBrown", "#8A5A2B");
        var c3 = Mat("LeafYellow", "#D9A93C");
        var stem = Mat("LeafStem", "#5A3A1F");
        Leaf(r, "L1", new Vector3(0, .004f, 0), 10, c1);
        Leaf(r, "L2", new Vector3(.05f, .01f, .03f), 70, c2);
        Leaf(r, "L3", new Vector3(-.04f, .012f, .04f), 120, c3);
        Leaf(r, "L4", new Vector3(-.03f, .018f, -.04f), 40, c1);
        Leaf(r, "L5", new Vector3(.04f, .022f, -.04f), 160, c3);
        Leaf(r, "L6", new Vector3(0, .028f, .005f), 95, c2);
        Box(r, "Twig", new Vector3(.01f, .03f, 0), new Vector3(.1f, .006f, .006f), new Vector3(0, 55, 0), stem);
        return r;
    }

    static void Leaf(GameObject r, string n, Vector3 pos, float yaw, Material m)
    {
        Sph(r, n, pos, new Vector3(.1f, .006f, .06f), new Vector3(Random.Range(-8f, 8f), yaw, Random.Range(-8f, 8f)), m);
    }

    static GameObject BuildGelas()
    {
        var r = NewRoot("Trash_Gelas");
        var cup = Mat("CupBlue", "#9BD4F0");
        var rim = Mat("CupRim", "#D8EEF9");
        var straw = Mat("Straw", "#E04848");
        Cyl(r, "Low", 0, 0, 0, .03f, .04f, cup);
        Cyl(r, "Mid", 0, .04f, 0, .036f, .04f, cup);
        Cyl(r, "High", 0, .08f, 0, .042f, .04f, cup);
        Cyl(r, "Rim", 0, .12f, 0, .046f, .008f, rim);
        Box(r, "Straw", new Vector3(.018f, .11f, 0), new Vector3(.008f, .17f, .008f), new Vector3(0, 0, -14), straw);
        return r;
    }

    static GameObject BuildKardus()
    {
        var r = NewRoot("Trash_Kardus");
        var brown = Mat("Cardboard", "#B98A55");
        var darker = Mat("CardboardDark", "#A07440");
        var tape = Mat("Tape", "#D9C79A");
        Box(r, "Body", new Vector3(0, .06f, 0), new Vector3(.2f, .12f, .15f), Vector3.zero, brown);
        Box(r, "Tape", new Vector3(0, .1215f, 0), new Vector3(.035f, .003f, .15f), Vector3.zero, tape);
        Box(r, "FlapL", new Vector3(-.12f, .14f, 0), new Vector3(.1f, .004f, .15f), new Vector3(0, 0, 35), darker);
        Box(r, "FlapR", new Vector3(.12f, .14f, 0), new Vector3(.1f, .004f, .15f), new Vector3(0, 0, -35), darker);
        Box(r, "Label", new Vector3(0, .06f, .0755f), new Vector3(.06f, .04f, .001f), Vector3.zero, tape);
        return r;
    }

    // --------------------------------------------------------------- HELPERS

    static GameObject NewRoot(string name)
    {
        return new GameObject(name);
    }

    static void Save(GameObject root, string prefabName)
    {
        // Collider otomatis dari ukuran semua bagian
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            var col = root.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = b.size;
        }

        string path = PrefabDir + "/" + prefabName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    static GameObject Prim(PrimitiveType t, GameObject parent, string n, Vector3 pos, Vector3 scale, Vector3 euler, Material m)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = n;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    // Silinder dengan dasar di (x, yBase, z)
    static GameObject Cyl(GameObject p, string n, float x, float yBase, float z, float radius, float height, Material m)
    {
        return Prim(PrimitiveType.Cylinder, p, n, new Vector3(x, yBase + height / 2f, z),
            new Vector3(radius * 2f, height / 2f, radius * 2f), Vector3.zero, m);
    }

    static GameObject Box(GameObject p, string n, Vector3 center, Vector3 size, Vector3 euler, Material m)
    {
        return Prim(PrimitiveType.Cube, p, n, center, size, euler, m);
    }

    static GameObject Sph(GameObject p, string n, Vector3 center, Vector3 diameters, Vector3 euler, Material m)
    {
        return Prim(PrimitiveType.Sphere, p, n, center, diameters, euler, m);
    }

    static Material Mat(string name, string hex, float metallic = 0f, float smoothness = 0.2f)
    {
        if (matCache.TryGetValue(name, out var cached)) return cached;

        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        bool urp = sh != null;
        if (!urp) sh = Shader.Find("Standard");

        var m = new Material(sh) { name = name };
        if (urp)
        {
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
        }
        else
        {
            m.SetColor("_Color", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
        }

        string path = MatDir + "/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        matCache[name] = m;
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        string parent = path.Substring(0, i);
        string leaf = path.Substring(i + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif