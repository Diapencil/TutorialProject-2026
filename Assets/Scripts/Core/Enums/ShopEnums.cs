// 상점 화면 전용 열거형 모음. IngredientType/ShopCondition은 GameEnums.cs에 이미 있으므로 여기서 재선언하지 않는다.
namespace SheepSheepBurger.Core
{
    /// <summary>
    /// 상점 좌측 사이드바의 탭 종류.
    /// </summary>
    public enum ShopTabType
    {
        Topping,
        Upgrade,
        Decoration,
        Debt
    }

    /// <summary>
    /// 업&수리 탭에서 다루는 설비 종류.
    /// </summary>
    public enum UpgradeType
    {
        Fryer,
        Grill
    }
}
