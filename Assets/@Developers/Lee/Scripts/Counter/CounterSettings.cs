using System.Collections.Generic;
using SheepSheepBurger.Core;
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
        [SerializeField] private List<CustomerData> availableCustomers = new();
        [SerializeField] private List<OrderData> availableOrders = new();
        [TextArea, SerializeField] private string perfectReaction = "Perfect!";
        [TextArea, SerializeField] private string goodReaction = "Good!";
        [TextArea, SerializeField] private string badReaction = "That was not what I ordered.";

        public int CustomersPerDay => customersPerDay;
        public float PatienceSeconds => patienceSeconds;
        public float ReactionSeconds => reactionSeconds;
        public string CookingSceneName => cookingSceneName;
        public IReadOnlyList<CustomerData> AvailableCustomers => availableCustomers;
        public IReadOnlyList<OrderData> AvailableOrders => availableOrders;
        public string GetReaction(Grade grade) => grade switch
        {
            Grade.Perfect => perfectReaction,
            Grade.Good => goodReaction,
            _ => badReaction
        };
    }
}
