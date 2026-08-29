using System;
using SheepSheepBurger.Core;
using UnityEngine;

namespace SheepSheepBurger.Counter
{
    public static class CounterSceneSession
    {
        public static OrderInstance ActiveOrder { get; private set; }
        public static BurgerData CookedBurger { get; private set; }
        public static bool HasConfirmedOrder { get; private set; }
        /// <summary>
        /// 이번 주문에서 "네?" 버튼(힌트 요청)을 눌렀는지 여부.
        /// CounterSceneUI는 Cooking↔Counter 씬 전환마다 파괴/재생성되므로,
        /// 씬 전환에도 살아남는 이 정적 세션에 보관해야 서빙 판정 시점까지 값이 유지된다.
        /// </summary>
        public static bool HintUsed { get; private set; }
        public static event Action<BurgerData> BurgerSubmitted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveOrder = null;
            CookedBurger = null;
            HasConfirmedOrder = false;
            HintUsed = false;
            BurgerSubmitted = null;
        }

        public static void BeginOrder(OrderInstance order)
        {
            ActiveOrder = order;
            ActiveOrder.phase = OrderPhase.Ordering;
            ActiveOrder.selectedOrderLine = string.Empty;
            CookedBurger = null;
            HasConfirmedOrder = false;
            HintUsed = false;
        }

        public static void MarkHintUsed() => HintUsed = true;

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
            HintUsed = false;
        }
    }
}
