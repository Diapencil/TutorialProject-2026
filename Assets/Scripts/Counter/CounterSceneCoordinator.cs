using System.Collections;
using System.Collections.Generic;
using SheepSheepBurger.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lee.Counter
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

        private void OnEnable() => CounterSceneSession.BurgerSubmitted += OnBurgerSubmitted;
        private void OnDisable() => CounterSceneSession.BurgerSubmitted -= OnBurgerSubmitted;

        private void Start()
        {
            ui.ConfirmClicked += ConfirmOrder;
            ui.ServeClicked += ServeBurger;
            dayProgress = DayProgressRuntime.GetOrCreate();
            ui.SetTop(dayProgress, settings.CustomersPerDay);
            customer.Hide();

            if (dayProgress.ServedCustomerCount >= settings.CustomersPerDay) return; // TODO settings 수정
            order = CounterSceneSession.ActiveOrder;
            if (order == null) CreateNextCustomer(); else RestoreReturningCustomer();
        }

        private void OnDestroy()
        {
            if (ui == null) return;
            ui.ConfirmClicked -= ConfirmOrder;
            ui.ServeClicked -= ServeBurger;
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
                if (candidate != null && candidate.recipe != null) orders.Add(candidate);

            if (customers.Count == 0 || orders.Count == 0)
            {
                Debug.LogError("CounterSettings requires at least one Core CustomerData and OrderData.");
                nextOrder = null;
                return false;
            }

            var selectedCustomer = customers[Random.Range(0, customers.Count)];
            nextOrder = new OrderInstance
            {
                customer = selectedCustomer,
                order = orders[Random.Range(0, orders.Count)], // TODO 현재 가지고 있는 재료로 제작 가능한 버거 중에서 선택
                spriteIndex = selectedCustomer.sprites == null || selectedCustomer.sprites.Count == 0
                    ? 0
                    : Random.Range(0, selectedCustomer.sprites.Count),
                patienceRemaining = Mathf.CeilToInt(settings.PatienceSeconds),
                phase = OrderPhase.Ordering
            };
            return true;
        }

        private void RestoreReturningCustomer()
        {
            // Cooking 씬에서 돌아온 손님은 이미 카운터에 서 있는 상태다.
            // 따라서 등장 연출과 주문 대사를 다시 표시하지 않는다.
            SetupCustomer(playEntrance: false);
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
            ui.SetOrderConfirmed(CounterSceneSession.HasConfirmedOrder);
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
            StartCoroutine(Resolve(OrderJudge.Judge(order.order, CounterSceneSession.CookedBurger)));
        }

        private IEnumerator Resolve(Grade grade)
        {
            if (resolving) yield break;
            resolving = true;
            var reward = OrderJudge.GetReward(order.order, grade);
            dayProgress.RegisterCustomer(reward);
            ui.SetTop(dayProgress, settings.CustomersPerDay);
            ui.SetCookedBurgerAvailable(false);
            ui.ShowResult(grade, reward, settings.GetReaction(grade));
            customer.Exit();
            yield return new WaitForSeconds(settings.ReactionSeconds);
            CounterSceneSession.ClearOrder();
            order = null;
            resolving = false;
            if (dayProgress.ServedCustomerCount < settings.CustomersPerDay) CreateNextCustomer();
        }
    }
}
