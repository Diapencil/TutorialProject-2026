// 씬에 재료 매핑이 배선되지 않았을 때 쓰는 코드 기반 대체 테이블.
using System.Collections.Generic;
using UnityEngine;
using BurgerAssemblyIngredientType = SheepSheepBurger.BurgerAssembly.IngredientType;
using CoreIngredientData = SheepSheepBurger.Core.IngredientData;

namespace SheepSheepBurger.Counter
{
    /// <summary>
    /// 조리 씬 IngredientType → Core IngredientData 를 애셋 참조 없이 만들어 준다.
    /// 카운터가 읽는 값은 id / ingredientName / costPerUse / grillable 네 개뿐이라
    /// 이 값들만 채우면 채점(OrderJudge)과 하루 집계(DayState)가 정상 동작한다.
    /// 값은 Assets/Data/Ingredients/*.asset 과 일치시켜야 한다.
    /// </summary>
    internal static class CounterReturnBridgeFallback
    {
        private readonly struct Entry
        {
            public readonly int Id;
            public readonly string Name;
            public readonly bool Grillable;

            public Entry(int id, string name, bool grillable)
            {
                Id = id;
                Name = name;
                Grillable = grillable;
            }
        }

        // TODO(기획확인): 애셋의 costPerUse가 전부 0이라 원가가 0으로 집계된다.
        // 스펙상 굽는 재료 3(0.3C) / 비조리 2(0.2C)이므로 애셋 값이 확정되면 함께 맞춰야 한다.
        private const int DefaultCostPerUse = 0;

        private static readonly Dictionary<BurgerAssemblyIngredientType, Entry> Table =
            new Dictionary<BurgerAssemblyIngredientType, Entry>
            {
                { BurgerAssemblyIngredientType.BunBottom,       new Entry(1,  "하단 번",  false) },
                { BurgerAssemblyIngredientType.BunTop,          new Entry(2,  "상단 번",  false) },
                { BurgerAssemblyIngredientType.Patty,           new Entry(3,  "패티",     true)  },
                { BurgerAssemblyIngredientType.ToppingCheese,   new Entry(4,  "치즈",     false) },
                { BurgerAssemblyIngredientType.ToppingPickle,   new Entry(5,  "피클",     false) },
                { BurgerAssemblyIngredientType.SauceKetchup,    new Entry(6,  "케첩",     false) },
                { BurgerAssemblyIngredientType.SauceMustard,    new Entry(7,  "머스타드", false) },
                { BurgerAssemblyIngredientType.ToppingJalapeno, new Entry(8,  "할라피뇨", false) },
                { BurgerAssemblyIngredientType.ToppingOnion,    new Entry(9,  "양파",     false) },
                { BurgerAssemblyIngredientType.ToppingLettuce,  new Entry(10, "양상추",   false) },
                { BurgerAssemblyIngredientType.ToppingTomato,   new Entry(11, "토마토",   false) },
                { BurgerAssemblyIngredientType.Egg,             new Entry(12, "계란",     true)  },
                { BurgerAssemblyIngredientType.Bacon,           new Entry(13, "베이컨",   true)  }
            };

        public static CoreIngredientData Create(BurgerAssemblyIngredientType type)
        {
            if (!Table.TryGetValue(type, out Entry entry))
            {
                return null;
            }

            CoreIngredientData data = ScriptableObject.CreateInstance<CoreIngredientData>();
            data.id = entry.Id;
            data.ingredientName = entry.Name;
            data.grillable = entry.Grillable;
            data.costPerUse = DefaultCostPerUse;
            data.name = entry.Name;
            return data;
        }
    }
}
