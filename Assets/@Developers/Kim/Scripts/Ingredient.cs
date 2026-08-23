using UnityEngine;

// 재료 데이터 구조입니다.
// ScriptableObject 에셋으로 만들어도 되고, 런타임에서는 IngredientLibrary가 같은 값을 제공합니다.
// 붙이는 오브젝트: 없음. 데이터 에셋 또는 코드 참조용입니다.
public enum IngredientType
{
    Bun,
    Patty,
    Cheese,
    Lettuce,
    Tomato,
    Onion,
    Pickle,
    FishFillet,
    BunTop
}

[CreateAssetMenu(menuName = "Burger Demo/Ingredient", fileName = "Ingredient")]
public class Ingredient : ScriptableObject
{
    public IngredientType id;
    public string displayName;
    public Color color;
}

public readonly struct IngredientDefinition
{
    public readonly IngredientType Id;
    public readonly string DisplayName;
    public readonly Color Color;

    public IngredientDefinition(IngredientType id, string displayName, Color color)
    {
        Id = id;
        DisplayName = displayName;
        Color = color;
    }
}

// 재료 색상과 이름을 한곳에서 관리합니다.
// 붙이는 오브젝트: 없음. 다른 스크립트에서 IngredientLibrary.Get(...)로 사용합니다.
public static class IngredientLibrary
{
    public static readonly IngredientType[] TrayIngredients =
    {
        IngredientType.Patty,
        IngredientType.Cheese,
        IngredientType.Lettuce,
        IngredientType.Tomato,
        IngredientType.Onion,
        IngredientType.Pickle,
        IngredientType.FishFillet
    };

    // 명세의 Unity 정규화 색상값을 그대로 사용합니다.
    public static IngredientDefinition Get(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.Bun:
                return new IngredientDefinition(type, "빵", new Color(0.957f, 0.784f, 0.459f));
            case IngredientType.Patty:
                return new IngredientDefinition(type, "패티", new Color(0.545f, 0.290f, 0.169f));
            case IngredientType.Cheese:
                return new IngredientDefinition(type, "치즈", new Color(1.000f, 0.835f, 0.000f));
            case IngredientType.Lettuce:
                return new IngredientDefinition(type, "양상추", new Color(0.545f, 0.765f, 0.290f));
            case IngredientType.Tomato:
                return new IngredientDefinition(type, "토마토", new Color(0.898f, 0.224f, 0.208f));
            case IngredientType.Onion:
                return new IngredientDefinition(type, "양파", new Color(0.847f, 0.706f, 0.886f));
            case IngredientType.Pickle:
                return new IngredientDefinition(type, "피클", new Color(0.333f, 0.545f, 0.184f));
            case IngredientType.FishFillet:
                return new IngredientDefinition(type, "물고기살", new Color(0.565f, 0.643f, 0.682f));
            case IngredientType.BunTop:
                return new IngredientDefinition(type, "윗빵", new Color(0.957f, 0.784f, 0.459f));
            default:
                return new IngredientDefinition(type, type.ToString(), Color.white);
        }
    }
}
