using System;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    internal sealed class BurgerCompletionPublisher
    {
        public event Action<BurgerData> Completed;

        public BurgerData LastCompletedBurger { get; private set; }

        public void Publish(BurgerData burgerData)
        {
            if (burgerData == null)
            {
                throw new ArgumentNullException(nameof(burgerData));
            }

            LastCompletedBurger = burgerData;
            Debug.Log("[BurgerAssembly] OnBurgerCompleted\n" + JsonUtility.ToJson(burgerData, true));
            Completed?.Invoke(burgerData);
        }

        public void Reset()
        {
            LastCompletedBurger = null;
        }
    }
}
