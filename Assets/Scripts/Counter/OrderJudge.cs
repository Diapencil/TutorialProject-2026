using System.Collections.Generic;
using SheepSheepBurger.Core;

namespace SheepSheepBurger.Counter
{
    public static class OrderJudge
    {
        /// <summary>
        /// hintUsed가 true면 requireNoHint 등급(Perfect/Good)은 후보에서 제외되므로
        /// "네?" 버튼을 눌렀을 경우 Perfect/Good이 나올 수 없다.
        /// </summary>
        public static Grade Judge(OrderData order, BurgerData burger, GradeConfig gradeConfig, bool hintUsed)
        {
            if (order == null || order.recipe == null || burger == null) return Grade.Bad;

            var counts = new Dictionary<int, int>();
            if (order.recipe.layers != null)
                foreach (var layer in order.recipe.layers)
                    if (layer?.ingredient != null) Add(counts, layer.ingredient.id, layer.quantity);
            if (burger.placedIngredients != null)
                foreach (var placed in burger.placedIngredients)
                    if (placed?.ingredient != null) Add(counts, placed.ingredient.id, -1);

            var errors = 0;
            foreach (var count in counts.Values) errors += System.Math.Abs(count);

            errors += CountCookStateErrors(order, burger);

            return ResolveGrade(errors, hintUsed, gradeConfig);
        }

        public static int GetReward(OrderData order, Grade grade, GradeConfig gradeConfig)
        {
            if (order == null || order.recipe == null) return 0;

            var entry = FindEntry(gradeConfig, grade);
            if (entry == null)
                return grade switch
                {
                    Grade.Perfect => order.recipe.basePrice,
                    Grade.Good => order.recipe.basePrice / 2,
                    _ => 0
                };

            return (entry.paysBasePrice ? order.recipe.basePrice : 0) + entry.tipAmount;
        }

        /// <summary>
        /// grillable 재료는 개수뿐 아니라 익힘 상태(cookState)도 요구치(cookRequirement)와 맞아야 한다.
        /// 재료 id별로 실제 익힘 상태 목록을 모은 뒤, 레시피가 요구하는 개수만큼 일치하는지 확인해
        /// 어긋난 개수만큼 오차를 더한다. (수량 오차는 위에서 이미 counts로 계산했으므로 여기서는 겹치는
        /// 개수 안에서의 익힘 상태 불일치만 센다.)
        /// </summary>
        private static int CountCookStateErrors(OrderData order, BurgerData burger)
        {
            if (order.recipe.layers == null || burger.placedIngredients == null) return 0;

            var placedStatesByIngredient = new Dictionary<int, List<CookState>>();
            foreach (var placed in burger.placedIngredients)
            {
                if (placed?.ingredient == null || !placed.ingredient.grillable) continue;
                if (!placedStatesByIngredient.TryGetValue(placed.ingredient.id, out var states))
                    placedStatesByIngredient[placed.ingredient.id] = states = new List<CookState>();
                states.Add(placed.cookState);
            }

            var cookErrors = 0;
            foreach (var layer in order.recipe.layers)
            {
                if (layer?.ingredient == null || !layer.ingredient.grillable) continue;

                placedStatesByIngredient.TryGetValue(layer.ingredient.id, out var placedStates);
                var actual = placedStates?.Count ?? 0;
                var overlap = System.Math.Min(layer.quantity, actual);
                if (overlap <= 0) continue;

                var matching = 0;
                foreach (var state in placedStates)
                    if (state == layer.cookRequirement) matching++;

                cookErrors += overlap - System.Math.Min(matching, overlap);
            }
            return cookErrors;
        }

        private static Grade ResolveGrade(int errors, bool hintUsed, GradeConfig gradeConfig)
        {
            if (gradeConfig == null || gradeConfig.grades == null || gradeConfig.grades.Count == 0)
                return errors == 0 ? Grade.Perfect : errors <= 1 ? Grade.Good : Grade.Bad;

            // 오차 허용치(maxErrors)를 만족하는 등급 중 가장 엄격한(maxErrors가 가장 작은) 등급을 고른다.
            GradeEntry best = null;
            foreach (var entry in gradeConfig.grades)
            {
                if (entry == null) continue;
                if (hintUsed && entry.requireNoHint) continue;
                if (errors > entry.maxErrors) continue;
                if (best == null || entry.maxErrors < best.maxErrors) best = entry;
            }
            if (best != null) return best.grade;

            // 어떤 등급의 오차 허용치도 만족하지 못하면 가장 관대한(maxErrors가 가장 큰) 등급으로 처리한다.
            GradeEntry fallback = null;
            foreach (var entry in gradeConfig.grades)
            {
                if (entry == null) continue;
                if (hintUsed && entry.requireNoHint) continue;
                if (fallback == null || entry.maxErrors > fallback.maxErrors) fallback = entry;
            }
            return fallback?.grade ?? Grade.Bad;
        }

        private static GradeEntry FindEntry(GradeConfig gradeConfig, Grade grade)
        {
            if (gradeConfig == null || gradeConfig.grades == null) return null;
            foreach (var entry in gradeConfig.grades)
                if (entry != null && entry.grade == grade) return entry;
            return null;
        }

        private static void Add(Dictionary<int, int> counts, int ingredientId, int amount)
        {
            counts.TryGetValue(ingredientId, out var current);
            counts[ingredientId] = current + amount;
        }
    }
}
