namespace SheepSheepBurger.BurgerAssembly
{
    public enum IngredientType
    {
        Patty = 0,
        BunBottom = 1,
        BunTop = 2,
        ToppingLettuce = 3,
        ToppingTomato = 4,
        ToppingCheese = 5,
        ToppingOnion = 6,
        ToppingPickle = 7,
        SauceKetchup = 8,
        SauceMustard = 9,
        Bacon = 10,
        Egg = 11,
        ToppingJalapeno = 12
    }

    public enum CookingDragKind
    {
        Ingredient,
        RawGrillItem,
        CookedGrillItem,
        Sauce
    }

    public enum CookingCameraZone
    {
        Grill,
        Board,
        Packaging
    }
}
