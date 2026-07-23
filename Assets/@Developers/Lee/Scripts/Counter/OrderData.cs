using UnityEngine;

namespace Lee.Counter
{
    /// <summary>한 손님에게만 유효한 주문과 인내 시간입니다.</summary>
    public sealed class OrderData
    {
        public RecipeData RequestedRecipe { get; }
        private readonly float deadline;

        public OrderData(RecipeData requestedRecipe, float patienceSeconds)
        {
            RequestedRecipe = requestedRecipe;
            deadline = Time.realtimeSinceStartup + patienceSeconds;
        }

        public float RemainingPatience => Mathf.Max(0f, deadline - Time.realtimeSinceStartup);
        public bool IsExpired => RemainingPatience <= 0f;
    }
}
