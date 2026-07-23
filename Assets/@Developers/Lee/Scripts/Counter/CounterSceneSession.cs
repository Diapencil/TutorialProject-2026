using System;

namespace Lee.Counter
{
    /// <summary>
    /// 카운터와 조리 씬 사이에서 현재 주문과 완성 버거를 보관합니다.
    /// 씬 전환 중에도 OrderData의 realtime 기반 인내 시간은 계속 흐릅니다.
    /// </summary>
    public static class CounterSceneSession
    {
        public static OrderData ActiveOrder { get; private set; }
        public static BurgerData CookedBurger { get; private set; }
        public static bool HasConfirmedOrder { get; private set; }
        public static event Action<BurgerData> BurgerSubmitted;

        public static void BeginOrder(OrderData order)
        {
            ActiveOrder = order;
            CookedBurger = null;
            HasConfirmedOrder = false;
        }

        public static void ConfirmOrderForCooking() => HasConfirmedOrder = true;
        public static void SubmitCookedBurger(BurgerData burger)
        {
            CookedBurger = burger;
            BurgerSubmitted?.Invoke(burger);
        }

        public static void ClearOrder()
        {
            ActiveOrder = null;
            CookedBurger = null;
            HasConfirmedOrder = false;
        }
    }
}
