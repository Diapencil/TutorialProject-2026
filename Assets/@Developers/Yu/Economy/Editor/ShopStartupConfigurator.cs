using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SheepSheepBurger.Economy.Editor
{
    [InitializeOnLoad]
    public static class ShopStartupConfigurator
    {
        private const string ShopScenePath = "Assets/@Developers/ChoiHJ/Economy/Scenes/ShopPrototype.unity";

        static ShopStartupConfigurator()
        {
            EditorApplication.delayCall += ConfigureShopStartup;
        }

        [MenuItem("Sheep Sheep Burger/Set Shop As Startup Scene")]
        public static void ConfigureShopStartup()
        {
            SceneAsset shopScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopScenePath);
            if (shopScene == null)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = shopScene;
            MoveShopSceneToBuildStart();
        }

        [MenuItem("Sheep Sheep Burger/Open Shop Startup Scene")]
        public static void ConfigureAndOpenShopScene()
        {
            ConfigureShopStartup();
            EditorSceneManager.OpenScene(ShopScenePath, OpenSceneMode.Single);
        }

        private static void MoveShopSceneToBuildStart()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ShopScenePath, true)
            };

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int index = 0; index < existingScenes.Length; index++)
            {
                EditorBuildSettingsScene scene = existingScenes[index];
                if (scene.path != ShopScenePath)
                {
                    scenes.Add(scene);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
