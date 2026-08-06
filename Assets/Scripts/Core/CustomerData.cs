using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Customer")]
    public class CustomerData : ScriptableObject
    {
        public int id;
        public List<OrderData> availableOrders;
        public string customerName;
        public string spritePath;
        public float patienceTime;
        public float attackChance;
        public float tipMultiplier;
        public int unlockDay;
    }
}
