using System.Collections.Generic;
using UnityEngine;

namespace Lee.Counter
{
    [CreateAssetMenu(menuName = "Lee/Counter/Counter Settings", fileName = "CounterSettings")]
    public sealed class CounterSettings : ScriptableObject
    {
        [Min(1), SerializeField] private int customersPerDay = 8;
        [Min(1f), SerializeField] private float patienceSeconds = 60f;
        [Min(0f), SerializeField] private float reactionSeconds = 2f;
        [SerializeField] private string cookingSceneName = "Cooking";
        [Tooltip("하루 동안 등장할 수 있는 손님 종류입니다.")]
        [SerializeField] private List<CustomerData> availableCustomers = new();
        [TextArea, SerializeField] private string perfectReaction = "완벽해요! 정말 맛있어요!";
        [TextArea, SerializeField] private string goodReaction = "조금 아쉽지만 맛있게 먹을게요.";
        [TextArea, SerializeField] private string badReaction = "제가 주문한 버거가 아닌 것 같아요.";
        [TextArea, SerializeField] private string timeoutReaction = "너무 오래 기다렸어요. 이만 갈게요.";

        public int CustomersPerDay => customersPerDay;
        public float PatienceSeconds => patienceSeconds;
        public float ReactionSeconds => reactionSeconds;
        public string CookingSceneName => cookingSceneName;
        public IReadOnlyList<CustomerData> AvailableCustomers => availableCustomers;
        public string GetReaction(ServiceResult result) => result switch
        {
            ServiceResult.Perfect => perfectReaction,
            ServiceResult.Good => goodReaction,
            ServiceResult.Timeout => timeoutReaction,
            _ => badReaction
        };
    }
}
