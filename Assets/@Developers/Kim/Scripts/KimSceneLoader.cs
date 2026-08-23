using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// Kim 프로토타입 씬 이동을 담당합니다.
// 붙이는 오브젝트: 없음. CounterUI와 CookingUI에서 코드로 호출합니다.
public static class KimSceneLoader
{
    public const string CounterScenePath = "Assets/@Developers/Kim/Scenes/CounterScene.unity";
    public const string CookingScenePath = "Assets/@Developers/Kim/Scenes/CookingScene.unity";

    // 에디터에서는 Build Settings를 건드리지 않고 경로로 씬을 엽니다.
    public static void LoadCounterScene()
    {
        LoadScene(CounterScenePath, "CounterScene");
    }

    public static void LoadCookingScene()
    {
        LoadScene(CookingScenePath, "CookingScene");
    }

    private static void LoadScene(string editorPath, string sceneName)
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(editorPath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(sceneName);
#endif
    }
}
