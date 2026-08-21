using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/SpecialCustomer")]
    public class SpecialCustomerData: ScriptableObject
    {
        public int id;
        public string customerName;
        public string spritePath;
        public OrderData fixedOrder;
        public EventTriggerType triggerType;
        public int triggerValue;
        public bool oneTimeOnly;
        public int bonusGold;
        public RecipeData unlockRecipeOnSuccess;
    }
}