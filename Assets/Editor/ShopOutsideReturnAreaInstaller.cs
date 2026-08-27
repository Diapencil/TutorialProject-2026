using System.Collections.Generic;
using System.Linq;
using SheepSheepBurger.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.EditorTools
{
    public static class ShopOutsideReturnAreaInstaller
    {
        private const string ShopScenePath = "Assets/Scenes/ShopScene.unity";
        private const string CounterScenePath = "Assets/Scenes/Counter.unity";
        private const string ReturnAreaObjectName = "ShopOutsideReturnArea";
        private const string MarkerObjectName = "Square";

        private static readonly Rect DefaultSafeViewportRect = new Rect(0f, 0.15098f, 1f, 0.7169f);

        [MenuItem("SheepSheep/Wire Shop Outside Return Area")]
        public static void WireShopOutsideReturnArea()
        {
            Scene scene = EditorSceneManager.OpenScene(ShopScenePath, OpenSceneMode.Single);

            Rect safeRect = ResolveSafeViewportRect();
            ShopOutsideReturnArea returnArea = FindOrCreateReturnArea();

            SerializedObject serialized = new SerializedObject(returnArea);
            serialized.FindProperty("counterSceneName").stringValue = "Counter";
            serialized.FindProperty("safeViewportRect").rectValue = safeRect;
            serialized.FindProperty("requirePointerUpOutside").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            RemoveMarkerIfPresent();
            EnsureSceneInBuildSettings(ShopScenePath);
            EnsureSceneInBuildSettings(CounterScenePath);

            EditorUtility.SetDirty(returnArea);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ShopOutsideReturnAreaInstaller] Shop outside return area is wired. " +
                $"safeViewportRect=(x:{safeRect.x:F4}, y:{safeRect.y:F4}, w:{safeRect.width:F4}, h:{safeRect.height:F4})");
            EditorGUIUtility.PingObject(returnArea);
        }

        private static Rect ResolveSafeViewportRect()
        {
            if (TryGetMarkerViewportRect(out Rect markerRect))
            {
                return markerRect;
            }

            Debug.LogWarning(
                $"[ShopOutsideReturnAreaInstaller] Marker '{MarkerObjectName}' was not found. " +
                "Using the saved default safe area.");
            return DefaultSafeViewportRect;
        }

        private static bool TryGetMarkerViewportRect(out Rect rect)
        {
            rect = default;

            GameObject marker = FindSceneObjectByName(MarkerObjectName);
            Camera camera = Camera.main != null
                ? Camera.main
                : Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

            if (marker == null || camera == null)
            {
                return false;
            }

            SpriteRenderer spriteRenderer = marker.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                rect = ClampViewportRect(WorldBoundsToViewportRect(camera, spriteRenderer.bounds));
                return rect.width > 0f && rect.height > 0f;
            }

            RectTransform rectTransform = marker.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                rect = ClampViewportRect(WorldCornersToViewportRect(camera, corners));
                return rect.width > 0f && rect.height > 0f;
            }

            return false;
        }

        private static Rect WorldBoundsToViewportRect(Camera camera, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z)
            };

            return WorldCornersToViewportRect(camera, corners);
        }

        private static Rect WorldCornersToViewportRect(Camera camera, Vector3[] corners)
        {
            float xMin = float.PositiveInfinity;
            float yMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMax = float.NegativeInfinity;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 viewportPoint = camera.WorldToViewportPoint(corners[i]);
                xMin = Mathf.Min(xMin, viewportPoint.x);
                yMin = Mathf.Min(yMin, viewportPoint.y);
                xMax = Mathf.Max(xMax, viewportPoint.x);
                yMax = Mathf.Max(yMax, viewportPoint.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect ClampViewportRect(Rect rect)
        {
            float xMin = Mathf.Clamp01(Mathf.Min(rect.xMin, rect.xMax));
            float xMax = Mathf.Clamp01(Mathf.Max(rect.xMin, rect.xMax));
            float yMin = Mathf.Clamp01(Mathf.Min(rect.yMin, rect.yMax));
            float yMax = Mathf.Clamp01(Mathf.Max(rect.yMin, rect.yMax));

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static ShopOutsideReturnArea FindOrCreateReturnArea()
        {
            GameObject returnAreaObject = FindSceneObjectByName(ReturnAreaObjectName);

            if (returnAreaObject == null)
            {
                returnAreaObject = new GameObject(ReturnAreaObjectName);
            }

            ShopOutsideReturnArea returnArea = returnAreaObject.GetComponent<ShopOutsideReturnArea>();

            if (returnArea == null)
            {
                returnArea = returnAreaObject.AddComponent<ShopOutsideReturnArea>();
            }

            return returnArea;
        }

        private static void RemoveMarkerIfPresent()
        {
            GameObject marker = FindSceneObjectByName(MarkerObjectName);

            if (marker != null)
            {
                Object.DestroyImmediate(marker);
            }
        }

        private static GameObject FindSceneObjectByName(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(go => go.name == objectName && go.scene == activeScene);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath))
            {
                Debug.LogWarning($"[ShopOutsideReturnAreaInstaller] Scene not found: {scenePath}");
                return;
            }

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
