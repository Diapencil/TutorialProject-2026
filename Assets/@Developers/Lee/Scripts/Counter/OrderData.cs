using UnityEngine;

namespace Lee.Counter
{
    /// <summary>한 손님에게만 유효한 주문과 인내 시간입니다.</summary>
    public sealed class OrderData
    {
        public CustomerData Customer { get; }
        public RecipeData RequestedRecipe { get; }
        private readonly RecipeData.CustomerRequestDialogue customerDialogue;
        public string CustomerRequest => customerDialogue.InitialRequest;
        public string CustomerClarificationRequest => customerDialogue.ClarificationRequest;
        private readonly float deadline;

        public OrderData(CustomerData customer, RecipeData requestedRecipe, float patienceSeconds)
        {
            Customer = customer;
            RequestedRecipe = requestedRecipe;
            customerDialogue = requestedRecipe.GetRandomCustomerRequestDialogue();
            deadline = Time.realtimeSinceStartup + patienceSeconds;
        }

        public float RemainingPatience => Mathf.Max(0f, deadline - Time.realtimeSinceStartup);
        public bool IsExpired => RemainingPatience <= 0f;
    }
}
