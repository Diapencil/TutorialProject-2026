// 재료 한 종류의 정적 데이터. 해금 상태는 이 애셋이 아니라 GameState에서만 관리한다.
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Ingredient")]
    public class IngredientData : ScriptableObject
    {
        public int id;
        public string ingredientName;
        public IngredientType type;

        /// <summary>상점 슬롯에 표시할 아이콘. 아트 리소스 입고 전까지는 비워둔다.</summary>
        public Sprite icon;

        /// <summary>실제 금액의 10배로 저장된 해금 비용.</summary>
        public int unlockCost;

        /// <summary>실제 금액의 10배로 저장된 1회 사용 비용.</summary>
        public int costPerUse;
        public bool isDefaultUnlocked;
        public bool grillable;
        public float cookTimeMin;
        public float cookTimeMax;
    }
}
