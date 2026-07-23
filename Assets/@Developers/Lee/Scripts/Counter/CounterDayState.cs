using UnityEngine;

namespace Lee.Counter
{
    /// <summary>프로토타입용 DayData입니다. 실제 프로젝트 DayData가 있으면 이 타입 대신 연결합니다.</summary>
    [CreateAssetMenu(menuName = "Lee/Counter/Day State", fileName = "DayState")]
    public sealed class CounterDayState : ScriptableObject
    {
        [Min(1), SerializeField] private int currentDay = 1;
        [SerializeField] private int servedCustomerCount;
        [SerializeField] private int dailyRevenue;

        // These are initial values only. Runtime changes belong to DayProgressRuntime.
        public int InitialDay => currentDay;
        public int InitialServedCustomerCount => servedCustomerCount;
        public int InitialDailyRevenue => dailyRevenue;
    }
}
