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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (state == null)
            {
                state = new GameState();
            }

            DontDestroyOnLoad(gameObject);
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
