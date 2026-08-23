using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 프로토타입 씬과 데이터 에셋을 자동 생성하는 에디터 유틸리티입니다.
// 붙이는 오브젝트: 없음. 배치 모드 또는 메뉴에서 실행합니다.
public static class PrototypeSceneBuilder
{
    private const string RootPath = "Assets/@Developers/Kim";
    private const string DataPath = RootPath + "/Data/Ingredients";
    private const string CounterScenePath = RootPath + "/Scenes/CounterScene.unity";
    private const string CookingScenePath = RootPath + "/Scenes/CookingScene.unity";

    [MenuItem("Burger Demo/Rebuild Prototype Scenes")]
    public static void BuildPrototypeScenes()
    {
        Directory.CreateDirectory(RootPath + "/Scenes");
        Directory.CreateDirectory(DataPath);

        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        CreateIngredientAssets();
        CreateCounterScene();
        CreateCookingScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Burger demo prototype scenes rebuilt.");
    }

    private static void CreateIngredientAssets()
    {
        IngredientType[] allTypes =
        {
            IngredientType.Bun,
            IngredientType.Patty,
            IngredientType.Cheese,
            IngredientType.Lettuce,
            IngredientType.Tomato,
            IngredientType.Onion,
            IngredientType.Pickle,
            IngredientType.FishFillet,
            IngredientType.BunTop
        };

        for (int i = 0; i < allTypes.Length; i++)
        {
            IngredientType type = allTypes[i];
            IngredientDefinition definition = IngredientLibrary.Get(type);
            string path = DataPath + "/" + type + ".asset";
            Ingredient asset = AssetDatabase.LoadAssetAtPath<Ingredient>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<Ingredient>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.id = definition.Id;
            asset.displayName = definition.DisplayName;
            asset.color = definition.Color;
            EditorUtility.SetDirty(asset);
        }
    }

    private static void CreateCounterScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CounterScene";
        CreateCamera(new Color(0.12f, 0.11f, 0.10f));

        GameObject uiObject = new GameObject("CounterUI");
        uiObject.AddComponent<CounterUI>();

        EditorSceneManager.SaveScene(scene, CounterScenePath);
    }

    private static void CreateCookingScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CookingScene";
        CreateCamera(new Color(0.11f, 0.12f, 0.13f));

        GameObject uiObject = new GameObject("CookingUI");
        uiObject.AddComponent<CookingUI>();

        EditorSceneManager.SaveScene(scene, CookingScenePath);
    }

    private static void CreateCamera(Color background)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

}
