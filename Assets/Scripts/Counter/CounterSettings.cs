using System.Collections.Generic;
using SheepSheepBurger.Core;
using UnityEngine;

namespace SheepSheepBurger.Counter
{
    [CreateAssetMenu(menuName = "Lee/Counter/Counter Settings", fileName = "CounterSettings")]
    public sealed class CounterSettings : ScriptableObject
    {
        [Min(1), SerializeField] private int customersPerDay = 8;
        [Min(1f), SerializeField] private float patienceSeconds = 60f;
        [Min(0f), SerializeField] private float reactionSeconds = 2f;
        [Min(0f), SerializeField] private float exitDelaySeconds = 0.5f;
        [SerializeField] private string cookingSceneName = "Cooking";
        [SerializeField] private GradeConfig gradeConfig;
        [SerializeField] private List<SheepSheepBurger.Core.CustomerData> availableCustomers = new();
        [Tooltip("지정한 일차의 지정 순번에 무작위 손님 대신 반드시 등장시킬 손님 목록입니다. 같은 일차/순번이 중복되면 목록에서 먼저 등록된 항목을 사용합니다.")]
        [SerializeField] private List<FixedCustomerScheduleEntry> fixedCustomerSchedule = new();
        [SerializeField] private List<OrderData> availableOrders = new();
        [TextArea, SerializeField] private string perfectLine = "Perfect!";
        [TextArea, SerializeField] private string normalLine = "Good!";
        [TextArea, SerializeField] private string badLine = "That was not what I ordered.";

        public int CustomersPerDay => customersPerDay;
        public float PatienceSeconds => patienceSeconds;
        public float ReactionSeconds => reactionSeconds;
        /// <summary>수령 대사가 다 출력된 후 퇴장 애니메이션이 시작되기까지의 텀입니다.</summary>
        public float ExitDelaySeconds => exitDelaySeconds;
        public string CookingSceneName => cookingSceneName;
        public GradeConfig GradeConfig => gradeConfig;
        public IReadOnlyList<SheepSheepBurger.Core.CustomerData> AvailableCustomers => availableCustomers;
        public IReadOnlyList<FixedCustomerScheduleEntry> FixedCustomerSchedule => fixedCustomerSchedule;
        public IReadOnlyList<OrderData> AvailableOrders => availableOrders;

        /// <summary>
        /// 해당 일차의 손님 순번에 배정된 고정 손님을 반환합니다.
        /// 일차와 순번은 모두 1부터 시작합니다.
        /// </summary>
        public CustomerData GetFixedCustomer(int dayNumber, int customerNumber)
        {
            return GetFixedCustomerScheduleEntry(dayNumber, customerNumber)?.Customer;
        }

        /// <summary>
        /// 해당 일차/순번에 배정된 고정 손님 규칙을 반환합니다.
        /// 규칙에는 손님뿐 아니라 손님 전용 주문도 포함될 수 있습니다.
        /// </summary>
        public FixedCustomerScheduleEntry GetFixedCustomerScheduleEntry(int dayNumber, int customerNumber)
        {
            if (fixedCustomerSchedule == null) return null;

            for (int i = 0; i < fixedCustomerSchedule.Count; i++)
            {
                FixedCustomerScheduleEntry entry = fixedCustomerSchedule[i];
                if (entry != null && entry.Matches(dayNumber, customerNumber)) return entry;
            }

            return null;
        }
        /// <summary>
        /// 등급별 수령 대사. 주문의 Dialogue(perfectLine/normalLine/badLine)를 우선 사용하고,
        /// 비어있으면 CounterSettings의 기본 대사로 대체한다. Good과 Normal은 normalLine을 공유한다.
        /// </summary>
        public string GetReaction(Grade grade, Dialogue dialogue)
        {
            var orderLine = grade switch
            {
                Grade.Perfect => dialogue != null ? dialogue.perfectLine : null,
                Grade.Bad => dialogue != null ? dialogue.badLine : null,
                _ => dialogue != null ? dialogue.normalLine : null
            };
            if (!string.IsNullOrWhiteSpace(orderLine)) return orderLine;

            return grade switch
            {
                Grade.Perfect => perfectLine,
                Grade.Bad => badLine,
                _ => normalLine
            };
        }
    }

    /// <summary>특정 일차의 특정 순번에 항상 등장할 손님 설정입니다.</summary>
    [System.Serializable]
    public sealed class FixedCustomerScheduleEntry
    {
        [Min(1), SerializeField] private int dayNumber = 1;
        [Min(1), SerializeField] private int customerNumber = 1;
        [SerializeField] private CustomerData customer;
        [Tooltip("비워 두면 일반 주문 목록에서 무작위 주문을 선택합니다.")]
        [SerializeField] private OrderData fixedOrder;

        public int DayNumber => dayNumber;
        public int CustomerNumber => customerNumber;
        public CustomerData Customer => customer;
        public OrderData FixedOrder => fixedOrder;

        public bool Matches(int targetDayNumber, int targetCustomerNumber)
        {
            return dayNumber == targetDayNumber && customerNumber == targetCustomerNumber;
        }
    }
}
