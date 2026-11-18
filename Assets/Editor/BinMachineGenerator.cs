#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Letakkan di Assets/Editor/BinMachineGenerator.cs
/// Menu: Tools > Generate Bins And Machines
/// Hasil: Bin_Organik, Bin_Anorganik, Mesin_Organik, Mesin_Anorganik
/// di Assets/GeneratedTrash/Prefabs. Semua model muat di kotak 1x1x1, dasar di y=0.
/// </summary>
public static class BinMachineGenerator
{
    const string RootDir = "Assets/GeneratedTrash";
    const string MatDir = RootDir + "/Materials";
    const string PrefabDir = RootDir + "/Prefabs";

    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Generate Bins And Machines")]
    public static void GenerateAll()
    {
        matCache.Clear();
        EnsureFolder(RootDir);
        EnsureFolder(MatDir);
        EnsureFolder(PrefabDir);

        Save(BuildBin("Bin_Organik", "#2E8B4A", "#3FAE5E", true), "Bin_Organik");
        Save(BuildBin("Bin_Anorganik", "#2F6FD0", "#4C8DF0", false), "Bin_Anorganik");
        Save(BuildMesinOrganik(), "Mesin_Organik");
        Save(BuildMesinAnorganik(), "Mesin_Anorganik");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bin & mesin dibuat di " + PrefabDir);
    }

    // ------------------------------------------------------------ BINS

    static GameObject BuildBin(string name, string bodyHex, string lidHex, bool organic)
    {
        var r = new GameObject(name);
        var body = Mat(name + "_Body", bodyHex);
        var lid = Mat(name + "_Lid", lidHex);
        var dark = Mat("Bin_Dark", "#2B2F33");
        var panel = Mat("Bin_Panel", "#F3EBD3");

        Box(r, "FootL", new Vector3(-.3f, .04f, 0), new Vector3(.1f, .08f, .6f), Vector3.zero, dark);
        Box(r, "FootR", new Vector3(.3f, .04f, 0), new Vector3(.1f, .08f, .6f), Vector3.zero, dark);
        Box(r, "Body", new Vector3(0, .45f, 0), new Vector3(.8f, .7f, .7f), Vector3.zero, body);
        Box(r, "Lid", new Vector3(0, .835f, 0), new Vector3(.86f, .07f, .76f), Vector3.zero, lid);
        Box(r, "Slot", new Vector3(0, .871f, .05f), new Vector3(.5f, .004f, .12f), Vector3.zero, dark);
        Box(r, "Handle", new Vector3(0, .895f, -.3f), new Vector3(.3f, .05f, .06f), Vector3.zero, dark);
        Box(r, "Stripe", new Vector3(0, .18f, .351f), new Vector3(.8f, .05f, .004f), Vector3.zero, lid);
        Box(r, "Panel", new Vector3(0, .5f, .355f), new Vector3(.42f, .3f, .01f), Vector3.zero, panel);

        if (organic)
        {
            var leaf = Mat("Bin_Leaf", "#3FAE5E");
            var stem = Mat("Bin_LeafStem", "#2E6B3A");
            Sph(r, "Leaf", new Vector3(0, .51f, .364f), new Vector3(.16f, .24f, .012f), new Vector3(0, 0, 25), leaf);
            Box(r, "LeafStem", new Vector3(-.03f, .42f, .364f), new Vector3(.015f, .1f, .01f), new Vector3(0, 0, 25), stem);
        }
        else
        {
            Box(r, "BottleBody", new Vector3(0, .46f, .364f), new Vector3(.1f, .16f, .012f), Vector3.zero, body);
            Box(r, "BottleNeck", new Vector3(0, .575f, .364f), new Vector3(.045f, .07f, .012f), Vector3.zero, body);
            Box(r, "BottleCap", new Vector3(0, .62f, .364f), new Vector3(.06f, .02f, .012f), Vector3.zero, body);
        }
        return r;
    }

