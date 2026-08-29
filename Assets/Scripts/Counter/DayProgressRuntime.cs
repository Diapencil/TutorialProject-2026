using System;
using SheepSheepBurger.Core;
using UnityEngine;

namespace SheepSheepBurger.Counter
{
    /// <summary>Owns Core runtime state for the current day without mutating data assets.</summary>
    public sealed class DayProgressRuntime : MonoBehaviour
    {
        private static DayProgressRuntime instance;

        public GameState GameState { get; private set; }
        public DayState DayState { get; private set; }
        public int CurrentDay => GameState != null ? GameState.currentDay : 1;
        public int ServedCustomerCount => DayState != null ? DayState.customersServed : 0;
        public int DailyRevenue => DayState != null ? DayState.dailyRevenue : 0;
        public int DailyIngredientCost => DayState != null ? DayState.dailyIngredientCost : 0;
        public int DailyProfit => DayState != null ? DayState.dailyProfit : 0;
        public int PerfectCount => DayState != null ? DayState.perfectCount : 0;
        public int GoodCount => DayState != null ? DayState.goodCount : 0;
        public int NormalCount => DayState != null ? DayState.normalCount : 0;
        public int BadCount => DayState != null ? DayState.badCount : 0;
        public bool IsCurrentDayComplete => DayState != null && DayState.isComplete;

        public event Action<DayState> DayCompleted;

        public static DayProgressRuntime GetOrCreate()
        {
            if (instance == null) instance = FindFirstObjectByType<DayProgressRuntime>();
            if (instance != null)
            {
                instance.Initialize();
                return instance;
            }

            var owner = new GameObject(nameof(DayProgressRuntime));
            instance = owner.AddComponent<DayProgressRuntime>();
            instance.Initialize();
            DontDestroyOnLoad(owner);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            Initialize();
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        private void Initialize()
        {
            if (GameState != null && DayState != null) return;

            // 상점(GameManager)과 같은 GameState 인스턴스를 공유해야 한다.
            // 따로 new GameState()를 만들면 카운터 매출이 상점 보유 금액에 반영되지 않는다.
            GameState = GameManager.GetOrCreate().State;
            GameState.EnsureRuntimeCollections();
            DayState = GameState.GetOrCreateCurrentDayState();
        }

        public void RegisterCustomer(OrderInstance order,
                                     BurgerData burger,
                                     OrderJudgement judgement,
                                     int reward)
        {
            RefreshStateReferences();
            DayState.RecordServedOrder(order,
                                       burger,
                                       judgement.grade,
                                       reward,
                                       judgement.hintUsed,
                                       judgement.ingredientErrors,
                                       judgement.cookStateErrors);
            GameState.totalCustomersServed++;
            GameState.gold += reward;
            GameManager.SaveCurrentGame();
        }

        public void CompleteCurrentDay()
        {
            RefreshStateReferences();
            bool wasAlreadyComplete = DayState.isComplete;
            GameState.CompleteCurrentDay();
            DayState = GameState.GetOrCreateCurrentDayState();
            GameManager.SaveCurrentGame();

            if (!wasAlreadyComplete)
            {
                DayCompleted?.Invoke(DayState);
            }
        }

        public void BeginNextDay()
        {
            RefreshStateReferences();
            GameState.BeginNextDay();
            DayState = GameState.GetOrCreateCurrentDayState();
            GameManager.SaveCurrentGame();
        }

        private void RefreshStateReferences()
        {
            if (GameState == null)
            {
                GameState = GameManager.GetOrCreate().State;
            }

            GameState.EnsureRuntimeCollections();
            DayState = GameState.GetOrCreateCurrentDayState();
        }
    }
}
