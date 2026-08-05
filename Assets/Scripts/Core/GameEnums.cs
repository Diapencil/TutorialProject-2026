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

    /// <summary>
    /// The operational state of the restaurant.
    /// </summary>
    public enum ShopCondition
    {
        Normal,
        Broken
    }
}
