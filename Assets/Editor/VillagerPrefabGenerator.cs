using UnityEditor;
using UnityEngine;
using Emberline.Enemies;
using Emberline.Player;
using Emberline.Core;

public class VillagerPrefabGenerator
{
    [MenuItem("Tools/Generate Villager Prefab")]
    public static void Generate()
    {
        string sourcePath = "Assets/Prefabs/Bandit.prefab";
        string targetDir = "Assets/Resources/Prefabs";
        string targetPath = targetDir + "/VillagerPrefab.prefab";

        if (!System.IO.Directory.Exists(targetDir))
        {
            System.IO.Directory.CreateDirectory(targetDir);
        }

        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (sourcePrefab == null)
        {
            Debug.LogError("Source prefab not found at " + sourcePath);
            return;
        }

        // Instantiate to modify
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
        instance.name = "VillagerPrefab";

        // Remove enemy components
        Object.DestroyImmediate(instance.GetComponent<EnemyBrain>());
        Object.DestroyImmediate(instance.GetComponent<CombatController>());
        Object.DestroyImmediate(instance.GetComponent<Health>());
        Object.DestroyImmediate(instance.GetComponent<CapsuleCollider>());
        
        // Remove any other specific enemy scripts
        foreach (var comp in instance.GetComponents<Component>())
        {
            if (comp is Transform) continue;
            if (comp is SkeletalRig) continue;
            if (comp is Animator) continue;
            if (comp is SkinnedMeshRenderer) continue;
            if (comp is MeshFilter) continue;
            if (comp is MeshRenderer) continue;
            
            // Only destroy scripts (MonoBehaviours)
            if (comp is MonoBehaviour)
            {
                Object.DestroyImmediate(comp);
            }
        }

        // Create the new prefab
        PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
        
        // Clean up scene instance
        Object.DestroyImmediate(instance);
        
        Debug.Log("VillagerPrefab generated at " + targetPath);
    }
}