    // -------------------------------------------------------- MACHINES

    static GameObject BuildMesinOrganik()
    {
        var r = new GameObject("Mesin_Organik");
        var gray = Mat("Mesin_Gray", "#4A4F55");
        var dark = Mat("Mesin_Dark", "#2B2F33");
        var metal = Mat("Mesin_Metal", "#9AA3AB", 0.8f, 0.6f);
        var green = Mat("Mesin_OrganikGreen", "#3E9B54");
        var darkGreen = Mat("Mesin_OrganikDark", "#2A6E3B");
        var leaf = Mat("Mesin_Leaf", "#6BE88A");
        var brown = Mat("Mesin_Compost", "#6B4423");
        var darkBrown = Mat("Mesin_CompostDark", "#4A2F1D");
        var lightG = Mat("Mesin_LightGreen", "#5BE37D");
        var lightR = Mat("Mesin_LightRed", "#E04848");
        var lightY = Mat("Mesin_LightYellow", "#F2C230");

        Box(r, "Base", new Vector3(0, .05f, 0), new Vector3(.95f, .1f, .95f), Vector3.zero, gray);
        Box(r, "CradleL", new Vector3(-.28f, .22f, 0), new Vector3(.1f, .24f, .7f), Vector3.zero, metal);
        Box(r, "CradleR", new Vector3(.28f, .22f, 0), new Vector3(.1f, .24f, .7f), Vector3.zero, metal);

        // Drum horizontal sepanjang sumbu X
        Prim(PrimitiveType.Cylinder, r, "Drum", new Vector3(0, .45f, 0), new Vector3(.6f, .4f, .6f), new Vector3(0, 0, 90), green);
        Prim(PrimitiveType.Cylinder, r, "RingL", new Vector3(-.2f, .45f, 0), new Vector3(.62f, .02f, .62f), new Vector3(0, 0, 90), darkGreen);
        Prim(PrimitiveType.Cylinder, r, "RingR", new Vector3(.2f, .45f, 0), new Vector3(.62f, .02f, .62f), new Vector3(0, 0, 90), darkGreen);
        Prim(PrimitiveType.Cylinder, r, "CapL", new Vector3(-.4f, .45f, 0), new Vector3(.5f, .012f, .5f), new Vector3(0, 0, 90), darkGreen);
        Prim(PrimitiveType.Cylinder, r, "CapR", new Vector3(.4f, .45f, 0), new Vector3(.5f, .012f, .5f), new Vector3(0, 0, 90), darkGreen);
        Sph(r, "LeafBadge", new Vector3(0, .45f, .305f), new Vector3(.14f, .18f, .02f), new Vector3(0, 0, 25), leaf);

        // Corong di atas
        Box(r, "Hopper", new Vector3(0, .8f, 0), new Vector3(.3f, .12f, .3f), Vector3.zero, gray);
        Box(r, "HopperRim", new Vector3(0, .88f, 0), new Vector3(.4f, .04f, .4f), Vector3.zero, metal);
        Box(r, "HopperInside", new Vector3(0, .901f, 0), new Vector3(.3f, .004f, .3f), Vector3.zero, dark);

        // Panel kontrol
        Box(r, "Panel", new Vector3(.3f, .21f, .47f), new Vector3(.28f, .22f, .06f), Vector3.zero, dark);
        Sph(r, "LightG", new Vector3(.22f, .25f, .505f), new Vector3(.05f, .05f, .05f), Vector3.zero, lightG);
        Sph(r, "LightR", new Vector3(.3f, .25f, .505f), new Vector3(.05f, .05f, .05f), Vector3.zero, lightR);
        Sph(r, "LightY", new Vector3(.38f, .25f, .505f), new Vector3(.05f, .05f, .05f), Vector3.zero, lightY);

        // Baki hasil kompos
        Box(r, "Tray", new Vector3(-.28f, .14f, .4f), new Vector3(.3f, .08f, .16f), Vector3.zero, brown);
        Sph(r, "Compost", new Vector3(-.28f, .19f, .4f), new Vector3(.22f, .08f, .12f), Vector3.zero, darkBrown);
        return r;
    }

