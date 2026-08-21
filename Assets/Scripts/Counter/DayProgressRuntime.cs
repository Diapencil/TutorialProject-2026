using SheepSheepBurger.Core;
using UnityEngine;

namespace Lee.Counter
{
    /// <summary>Owns Core runtime state for the current day without mutating data assets.</summary>
    public sealed class DayProgressRuntime : MonoBehaviour
    {
        private static DayProgressRuntime instance;

        public GameState GameState { get; private set; }
        public DayState DayState { get; private set; }
        public int CurrentDay => GameState.currentDay;
        public int ServedCustomerCount => DayState.customersServed;
        public int DailyRevenue => DayState.dailyRevenue;

        public static DayProgressRuntime GetOrCreate()
        {
            if (instance == null) instance = FindFirstObjectByType<DayProgressRuntime>();
            if (instance != null) return instance;

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
            if (GameState != null) return;
            GameState = new GameState { currentDay = 1 };
            DayState = new DayState { count = new System.Collections.Generic.List<int>() };
        }

        public void RegisterCustomer(int reward)
        {
            DayState.customersServed++;
            DayState.dailyRevenue += reward;
            GameState.totalCustomersServed++;
            GameState.gold += reward;
        }

        public void BeginNextDay()
        {
            GameState.currentDay++;
            DayState.customersServed = 0;
            DayState.dailyRevenue = 0;
            DayState.wasAttackedToday = false;
        }
    }
}
