// 조리 씬이 로드될 때 카운터 복귀 브릿지가 없으면 런타임에 자동으로 붙인다.
using SheepSheepBurger.BurgerAssembly;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.Counter
{
    /// <summary>
    /// 씬 파일에 CounterReturnBridge가 빠져 있어도 포장 후 카운터로 돌아가게 만든다.
    /// 씬을 다시 굽거나 경로가 바뀌어도 이 경로는 끊기지 않는다.
    /// 씬에 이미 브릿지가 있으면 아무것도 하지 않으므로 배선된 매핑이 우선한다.
    /// </summary>
    internal static class CounterReturnBridgeBootstrap
    {
        private const string CookingSceneName = "BurgerAssembly";
        private const string BridgeObjectName = "CounterReturnBridge (Auto)";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            // AfterSceneLoad 시점에는 첫 씬이 이미 올라와 있으므로 여기서 한 번 확인한다.
            EnsureBridge(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBridge(scene);
        }

        private static void EnsureBridge(Scene scene)
        {
            if (!scene.IsValid() || scene.name != CookingSceneName)
            {
                return;
            }

            if (Object.FindFirstObjectByType<BurgerAssemblyCounterBridge>() != null)
            {
                return;
            }

            BurgerAssemblyController controller = Object.FindFirstObjectByType<BurgerAssemblyController>();
            if (controller == null)
            {
                Debug.LogWarning("[CounterReturnBridge] BurgerAssemblyController를 찾지 못해 자동 설치를 건너뜁니다.");
                return;
            }

            GameObject owner = new GameObject(BridgeObjectName);
            SceneManager.MoveGameObjectToScene(owner, scene);

            BurgerAssemblyCounterBridge bridge = owner.AddComponent<BurgerAssemblyCounterBridge>();
            bridge.AttachController(controller);

            Debug.Log("[CounterReturnBridge] 씬에 브릿지가 없어 런타임에 자동 설치했습니다.");
        }
    }
}
