using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Order")]
    public class OrderData : ScriptableObject
    {
        public int id;
        public RecipeData recipe;
        public Dialogue dialogue;
    }
}
