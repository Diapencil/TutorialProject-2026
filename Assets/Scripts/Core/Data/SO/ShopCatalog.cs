// 상점에서 사용하는 재료, 설비, 장식 애셋을 한곳에서 제공한다.
using System;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Shop Catalog", fileName = "ShopCatalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        public const string ResourcesPath = "Shop/ShopCatalog";

        [SerializeField] private IngredientData[] ingredients = Array.Empty<IngredientData>();
        [SerializeField] private UpgradeData[] upgrades = Array.Empty<UpgradeData>();
        [SerializeField] private DecorationData[] decorations = Array.Empty<DecorationData>();

        public IngredientData[] Ingredients => ingredients;
        public UpgradeData[] Upgrades => upgrades;
        public DecorationData[] Decorations => decorations;

        public static ShopCatalog LoadDefault()
        {
            return Resources.Load<ShopCatalog>(ResourcesPath);
        }

        public UpgradeData GetUpgrade(int id)
        {
            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i] != null && upgrades[i].id == id)
                {
                    return upgrades[i];
                }
            }

            return null;
        }
    }
}
