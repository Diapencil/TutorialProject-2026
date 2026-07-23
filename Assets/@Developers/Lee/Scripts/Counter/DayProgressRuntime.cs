using UnityEngine;

namespace Lee.Counter
{
    /// <summary>
    /// Holds day progress for the current game run. It survives scene loads and never mutates a data asset.
    /// </summary>
    public sealed class DayProgressRuntime : MonoBehaviour
    {
        private static DayProgressRuntime instance;

        public int CurrentDay { get; private set; }
        public int ServedCustomerCount { get; private set; }
        public int DailyRevenue { get; private set; }

        public static DayProgressRuntime GetOrCreate(CounterDayState initialState)
        {
            if (instance == null)
                instance = FindFirstObjectByType<DayProgressRuntime>();
            if (instance != null)
                return instance;

            var owner = new GameObject(nameof(DayProgressRuntime));
            instance = owner.AddComponent<DayProgressRuntime>();
            instance.Initialize(initialState);
            DontDestroyOnLoad(owner);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Initialize(CounterDayState initialState)
        {
            if (initialState == null)
            {
                Debug.LogError("CounterDayState is required to initialize day progress.");
                CurrentDay = 1;
                return;
            }

            CurrentDay = initialState.InitialDay;
            ServedCustomerCount = initialState.InitialServedCustomerCount;
            DailyRevenue = initialState.InitialDailyRevenue;
        }

        public void RegisterCustomer(int reward)
        {
            ServedCustomerCount++;
            DailyRevenue += reward;
        }

        public void BeginNextDay()
        {
            CurrentDay++;
            ServedCustomerCount = 0;
            DailyRevenue = 0;
        }
    }
}
