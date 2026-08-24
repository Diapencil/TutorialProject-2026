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
        [SerializeField] private string cookingSceneName = "Cooking";
        [SerializeField] private GradeConfig gradeConfig;
        [SerializeField] private List<SheepSheepBurger.Core.CustomerData> availableCustomers = new();
        [SerializeField] private List<OrderData> availableOrders = new();
        [TextArea, SerializeField] private string perfectLine = "Perfect!";
        [TextArea, SerializeField] private string normalLine = "Good!";
        [TextArea, SerializeField] private string badLine = "That was not what I ordered.";

        public int CustomersPerDay => customersPerDay;
        public float PatienceSeconds => patienceSeconds;
        public float ReactionSeconds => reactionSeconds;
        public string CookingSceneName => cookingSceneName;
        public GradeConfig GradeConfig => gradeConfig;
        public IReadOnlyList<SheepSheepBurger.Core.CustomerData> AvailableCustomers => availableCustomers;
        public IReadOnlyList<OrderData> AvailableOrders => availableOrders;
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
}
