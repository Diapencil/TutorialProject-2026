// GameState를 들고 씬 전환에도 살아남는 임시 싱글톤. 세이브/로드는 아직 범위 밖이다.
using UnityEngine;

namespace SheepSheepBurger.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState state = new GameState();

        [Header("디버그")]
        [Tooltip("[Add 10000 Gold] 컨텍스트 메뉴로 지급할 금액(10배 정수).")]
        [SerializeField] private int debugGoldAmount = 10000;

        public GameState State => state;

        /// <summary>
        /// 어느 씬에서 플레이를 시작하든 GameState가 반드시 하나 존재하도록 보장한다.
        /// 씬에 GameManagerObj가 있으면 그것을 쓰고, 없으면 런타임에 만든다.
        /// </summary>
        public static GameManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameManager found = FindFirstObjectByType<GameManager>();
            if (found != null)
            {
                // Awake가 아직 안 돌았을 수 있으므로 여기서 Instance를 확정한다.
                found.EnsureInitialized();
                return Instance;
            }

            GameObject owner = new GameObject(nameof(GameManager));
            GameManager created = owner.AddComponent<GameManager>();
            created.EnsureInitialized();
            return created;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }

            Instance = this;

            if (state == null)
            {
                state = new GameState();
            }

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [ContextMenu("Add 10000 Gold")]
        private void AddDebugGold()
        {
            if (state == null)
            {
                return;
            }

            state.gold += debugGoldAmount;
            Debug.Log($"[GameManager] 디버그 골드 지급. 현재 보유: {state.gold}", this);
        }
    }
}
