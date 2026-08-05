using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Customer")]
    public class CustomerData : ScriptableObject
    {
        public int id;
        public string name;
        public string spritePath;
        public float patienceTime;
        public float attackChance;
        public float tipMultiplier;
        public int unlockDay;
    }
}
