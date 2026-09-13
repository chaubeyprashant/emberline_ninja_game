using UnityEngine;
using UnityEditor;

public class GenerateKenneyPrefabs
{
    [MenuItem("Emberline/Generate Props Prefabs")]
    public static void GeneratePrefabs()
    {
        CreateHousePrefab();
        CreateRuinPrefab();
        CreateShrinePrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[Emberline] Finished generating house, ruin, and shrine prefabs.");
    }

    private static void CreateHousePrefab()
    {
        string savePath = "Assets/Resources/Props/Dressing/house.prefab";
        if (System.IO.File.Exists(savePath)) {
            AssetDatabase.DeleteAsset(savePath);
        }

        GameObject root = new GameObject("house");

        // Load Hexagon house model
        GameObject structure = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Hexagon/building_home_A.fbx");

        if (structure != null)
        {
            GameObject sInst = (GameObject)PrefabUtility.InstantiatePrefab(structure, root.transform);
            sInst.transform.localPosition = Vector3.zero;
        }
        else Debug.LogError("building_home_A.fbx not found");

        // Scale up to fit the original size
        root.transform.localScale = new Vector3(3f, 3f, 3f);

        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
    }

    private static void CreateRuinPrefab()
    {
        string savePath = "Assets/Resources/Props/Dressing/ruin.prefab";
        if (System.IO.File.Exists(savePath)) {
            AssetDatabase.DeleteAsset(savePath);
        }

        GameObject root = new GameObject("ruin");

        // Load Hexagon destroyed model
        GameObject structure = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Hexagon/building_destroyed.fbx");

        if (structure != null)
        {
            GameObject sInst = (GameObject)PrefabUtility.InstantiatePrefab(structure, root.transform);
            sInst.transform.localPosition = Vector3.zero;
        }

        root.transform.localScale = new Vector3(3f, 3f, 3f);

        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
    }

    private static void CreateShrinePrefab()
    {
        string savePath = "Assets/Resources/Props/Dressing/shrine.prefab";
        if (System.IO.File.Exists(savePath)) {
            Debug.Log($"[Emberline] Prefab already exists: {savePath}");
            return;
        }

        GameObject root = new GameObject("shrine");

        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Kenney/nature/stone_tallE.fbx");
        GameObject campfire = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Kenney/nature/campfire_stones.fbx");

        if (stone != null)
        {
            GameObject sInst = (GameObject)PrefabUtility.InstantiatePrefab(stone, root.transform);
            sInst.transform.localPosition = Vector3.zero;
            sInst.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        }

        if (campfire != null)
        {
            GameObject cInst = (GameObject)PrefabUtility.InstantiatePrefab(campfire, root.transform);
            cInst.transform.localPosition = new Vector3(0, 0, 1.5f); // Front of the stone
            cInst.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
    }
}
