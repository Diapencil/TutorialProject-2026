using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly.Editor
{
    public sealed class PackagingArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/BurgerAssembly/Packaging";
        private const string CookingScenePath = "Assets/Scenes/BurgerAssembly.unity";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
        }

        [MenuItem("Sheep Sheep Burger/Reimport Packaging Art")]
        public static void ReimportAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Sheep Sheep Burger/Install Packaging Art In Cooking Scene")]
        public static void InstallInCookingScene()
        {
            Scene scene = EditorSceneManager.OpenScene(CookingScenePath, OpenSceneMode.Single);
            BurgerPackagingController packaging = Object.FindFirstObjectByType<BurgerPackagingController>(
                FindObjectsInactive.Include);
            if (packaging == null)
            {
                throw new System.InvalidOperationException("BurgerPackagingController was not found in the cooking scene.");
            }

            packaging.Configure(packaging.transform as RectTransform, null);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CookingScenePath))
            {
                throw new System.InvalidOperationException("The cooking scene could not be saved.");
            }

            Debug.Log("[Packaging] Burger-box art installed in the cooking scene.");
        }

        [MenuItem("Sheep Sheep Burger/Verify Packaging Flow")]
        public static void VerifyPackagingFlow()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var pageObject = new GameObject("PackagingVerificationPage", typeof(RectTransform));
            RectTransform page = pageObject.GetComponent<RectTransform>();
            page.sizeDelta = new Vector2(1920f, 1080f);

            BurgerPackagingController packaging = pageObject.AddComponent<BurgerPackagingController>();
            packaging.Configure(page, null);
            Image openBox = FindImage(page, "BurgerBoxOpenArt");
            if (openBox == null || !openBox.enabled || openBox.sprite == null ||
                openBox.sprite.name != "burger_box_open")
            {
                throw new System.InvalidOperationException("Open burger-box art was not loaded.");
            }

            var burgerObject = new GameObject("VerificationBurger", typeof(RectTransform));
            RectTransform burger = burgerObject.GetComponent<RectTransform>();
            bool packagedEventRaised = false;
            packaging.Packaged += () => packagedEventRaised = true;
            if (!packaging.TryPlaceBurger(burger, Vector2.zero, 54f, -54f, 54f))
            {
                throw new System.InvalidOperationException("The assembled burger was not accepted by the box.");
            }

            Image closedBox = FindImage(page, "BurgerBoxClosingArt");
            if (!packaging.IsPackaged || !packagedEventRaised || burger.gameObject.activeSelf ||
                closedBox == null || !closedBox.enabled || closedBox.sprite == null ||
                closedBox.sprite.name != "burger_box_closed")
            {
                throw new System.InvalidOperationException("Automatic burger-box closing did not complete correctly.");
            }

            if (page.Find("PackagedBurgerArt") != null)
            {
                throw new System.InvalidOperationException("Legacy replacement burger art was generated.");
            }

            Debug.Log("[Packaging] Open, closing, and closed-box flow verified successfully.");
        }

        private static Image FindImage(Transform root, string objectName)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name == objectName)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
