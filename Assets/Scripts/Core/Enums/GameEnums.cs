namespace SheepSheepBurger.Core
{
    /// <summary>
    /// A burger ingredient's role in its assembly order.
    /// </summary>
    public enum IngredientType
    {
        Bun,
        Patty,
        Topping,
        Sauce
    }

    /// <summary>
    /// The result of judging a submitted order.
    /// </summary>
    public enum Grade
    {
        Perfect,
        Good,
        Normal,
        Bad
    }

    public enum OrderPhase
    {
        Ordering,
        Cooking,
        Serving,
        Resolved
    }

    public enum CookState
    {
        NotRequired,
        UnderCooked,
        Perfect,
        Burnt
    }
    
    // Condition of Special Customer
    public enum EventTriggerType
    {
        SpecificDay,        // 특정 일차에 등장
        AfterNCustomers,    // 누적 n명 응대 후 등장
        DebtBelowAmount,    // 빚이 특정 금액 이하로 떨어졌을 때
        Manual              // 스크립트로 직접 호출
    }
    
    /// <summary>
    /// The operational state of the restaurant.
    /// </summary>
    public enum ShopCondition
    {
        Normal,
        Broken
    }
}
