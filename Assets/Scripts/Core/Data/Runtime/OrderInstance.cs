using System;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class OrderInstance
    {
        public CustomerData customer;
        public OrderData order;
        public int spriteIndex;
        public string selectedOrderLine;
        public int patienceRemaining;
        public bool hintUsed;
        public OrderPhase phase;
    }
}