    static GameObject BuildMesinAnorganik()
    {
        var r = new GameObject("Mesin_Anorganik");
        var gray = Mat("Mesin_Gray", "#4A4F55");
        var dark = Mat("Mesin_Dark", "#2B2F33");
        var metal = Mat("Mesin_Metal", "#9AA3AB", 0.8f, 0.6f);
        var blue = Mat("Mesin_AnorganikBlue", "#3A6FB5");
        var darkBlue = Mat("Mesin_AnorganikDark", "#2B4F86");
        var glass = Mat("Mesin_Glass", "#BFE3F2", 0f, 0.8f);
        var yellow = Mat("Mesin_Yellow", "#F2C230");
        var lightG = Mat("Mesin_LightGreen", "#5BE37D");
        var lightR = Mat("Mesin_LightRed", "#E04848");
        var bale = Mat("Mesin_Bale", "#8EC9E8");

        Box(r, "Base", new Vector3(0, .05f, 0), new Vector3(.95f, .1f, .95f), Vector3.zero, gray);
        Box(r, "Body", new Vector3(0, .5f, 0), new Vector3(.8f, .8f, .7f), Vector3.zero, blue);
        Box(r, "TopPlate", new Vector3(0, .92f, 0), new Vector3(.5f, .04f, .4f), Vector3.zero, darkBlue);
        Box(r, "Slot", new Vector3(0, .941f, 0), new Vector3(.36f, .004f, .24f), Vector3.zero, dark);
        Cyl(r, "Pipe", .3f, .9f, -.15f, .05f, .1f, metal);

        // Jendela + roda gigi
        Box(r, "Window", new Vector3(0, .62f, .352f), new Vector3(.5f, .3f, .01f), Vector3.zero, glass);
        Prim(PrimitiveType.Cylinder, r, "Gear", new Vector3(0, .62f, .36f), new Vector3(.2f, .006f, .2f), new Vector3(90, 0, 0), yellow);
        Prim(PrimitiveType.Cylinder, r, "GearHub", new Vector3(0, .62f, .366f), new Vector3(.07f, .006f, .07f), new Vector3(90, 0, 0), gray);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            float rad = a * Mathf.Deg2Rad;
            Box(r, "Tooth" + i, new Vector3(Mathf.Cos(rad) * .115f, .62f + Mathf.Sin(rad) * .115f, .364f),
                new Vector3(.05f, .05f, .01f), new Vector3(0, 0, a), yellow);
        }

        // Panel kontrol + strip bahaya
        Box(r, "Panel", new Vector3(0, .3f, .352f), new Vector3(.5f, .14f, .01f), Vector3.zero, dark);
        Sph(r, "LightG", new Vector3(-.12f, .3f, .36f), new Vector3(.05f, .05f, .05f), Vector3.zero, lightG);
        Sph(r, "LightR", new Vector3(0, .3f, .36f), new Vector3(.05f, .05f, .05f), Vector3.zero, lightR);
        Sph(r, "LightY", new Vector3(.12f, .3f, .36f), new Vector3(.05f, .05f, .05f), Vector3.zero, yellow);
        Box(r, "Hazard", new Vector3(0, .14f, .352f), new Vector3(.8f, .04f, .004f), Vector3.zero, yellow);

        // Baki hasil daur ulang
        Box(r, "Tray", new Vector3(0, .15f, .425f), new Vector3(.4f, .04f, .15f), Vector3.zero, metal);
        Box(r, "Bale", new Vector3(0, .2f, .425f), new Vector3(.12f, .06f, .1f), new Vector3(0, 15, 0), bale);
        return r;
    }

    // --------------------------------------------------------- HELPERS

    static void Save(GameObject root, string prefabName)
    {
        PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + prefabName + ".prefab");
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
