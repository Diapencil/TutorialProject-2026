using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Economy
{
    public sealed class CustomerCatalog
    {
        private readonly List<CustomerPreferenceData> customers;

        public CustomerCatalog(IEnumerable<CustomerPreferenceData> source)
        {
            customers = new List<CustomerPreferenceData>(source ?? throw new ArgumentNullException(nameof(source)));
        }

        public IList<CustomerPreferenceData> Customers => customers.AsReadOnly();

        public static CustomerCatalog CreateDefault()
        {
            return new CustomerCatalog(new[]
            {
                new CustomerPreferenceData(CustomerId.Lion, "Lion", 35, new[] { BurgerRecipeId.Hamburger, BurgerRecipeId.SoopSoopBurger }),
                new CustomerPreferenceData(CustomerId.Wolf, "Wolf", 35, new[] { BurgerRecipeId.SoopSoopBurger, BurgerRecipeId.MiaBurger, BurgerRecipeId.Hamburger, BurgerRecipeId.ThreePattyBurger }),
                new CustomerPreferenceData(CustomerId.Elephant, "Elephant", 15, new[] { BurgerRecipeId.VegetarianBurger, BurgerRecipeId.MiaBurger }),
                new CustomerPreferenceData(CustomerId.Giraffe, "Giraffe", 15, new[] { BurgerRecipeId.VeganBurger })
            });
        }

        public CustomerPreferenceData PickWeighted(System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int total = 0;
            for (int index = 0; index < customers.Count; index++)
            {
                total += Math.Max(0, customers[index].appearanceWeight);
            }

            if (total <= 0)
            {
                return customers.Count > 0 ? customers[0] : null;
            }

            int roll = random.Next(0, total);
            int cursor = 0;
            for (int index = 0; index < customers.Count; index++)
            {
                cursor += Math.Max(0, customers[index].appearanceWeight);
                if (roll < cursor)
                {
                    return customers[index];
                }
            }

            return customers[customers.Count - 1];
        }
    }
}
