using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 프로토타입 기본 구성 검증용 에디터 유틸리티입니다.
// 붙이는 오브젝트: 없음. 배치 모드에서 -executeMethod PrototypeValidator.ValidateProject 로 실행합니다.
public static class PrototypeValidator
{
    private const string RootPath = "Assets/@Developers/Kim";
    private const string DataPath = RootPath + "/Data/Ingredients";
    private const string CounterScenePath = RootPath + "/Scenes/CounterScene.unity";
    private const string CookingScenePath = RootPath + "/Scenes/CookingScene.unity";

    public static void ValidateProject()
    {
        ValidateSceneComponent<CounterUI>(CounterScenePath, "CounterUI");
        ValidateSceneComponent<CookingUI>(CookingScenePath, "CookingUI");
        ValidateIngredientAssets();
        Debug.Log("Burger demo validation passed.");
    }

    private static void ValidateSceneComponent<T>(string scenePath, string objectName) where T : Component
    {
        EditorSceneManager.OpenScene(scenePath);
        GameObject target = GameObject.Find(objectName);

        if (target == null)
        {
            throw new System.Exception(scenePath + "에 " + objectName + " 오브젝트가 없습니다.");
        }

        if (target.GetComponent<T>() == null)
        {
            throw new System.Exception(objectName + "에 " + typeof(T).Name + " 컴포넌트가 없습니다.");
        }
    }

    private static void ValidateIngredientAssets()
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
            string path = DataPath + "/" + type + ".asset";
            Ingredient ingredient = AssetDatabase.LoadAssetAtPath<Ingredient>(path);

            if (ingredient == null)
            {
                throw new System.Exception(path + " 재료 에셋이 없습니다.");
            }

            if (ingredient.id != type)
            {
                throw new System.Exception(path + " 재료 id가 잘못되었습니다.");
            }
        }
    }

}
