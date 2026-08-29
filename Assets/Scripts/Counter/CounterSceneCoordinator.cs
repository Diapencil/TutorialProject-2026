using System.Collections;
using System.Collections.Generic;
using SheepSheepBurger.Core;
using SheepSheepBurger.Results;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.Counter
{
    public sealed class CounterSceneCoordinator : MonoBehaviour
    {
        [SerializeField] private CounterSettings settings;
        [SerializeField] private CounterSceneUI ui;
        [Tooltip("씬에 고정 배치된 손님 오브젝트입니다. 주문마다 새로 생성하지 않고 이 오브젝트를 재사용합니다.")]
        [SerializeField] private CustomerPresenter customer;

        private OrderInstance order;
        private DayProgressRuntime dayProgress;
        private bool resolving;

        public bool IsResolvingOrder => resolving;

        private void OnEnable() => CounterSceneSession.BurgerSubmitted += OnBurgerSubmitted;
        private void OnDisable() => CounterSceneSession.BurgerSubmitted -= OnBurgerSubmitted;

        private void Start()
        {
            ui.ConfirmClicked += ConfirmOrder;
            ui.ServeClicked += ServeBurger;
            ui.ClarificationRequested += CounterSceneSession.MarkHintUsed;
            dayProgress = DayProgressRuntime.GetOrCreate();
            ui.SetTop(dayProgress, settings.CustomersPerDay);
            ui.HideOrder();
            if (customer != null) customer.Hide();

            if (dayProgress.IsCurrentDayComplete ||
                dayProgress.ServedCustomerCount >= settings.CustomersPerDay)
            {
                OpenCurrentDayResult();
                return;
            }

            order = CounterSceneSession.ActiveOrder;
            if (!IsValidActiveOrder(order))
            {
                CounterSceneSession.ClearOrder();
                order = null;
            }

            if (order == null) CreateNextCustomer(); else RestoreReturningCustomer();
        }

        private void OnDestroy()
        {
            if (ui == null) return;
            ui.ConfirmClicked -= ConfirmOrder;
            ui.ServeClicked -= ServeBurger;
            ui.ClarificationRequested -= CounterSceneSession.MarkHintUsed;
        }

        private void CreateNextCustomer()
        {
            if (!TryCreateOrder(out order)) return;
            CounterSceneSession.BeginOrder(order);
            SetupCustomer(playEntrance: true);
        }

        private bool TryCreateOrder(out OrderInstance nextOrder)
        {
            var customers = new List<SheepSheepBurger.Core.CustomerData>();
            foreach (var candidate in settings.AvailableCustomers) // TODO settings 수정
                if (candidate != null) customers.Add(candidate);
            var orders = new List<OrderData>();
            foreach (var candidate in settings.AvailableOrders)
                if (CanPrepareOrder(candidate)) orders.Add(candidate);

            if (customers.Count == 0 || orders.Count == 0)
            {
                Debug.LogError("CounterSettings에 현재 해금된 재료로 만들 수 있는 주문과 손님이 필요합니다.");
                nextOrder = null;
                return false;
            }

            var selectedCustomer = customers[Random.Range(0, customers.Count)];
            nextOrder = new OrderInstance
            {
                customer = selectedCustomer,
                order = orders[Random.Range(0, orders.Count)],
                spriteIndex = selectedCustomer.sprites == null || selectedCustomer.sprites.Count == 0
                    ? 0
                    : Random.Range(0, selectedCustomer.sprites.Count),
                patienceRemaining = Mathf.CeilToInt(settings.PatienceSeconds),
                phase = OrderPhase.Ordering
            };
            return true;
        }

        private void OpenCurrentDayResult()
        {
            DayResultLayerController resultLayer = DayResultLayerController.GetOrCreate();
            if (resultLayer != null)
            {
                resultLayer.Open(dayProgress.DayState);
            }
        }

        private static bool IsValidActiveOrder(OrderInstance activeOrder)
        {
            return activeOrder != null &&
                activeOrder.customer != null &&
                activeOrder.order != null &&
                activeOrder.order.recipe != null;
        }

        private static bool CanPrepareOrder(OrderData candidate)
        {
            if (candidate == null || candidate.recipe == null || candidate.recipe.layers == null)
            {
                return false;
            }

            for (int i = 0; i < candidate.recipe.layers.Count; i++)
            {
                RecipeLayer layer = candidate.recipe.layers[i];
                if (layer == null || !ShopProgressBridge.IsCoreIngredientUnlocked(layer.ingredient))
                {
                    return false;
                }
            }

            return true;
        }

        private void RestoreReturningCustomer()
        {
            ApplyCustomerSprite();
            customer.ShowImmediate();

            if (!CounterSceneSession.HasConfirmedOrder && CounterSceneSession.CookedBurger == null)
            {
                ui.RestoreOrder(order, CounterSceneSession.HintUsed);
            }
            else
            {
                ui.HideOrder();
            }

            ui.SetOrderConfirmed(CounterSceneSession.HasConfirmedOrder);
            ui.SetCookedBurgerAvailable(CounterSceneSession.CookedBurger != null);
        }

        private void SetupCustomer(bool playEntrance)
        {
            ApplyCustomerSprite();
            ui.HideOrder();
            ui.SetCookedBurgerAvailable(false);

            if (!playEntrance)
            {
                customer.ShowImmediate();
                return;
            }

            customer.Enter();
            StartCoroutine(ShowOrderAfterCustomerEnters(order));
        }

        private void ApplyCustomerSprite()
        {
            var sprites = order.customer.sprites;
            if (sprites == null || sprites.Count == 0) return;

            customer.SetSprite(sprites[Mathf.Clamp(order.spriteIndex, 0, sprites.Count - 1)]);
        }

        private IEnumerator ShowOrderAfterCustomerEnters(OrderInstance enteringOrder)
        {
            yield return new WaitForSeconds(customer.EnterDuration);

            // 등장 중 주문이 교체되거나 씬이 전환된 경우에는 이전 주문을 표시하지 않는다.
            if (order != enteringOrder || resolving) yield break;

            ui.ShowOrder(enteringOrder);
        }

        private void ConfirmOrder()
        {
            if (resolving || order == null) return;
            CounterSceneSession.ConfirmOrderForCooking();
            ui.SetOrderConfirmed(true);
            SceneManager.LoadScene(settings.CookingSceneName);
        }

        private void OnBurgerSubmitted(BurgerData burger)
        {
            if (order == null || resolving) return;
            ui.SetCookedBurgerAvailable(burger != null);
        }

        private void ServeBurger()
        {
            if (resolving || order == null || CounterSceneSession.CookedBurger == null) return;
            BurgerData submittedBurger = CounterSceneSession.CookedBurger;
            OrderJudgement judgement = OrderJudge.JudgeDetailed(order.order,
                                                                submittedBurger,
                                                                settings.GradeConfig,
                                                                CounterSceneSession.HintUsed);
            StartCoroutine(Resolve(judgement, submittedBurger));
        }

        private IEnumerator Resolve(OrderJudgement judgement, BurgerData submittedBurger)
        {
            if (resolving) yield break;
            resolving = true;
            Grade grade = judgement.grade;
            var reward = OrderJudge.GetReward(order.order, grade, settings.GradeConfig);
            dayProgress.RegisterCustomer(order, submittedBurger, judgement, reward);
            bool isDayComplete = dayProgress.ServedCustomerCount >= settings.CustomersPerDay;
            ui.SetTop(dayProgress, settings.CustomersPerDay);
            ui.SetCookedBurgerAvailable(false);
            yield return ui.ShowResultRoutine(grade, reward, settings.GetReaction(grade, order.order.dialogue));
            yield return new WaitForSeconds(settings.ExitDelaySeconds);
            ui.HideSpeechBubble();
            customer.Exit();
            yield return new WaitForSeconds(settings.ReactionSeconds);
            CounterSceneSession.ClearOrder();
            order = null;
            if (isDayComplete)
            {
                dayProgress.CompleteCurrentDay();
            }
            resolving = false;
            if (!isDayComplete) CreateNextCustomer();
        }
    }
}
