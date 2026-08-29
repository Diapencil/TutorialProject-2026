using System;
using System.Collections;
using System.Collections.Generic;
using SheepSheepBurger.Audio;
using SheepSheepBurger.BurgerAssembly;
using SheepSheepBurger.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using BurgerAssemblyIngredientType = SheepSheepBurger.BurgerAssembly.IngredientType;
using BurgerAssemblyBurgerData = SheepSheepBurger.BurgerAssembly.BurgerData;
using CoreBurgerData = SheepSheepBurger.Core.BurgerData;
using CorePlacedIngredient = SheepSheepBurger.Core.PlacedIngredient;
using CoreIngredientData = SheepSheepBurger.Core.IngredientData;

namespace SheepSheepBurger.Counter
{
    /// <summary>
    /// Connects the (Counter-independent) BurgerAssembly prototype back to the Counter scene:
    /// on packaging it converts the completed BurgerAssembly.BurgerData into Core.BurgerData,
    /// submits it through CounterSceneSession, then returns to the Counter scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BurgerAssemblyCounterBridge : MonoBehaviour
    {
        [Serializable]
        private struct IngredientMapEntry
        {
            public BurgerAssemblyIngredientType burgerAssemblyType;
            public CoreIngredientData coreIngredient;
        }

        [SerializeField] private BurgerAssemblyController controller;
        [SerializeField] private string counterSceneName = "Counter";
        [Min(0f), SerializeField] private float returnDelaySeconds = 1.5f;
        [SerializeField] private List<IngredientMapEntry> ingredientMap = new();

        private readonly Dictionary<BurgerAssemblyIngredientType, CoreIngredientData> ingredientLookup = new();
        private BurgerPackagingController packagingController;
        private bool submitted;

        /// <summary>
        /// 씬에 브릿지가 없어서 런타임에 자동 설치될 때 컨트롤러를 넣어준다.
        /// Start의 대기 코루틴이 controller를 계속 확인하므로 Awake 이후에 넣어도 된다.
        /// </summary>
        public void AttachController(BurgerAssemblyController value)
        {
            controller = value;
            ApplyActiveCustomerDialogue();
        }

        private void Awake()
        {
            ingredientLookup.Clear();
            foreach (var entry in ingredientMap)
                if (entry.coreIngredient != null)
                    ingredientLookup[entry.burgerAssemblyType] = entry.coreIngredient;

            ApplyActiveCustomerDialogue();
        }

        private void ApplyActiveCustomerDialogue()
        {
            if (controller == null) return;

            OrderInstance activeOrder = CounterSceneSession.ActiveOrder;
            if (activeOrder == null) return;

            string speaker = activeOrder.customer != null
                ? activeOrder.customer.customerName
                : string.Empty;
            string line = activeOrder.selectedOrderLine;
            if (string.IsNullOrWhiteSpace(line) &&
                activeOrder.order != null &&
                activeOrder.order.dialogue != null &&
                activeOrder.order.dialogue.orderLines != null)
            {
                foreach (string candidate in activeOrder.order.dialogue.orderLines)
                {
                    if (string.IsNullOrWhiteSpace(candidate)) continue;
                    line = candidate;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(line) &&
                activeOrder.order != null &&
                activeOrder.order.recipe != null)
            {
                line = activeOrder.order.recipe.recipeName;
            }

            controller.SetCustomerDialogue(speaker, line);
        }

        private void Start()
        {
            StartCoroutine(WaitForPackagingController());
        }

        private void OnDestroy()
        {
            if (packagingController != null) packagingController.Packaged -= HandlePackaged;
        }

        private IEnumerator WaitForPackagingController()
        {
            // BurgerAssemblyController builds its runtime UI (including the packaging
            // controller) during its own Awake, so wait until that reference exists
            // instead of assuming component initialization order.
            while (controller == null || controller.PackagingController == null) yield return null;
            packagingController = controller.PackagingController;
            packagingController.Packaged += HandlePackaged;
        }

        private void HandlePackaged()
        {
            if (submitted) return;
            submitted = true;

            var burger = controller.LastCompletedBurger;
            if (burger != null) CounterSceneSession.SubmitCookedBurger(ConvertToCoreBurger(burger));
            StartCoroutine(ReturnToCounter());
        }

        private IEnumerator ReturnToCounter()
        {
            float soundDelay = Mathf.Min(0.35f, returnDelaySeconds);
            if (soundDelay > 0f)
            {
                yield return new WaitForSeconds(soundDelay);
            }
            AudioManager.GetOrCreate().PlaySfx(AudioCueIds.SendPackage);

            float remainingDelay = Mathf.Max(0f, returnDelaySeconds - soundDelay);
            if (remainingDelay > 0f)
            {
                yield return new WaitForSeconds(remainingDelay);
            }

            SceneManager.LoadScene(counterSceneName);
        }

        private CoreBurgerData ConvertToCoreBurger(BurgerAssemblyBurgerData burger)
        {
            var placedIngredients = new List<CorePlacedIngredient>();
            foreach (var placement in burger.ingredients)
            {
                var ingredient = ResolveIngredient(placement.type);
                if (ingredient == null) continue;
                placedIngredients.Add(new CorePlacedIngredient
                {
                    ingredient = ingredient,
                    position = placement.position,
                    layerOrder = placement.layerOrder,
                    cookState = ResolveCookState(placement)
                });
            }

            foreach (var stroke in burger.sauceStrokes)
            {
                var ingredient = ResolveIngredient(stroke.type);
                if (ingredient == null) continue;
                placedIngredients.Add(new CorePlacedIngredient
                {
                    ingredient = ingredient,
                    position = Vector2.zero,
                    layerOrder = stroke.layerOrder,
                    cookState = CookState.NotRequired
                });
            }

            return new CoreBurgerData
            {
                placedIngredients = placedIngredients,
                appliedSauceIds = new List<int>(),
                sauceTrailPoints = new List<Vector2>(),
                isComplete = true
            };
        }

        private CoreIngredientData ResolveIngredient(BurgerAssemblyIngredientType type)
        {
            if (ingredientLookup.TryGetValue(type, out var ingredient)) return ingredient;

            // 씬에 배선된 매핑이 없으면(런타임 자동 설치) 코드 테이블로 만들어 쓴다.
            // OrderJudge와 DayState가 읽는 값은 id / ingredientName / costPerUse / grillable 네 개뿐이라
            // 애셋 참조 없이도 채점과 집계가 정상 동작한다.
            CoreIngredientData fallback = CounterReturnBridgeFallback.Create(type);
            if (fallback != null)
            {
                ingredientLookup[type] = fallback;
                return fallback;
            }

            Debug.LogWarning($"BurgerAssemblyCounterBridge: no Core.IngredientData mapped for {type}.");
            return null;
        }

        private static CookState ResolveCookState(IngredientPlacement placement)
        {
            if (!placement.hasGrillState) return CookState.NotRequired;
            return placement.grillPhase switch
            {
                PattyGrillPhase.Done => CookState.Perfect,
                PattyGrillPhase.Overcooked => CookState.Burnt,
                _ => CookState.UnderCooked
            };
        }
    }
}
