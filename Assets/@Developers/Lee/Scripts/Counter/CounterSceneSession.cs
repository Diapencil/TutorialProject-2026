using System;
using SheepSheepBurger.Core;

namespace Lee.Counter
{
    public static class CounterSceneSession
    {
        public static OrderInstance ActiveOrder { get; private set; }
        public static BurgerData CookedBurger { get; private set; }
        public static bool HasConfirmedOrder { get; private set; }
        public static event Action<BurgerData> BurgerSubmitted;

        public static void BeginOrder(OrderInstance order)
        {
            ActiveOrder = order;
            ActiveOrder.phase = OrderPhase.Ordering;
            CookedBurger = null;
            HasConfirmedOrder = false;
        }

        public static void ConfirmOrderForCooking()
        {
            HasConfirmedOrder = true;
            if (ActiveOrder != null) ActiveOrder.phase = OrderPhase.Cooking;
        }

        public static void SubmitCookedBurger(BurgerData burger)
        {
            CookedBurger = burger;
            if (ActiveOrder != null) ActiveOrder.phase = OrderPhase.Serving;
            BurgerSubmitted?.Invoke(burger);
        }

        public static void ClearOrder()
        {
            if (ActiveOrder != null) ActiveOrder.phase = OrderPhase.Resolved;
            ActiveOrder = null;
            CookedBurger = null;
            HasConfirmedOrder = false;
        }
    }
}
