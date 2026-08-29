using SheepSheepBurger.Results;
using UnityEditor;
using UnityEngine;

namespace SheepSheepBurger.EditorTools
{
    public static class DayResultLayerPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/DayResultLayer.prefab";

        [MenuItem("SheepSheep/Build Day Result Layer Prefab")]
        public static void BuildPrefab()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, "Resources/UI"));
            DayResultLayerFontBaker.EnsureResultLayerFont();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject root = prefab != null
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("DayResultLayer", typeof(RectTransform));

            try
            {
                root.name = "DayResultLayer";

                if (root.GetComponent<RectTransform>() == null)
                {
                    root.AddComponent<RectTransform>();
                }

                DayResultLayerController controller = root.GetComponent<DayResultLayerController>();
                controller = controller != null ? controller : root.AddComponent<DayResultLayerController>();
                controller.ApplyPolishedDesignDefaults();
                controller.RebuildRoughLayout();

                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[DayResultLayerPrefabBuilder] Built {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
