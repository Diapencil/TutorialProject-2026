// 상점 상품 데이터, 플레이스홀더 아트, 씬 참조와 카운터 장식 레이어를 기존 레이아웃을 보존하며 설치한다.
using System;
using System.IO;
using SheepSheepBurger.Core;
using SheepSheepBurger.Counter;
using SheepSheepBurger.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SheepSheepBurger.EditorTools
{
    public static class ShopContentInstaller
    {
        private const string ShopScenePath = "Assets/Scenes/ShopScene.unity";
        private const string CounterScenePath = "Assets/Scenes/Counter.unity";
        private const string PlaceholderFolder = "Assets/GameAssets/Shop";
        private const string PlaceholderPath = PlaceholderFolder + "/ShopPlaceholderSquare.png";
        private const string CatalogFolder = "Assets/Resources/Shop";
        private const string CatalogPath = CatalogFolder + "/ShopCatalog.asset";

        private static readonly string[] IngredientPaths =
        {
            "Assets/Data/Ingredients/Bacon.asset",
            "Assets/Data/Ingredients/egg.asset",
            "Assets/Data/Ingredients/Pickle.asset",
            "Assets/Data/Ingredients/Jalapeno.asset"
        };

        private static readonly string[] AllIngredientPaths =
        {
            "Assets/Data/Ingredients/BunBottom.asset",
            "Assets/Data/Ingredients/BunTop.asset",
            "Assets/Data/Ingredients/Patty.asset",
            "Assets/Data/Ingredients/Cheese.asset",
            "Assets/Data/Ingredients/Pickle.asset",
            "Assets/Data/Ingredients/Ketchup.asset",
            "Assets/Data/Ingredients/Mustard.asset",
            "Assets/Data/Ingredients/Jalapeno.asset",
            "Assets/Data/Ingredients/onion.asset",
            "Assets/Data/Ingredients/Lettuce.asset",
            "Assets/Data/Ingredients/Tomato.asset",
            "Assets/Data/Ingredients/egg.asset",
            "Assets/Data/Ingredients/Bacon.asset"
        };

        private static readonly string[] UpgradePaths =
        {
            "Assets/Data/Shop/Upgrades/Fryer.asset",
            "Assets/Data/Shop/Upgrades/Grill.asset"
        };

        private static readonly string[] DecorationPaths =
        {
            "Assets/Data/Shop/Decorations/FlowerPot.asset",
            "Assets/Data/Shop/Decorations/Banner.asset",
            "Assets/Data/Shop/Decorations/Figure.asset",
            "Assets/Data/Shop/Decorations/ManekiNeko.asset"
        };

        private static readonly Vector2[] DecorationPositions =
        {
            new Vector2(-300f, -205f),
            new Vector2(-95f, 220f),
            new Vector2(150f, -205f),
            new Vector2(300f, -205f)
        };

        [MenuItem("SheepSheep/Install Shop Content And Progress Links")]
        public static void Install()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Sprite placeholder = EnsurePlaceholderSprite();
                IngredientData[] ingredients = ConfigureIngredients(placeholder);
                UpgradeData[] upgrades = ConfigureUpgrades(placeholder);
                DecorationData[] decorations = ConfigureDecorations(placeholder);
                ShopCatalog catalog = EnsureCatalog(ingredients, upgrades, decorations);

                RepairShopScene(catalog, ingredients, upgrades, decorations);
                RepairCounterScene(decorations);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[ShopContentInstaller] 상점 데이터와 게임 진행 연결 설치를 완료했습니다.");
            }
            finally
            {
                if (!Application.isBatchMode && previousSetup != null && previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }
        }

        private static Sprite EnsurePlaceholderSprite()
        {
            EnsureFolder(PlaceholderFolder);

            if (!File.Exists(PlaceholderPath))
            {
                const int size = 64;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color32 fill = new Color32(180, 205, 171, 255);
                Color32 border = new Color32(64, 92, 69, 255);
                Color32[] pixels = new Color32[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool isBorder = x < 4 || x >= size - 4 || y < 4 || y >= size - 4;
                        pixels[y * size + x] = isBorder ? border : fill;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(PlaceholderPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(PlaceholderPath, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(PlaceholderPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath);
        }

        private static IngredientData[] ConfigureIngredients(Sprite placeholder)
        {
            IngredientData[] allIngredients = LoadAssets<IngredientData>(AllIngredientPaths);
            for (int i = 0; i < allIngredients.Length; i++)
            {
                IngredientData data = allIngredients[i];

                bool isLockedShopIngredient = data.id == 13 || data.id == 12 || data.id == 8;
                data.isDefaultUnlocked = !isLockedShopIngredient;
                data.unlockCost = data.id == 13 || data.id == 12
                    ? 1000
                    : data.id == 8
                        ? 800
                        : 0;
                data.costPerUse = data.grillable ? 3 : 2;
                if (data.icon == null)
                {
                    data.icon = placeholder;
                }
                EditorUtility.SetDirty(data);
            }

            IngredientData[] ingredients = LoadAssets<IngredientData>(IngredientPaths);
            ConfigureIngredient(ingredients[0], false, 1000, 3, placeholder);
            ConfigureIngredient(ingredients[1], false, 1000, 3, placeholder);
            ConfigureIngredient(ingredients[2], true, 0, 2, placeholder);
            ConfigureIngredient(ingredients[3], false, 800, 2, placeholder);
            return ingredients;
        }

        private static void ConfigureIngredient(
            IngredientData data,
            bool defaultUnlocked,
            int unlockCost,
            int costPerUse,
            Sprite placeholder)
        {
            data.isDefaultUnlocked = defaultUnlocked;
            data.unlockCost = unlockCost;
            data.costPerUse = costPerUse;
            if (data.icon == null)
            {
                data.icon = placeholder;
            }
            EditorUtility.SetDirty(data);
        }

        private static UpgradeData[] ConfigureUpgrades(Sprite placeholder)
        {
            UpgradeData[] upgrades = LoadAssets<UpgradeData>(UpgradePaths);
            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i].icon == null)
                {
                    upgrades[i].icon = placeholder;
                }
                EditorUtility.SetDirty(upgrades[i]);
            }
            return upgrades;
        }

        private static DecorationData[] ConfigureDecorations(Sprite placeholder)
        {
            DecorationData[] decorations = LoadAssets<DecorationData>(DecorationPaths);
            for (int i = 0; i < decorations.Length; i++)
            {
                if (decorations[i].sprite == null)
                {
                    decorations[i].sprite = placeholder;
                }
                if (decorations[i].counterPosition == Vector2.zero)
                {
                    decorations[i].counterPosition = DecorationPositions[i];
                }
                EditorUtility.SetDirty(decorations[i]);
            }
            return decorations;
        }

        private static ShopCatalog EnsureCatalog(
            IngredientData[] ingredients,
            UpgradeData[] upgrades,
            DecorationData[] decorations)
        {
            EnsureFolder(CatalogFolder);
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShopCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SetObjectArray(serialized.FindProperty("ingredients"), ingredients);
            SetObjectArray(serialized.FindProperty("upgrades"), upgrades);
            SetObjectArray(serialized.FindProperty("decorations"), decorations);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void RepairShopScene(
            ShopCatalog catalog,
            IngredientData[] ingredients,
            UpgradeData[] upgrades,
            DecorationData[] decorations)
        {
            Scene scene = EditorSceneManager.OpenScene(ShopScenePath, OpenSceneMode.Single);
            ShopManager manager = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                throw new InvalidOperationException("ShopScene에서 ShopManager를 찾지 못했습니다.");
            }

            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            SetObjectArray(serialized.FindProperty("allIngredients"), ingredients);
            SetObjectArray(serialized.FindProperty("allUpgrades"), upgrades);
            SetObjectArray(serialized.FindProperty("allDecorations"), decorations);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RepairCounterScene(DecorationData[] decorations)
        {
            Scene scene = EditorSceneManager.OpenScene(CounterScenePath, OpenSceneMode.Single);
            // 씬을 Single 모드로 교체하면 이전 씬에서 사용하던 애셋 래퍼가 언로드될 수 있다.
            decorations = LoadAssets<DecorationData>(DecorationPaths);
            Canvas canvas = FindCounterCanvas();
            if (canvas == null)
            {
                throw new InvalidOperationException("Counter 씬에서 Canvas를 찾지 못했습니다.");
            }

            Transform existing = canvas.transform.Find("PurchasedDecorations");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject("PurchasedDecorations", typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Transform counterFront = canvas.transform.Find("CounterFront");
            if (counterFront != null)
            {
                MoveImmediatelyAfter(rootRect, counterFront);
            }

            CounterDecorationPresenter presenter = root.GetComponent<CounterDecorationPresenter>();
            if (presenter == null)
            {
                presenter = root.AddComponent<CounterDecorationPresenter>();
            }

            GameObject[] visuals = new GameObject[decorations.Length];
            for (int i = 0; i < decorations.Length; i++)
            {
                DecorationData decoration = decorations[i];
                string objectName = $"Decoration_{decoration.id}_{decoration.name}";
                Transform child = rootRect.Find(objectName);
                bool created = child == null;
                GameObject visual = created
                    ? new GameObject(objectName, typeof(RectTransform), typeof(Image))
                    : child.gameObject;
                RectTransform rect = visual.GetComponent<RectTransform>();
                rect.SetParent(rootRect, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                if (created)
                {
                    rect.anchoredPosition = decoration.counterPosition;
                }
                rect.sizeDelta = new Vector2(176f, 176f);

                Image image = visual.GetComponent<Image>();
                image.sprite = decoration.sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
                visual.SetActive(true);
                visuals[i] = visual;
            }

            SerializedObject serialized = new SerializedObject(presenter);
            SerializedProperty entries = serialized.FindProperty("decorationVisuals");
            entries.arraySize = decorations.Length;
            for (int i = 0; i < decorations.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("decorationId").intValue = decorations[i].id;
                entry.FindPropertyRelative("visual").objectReferenceValue = visuals[i];
            }
            serialized.FindProperty("defaultVisualSize").vector2Value = new Vector2(176f, 176f);
            serialized.FindProperty("placementPreviewSize").vector2Value = new Vector2(192f, 192f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void MoveImmediatelyAfter(Transform moved, Transform reference)
        {
            if (moved == null || reference == null || moved.parent != reference.parent)
            {
                return;
            }

            int currentIndex = moved.GetSiblingIndex();
            int referenceIndex = reference.GetSiblingIndex();
            int targetIndex = currentIndex < referenceIndex
                ? referenceIndex
                : referenceIndex + 1;
            moved.SetSiblingIndex(targetIndex);
        }

        private static Canvas FindCounterCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].name == "Canvas")
                {
                    return canvases[i];
                }
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static T[] LoadAssets<T>(string[] paths) where T : Object
        {
            T[] result = new T[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
                if (result[i] == null)
                {
                    throw new FileNotFoundException($"필요한 애셋을 찾지 못했습니다: {paths[i]}");
                }
            }
            return result;
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }
                current = next;
            }
        }
    }
}
