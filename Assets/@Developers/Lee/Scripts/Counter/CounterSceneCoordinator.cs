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
        [SerializeField] private CustomerPresenter customerPrefab;
        [SerializeField] private Transform customerSpawnPoint;

        private OrderInstance order;
        private CustomerPresenter customer;
        private DayProgressRuntime dayProgress;
        private bool resolving;
        private float remainingPatience;

        private void OnEnable() => CounterSceneSession.BurgerSubmitted += OnBurgerSubmitted;
        private void OnDisable() => CounterSceneSession.BurgerSubmitted -= OnBurgerSubmitted;

        private void Start()
        {
            ui.ConfirmClicked += ConfirmOrder;
            ui.ServeClicked += ServeBurger;
            dayProgress = DayProgressRuntime.GetOrCreate();
            ui.SetTop(dayProgress, settings.CustomersPerDay);

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

        private void Update()
        {
            if (order == null || resolving) return;
            remainingPatience = Mathf.Max(0, remainingPatience - Time.deltaTime);
            order.patienceRemaining = Mathf.CeilToInt(remainingPatience);
            ui.SetPatience(remainingPatience);
            if (remainingPatience <= 0) StartCoroutine(Resolve(Grade.Bad));
        }

        private void CreateNextCustomer()
        {
            if (!TryCreateOrder(out order)) return;
            CounterSceneSession.BeginOrder(order);
            remainingPatience = order.patienceRemaining;
            SetupCustomer();
        }

        private bool TryCreateOrder(out OrderInstance nextOrder)
        {
            var customers = new List<CustomerData>();
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
                spriteIndex = selectedCustomer.spritePath == null || selectedCustomer.spritePath.Count == 0
                    ? 0
                    : Random.Range(0, selectedCustomer.spritePath.Count),
                patienceRemaining = Mathf.CeilToInt(settings.PatienceSeconds),
                phase = OrderPhase.Ordering
            };
            return true;
        }

        private void RestoreReturningCustomer()
        {
            remainingPatience = order.patienceRemaining;
            SetupCustomer();
            ui.SetOrderConfirmed(CounterSceneSession.HasConfirmedOrder);
            ui.SetCookedBurgerAvailable(CounterSceneSession.CookedBurger != null);
        }

        private void SetupCustomer()
        {
            customer = Instantiate(customerPrefab, customerSpawnPoint);
            customer.Enter();
            ui.ShowOrder(order);
            ui.SetOrderConfirmed(false);
            ui.SetCookedBurgerAvailable(false);
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
            Destroy(customer.gameObject);
            CounterSceneSession.ClearOrder();
            order = null;
            resolving = false;
            if (dayProgress.ServedCustomerCount < settings.CustomersPerDay) CreateNextCustomer();
        }
    }
}
