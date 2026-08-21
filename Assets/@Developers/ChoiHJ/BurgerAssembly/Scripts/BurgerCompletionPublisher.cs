using System;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    internal sealed class BurgerCompletionPublisher
    {
        public event Action<BurgerData> Completed;
        public event Action<PaymentResult> PaymentCalculated;

        public BurgerData LastCompletedBurger { get; private set; }
        public PaymentResult LastPaymentResult { get; private set; }

        public void Publish(BurgerData burgerData, PaymentResult paymentResult)
        {
            if (burgerData == null)
            {
                throw new ArgumentNullException(nameof(burgerData));
            }
            if (paymentResult == null)
            {
                throw new ArgumentNullException(nameof(paymentResult));
            }

            LastCompletedBurger = burgerData;
            LastPaymentResult = paymentResult;
            Debug.Log(
                "[BurgerAssembly] OnBurgerCompleted\n" + JsonUtility.ToJson(burgerData, true) +
                "\n[CookingScene] PaymentResult\n" + JsonUtility.ToJson(paymentResult, true));
            Completed?.Invoke(burgerData);
            PaymentCalculated?.Invoke(paymentResult);
        }

        public void Reset()
        {
            LastCompletedBurger = null;
            LastPaymentResult = null;
        }
    }
}
