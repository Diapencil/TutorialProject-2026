using SheepSheepBurger.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.EditorTools
{
    /// <summary>첫 조리 튜토리얼을 반복 검증하기 위한 에디터 전용 메뉴입니다.</summary>
    public static class FirstCookingTutorialEditor
    {
        private const string ResetMenuPath = "Sheep Sheep Burger/튜토리얼/첫 요리 튜토리얼 다시 보기";

        [MenuItem(ResetMenuPath)]
        private static void ResetFirstCookingTutorial()
        {
            FirstCookingTutorial.ResetCompletionForDevelopment();
            Debug.Log("[Tutorial] 첫 요리 튜토리얼 완료 기록을 초기화했습니다.");

            if (Application.isPlaying)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        [MenuItem(ResetMenuPath, true)]
        private static bool ValidateResetFirstCookingTutorial()
        {
            return !EditorApplication.isCompiling;
        }
    }
}
