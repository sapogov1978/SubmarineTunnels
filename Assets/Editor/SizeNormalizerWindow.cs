using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SizeNormalizerWindow : EditorWindow
{
    private const string RootArtPath = "Assets/_Game/Art/Sprites";
    private const string RootGamePath = "Assets/_Game";

    private int targetPPU = 100;
    private bool includeScenes = false;
    private bool normalizeAllTransforms = false;
    private bool normalizeRelevantTransforms = true;

    [MenuItem("Tools/Size Normalizer")]
    private static void Open()
    {
        GetWindow<SizeNormalizerWindow>("Size Normalizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Standardize Sizes", EditorStyles.boldLabel);
        targetPPU = EditorGUILayout.IntField("Target PPU", targetPPU);
        includeScenes = EditorGUILayout.Toggle("Include Scenes", includeScenes);
        normalizeAllTransforms = EditorGUILayout.Toggle("Normalize All Transforms", normalizeAllTransforms);
        normalizeRelevantTransforms = EditorGUILayout.Toggle("Normalize Relevant Transforms", normalizeRelevantTransforms);

        if (normalizeAllTransforms)
        {
            normalizeRelevantTransforms = false;
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Report"))
        {
            RunReport();
        }

        if (GUILayout.Button("Apply"))
        {
            if (EditorUtility.DisplayDialog(
                    "Normalize Sizes",
                    "This will change sprite PPU and reset scales in prefabs" +
                    (includeScenes ? " and scenes" : "") +
                    ". Continue?",
                    "Apply",
                    "Cancel"))
            {
                ApplyChanges();
            }
        }
    }

    private void RunReport()
    {
        int ppuMismatchCount = 0;
        foreach (string path in GetTexturePaths(RootArtPath))
        {
            if (TryGetTextureImporter(path, out TextureImporter importer))
            {
                if (Mathf.Abs(importer.spritePixelsPerUnit - targetPPU) > 0.01f)
                {
                    ppuMismatchCount++;
                    Debug.Log($"[SizeNormalizer] PPU mismatch: {path} -> {importer.spritePixelsPerUnit}");
                }
            }
        }

        int prefabScaleCount = ReportScaleIssuesInPrefabs();
        int sceneScaleCount = includeScenes ? ReportScaleIssuesInScenes() : 0;

        Debug.Log($"[SizeNormalizer] Report: PPU mismatches={ppuMismatchCount}, prefab scale issues={prefabScaleCount}, scene scale issues={sceneScaleCount}");
    }

    private void ApplyChanges()
    {
        int ppuChanged = 0;
        foreach (string path in GetTexturePaths(RootArtPath))
        {
            if (TryGetTextureImporter(path, out TextureImporter importer))
            {
                if (Mathf.Abs(importer.spritePixelsPerUnit - targetPPU) > 0.01f)
                {
                    importer.spritePixelsPerUnit = targetPPU;
                    importer.SaveAndReimport();
                    ppuChanged++;
                }
            }
        }

        int prefabsChanged = NormalizeScalesInPrefabs();
        int scenesChanged = includeScenes ? NormalizeScalesInScenes() : 0;

        Debug.Log($"[SizeNormalizer] Apply complete: PPU changed={ppuChanged}, prefabs changed={prefabsChanged}, scenes changed={scenesChanged}");
    }

    private static IEnumerable<string> GetTexturePaths(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            yield break;

        foreach (string file in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd")
            {
                yield return file.Replace("\\", "/");
            }
        }
    }

    private static bool TryGetTextureImporter(string path, out TextureImporter importer)
    {
        importer = AssetImporter.GetAtPath(path) as TextureImporter;
        return importer != null;
    }

    private int ReportScaleIssuesInPrefabs()
    {
        int count = 0;
        string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { RootGamePath });
        foreach (string guid in prefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool hasIssue = HasScaleIssues(root.transform);
            if (hasIssue)
            {
                count++;
                Debug.Log($"[SizeNormalizer] Prefab scale issue: {path}");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        return count;
    }

    private int ReportScaleIssuesInScenes()
    {
        int count = 0;
        string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes", RootGamePath });
        foreach (string guid in scenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            bool hasIssue = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (HasScaleIssues(root.transform))
                {
                    hasIssue = true;
                    break;
                }
            }

            if (hasIssue)
            {
                count++;
                Debug.Log($"[SizeNormalizer] Scene scale issue: {path}");
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        return count;
    }

    private int NormalizeScalesInPrefabs()
    {
        int changed = 0;
        string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { RootGamePath });
        foreach (string guid in prefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (NormalizeScales(root.transform))
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        return changed;
    }

    private int NormalizeScalesInScenes()
    {
        int changed = 0;
        string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes", RootGamePath });
        foreach (string guid in scenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (NormalizeScales(root.transform))
                {
                    dirty = true;
                }
            }

            if (dirty)
            {
                EditorSceneManager.SaveScene(scene);
                changed++;
            }
        }

        return changed;
    }

    private bool HasScaleIssues(Transform root)
    {
        bool issue = false;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!ShouldNormalizeTransform(t))
                continue;

            if (t.localScale != Vector3.one)
            {
                issue = true;
                break;
            }
        }

        return issue;
    }

    private bool NormalizeScales(Transform root)
    {
        bool changed = false;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!ShouldNormalizeTransform(t))
                continue;

            if (t.localScale != Vector3.one)
            {
                t.localScale = Vector3.one;
                changed = true;
            }
        }

        return changed;
    }

    private bool ShouldNormalizeTransform(Transform t)
    {
        if (normalizeAllTransforms)
            return true;

        if (!normalizeRelevantTransforms)
            return false;

        if (t.GetComponent<SpriteRenderer>() != null || t.GetComponent<Collider2D>() != null)
            return true;

        foreach (Transform child in t)
        {
            if (child.GetComponentInChildren<SpriteRenderer>(true) != null ||
                child.GetComponentInChildren<Collider2D>(true) != null)
                return true;
        }

        return false;
    }
}
