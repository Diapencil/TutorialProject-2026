using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lee.Counter
{
    /// <summary>카운터 씬의 흐름을 조율합니다. 조리, 저장, 결과 씬의 구현은 소유하지 않습니다.</summary>
    public sealed class CounterSceneCoordinator : MonoBehaviour
    {
        [SerializeField] private CounterSettings settings;
        [SerializeField] private CounterDayState dayState;
        [SerializeField] private CounterSceneUI ui;
        [SerializeField] private CustomerPresenter customerPrefab;
        [SerializeField] private Transform customerSpawnPoint;

        private OrderData order;
        private CustomerPresenter customer;
        private DayProgressRuntime dayProgress;
        private bool resolving;

        private void OnEnable() => CounterSceneSession.BurgerSubmitted += OnBurgerSubmitted;
        private void OnDisable() => CounterSceneSession.BurgerSubmitted -= OnBurgerSubmitted;
        private void Start()
        {
            ui.ConfirmClicked += ConfirmOrder;
            ui.ServeClicked += ServeBurger;
            dayProgress = DayProgressRuntime.GetOrCreate(dayState);
            ui.SetTop(dayProgress, settings.CustomersPerDay);

            if (dayProgress.ServedCustomerCount >= settings.CustomersPerDay)
            {
                Debug.Log("하루가 종료되었습니다. Day Result 씬 연결이 필요합니다.");
                return;
            }
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
            ui.SetPatience(order.RemainingPatience);
            if (order.IsExpired) StartCoroutine(Resolve(ServiceResult.Timeout));
        }

        private void CreateNextCustomer()
        {
            if (!TryCreateOrder(out order)) return;
            CounterSceneSession.BeginOrder(order);
            SetupCustomer();
        }
        private bool TryCreateOrder(out OrderData nextOrder)
        {
            var eligibleCustomers = new System.Collections.Generic.List<CustomerData>();
            foreach (var availableCustomer in settings.AvailableCustomers)
            {
                if (availableCustomer != null && availableCustomer.PreferredRecipes.Count > 0)
                    eligibleCustomers.Add(availableCustomer);
            }

            if (eligibleCustomers.Count == 0)
            {
                Debug.LogError("CounterSettings에 선호 레시피가 설정된 손님이 없습니다.");
                nextOrder = null;
                return false;
            }

            var customerData = eligibleCustomers[Random.Range(0, eligibleCustomers.Count)];
            if (!customerData.TryGetRandomPreferredRecipe(out var recipe))
            {
                Debug.LogError($"'{customerData.name}' 손님에게 유효한 선호 레시피가 없습니다.", customerData);
                nextOrder = null;
                return false;
            }

            nextOrder = new OrderData(customerData, recipe, settings.PatienceSeconds);
            return true;
        }
        private void RestoreReturningCustomer()
        {
            SetupCustomer();
            var hasBurger = CounterSceneSession.CookedBurger != null;
            ui.SetOrderConfirmed(CounterSceneSession.HasConfirmedOrder);
            ui.SetCookedBurgerAvailable(hasBurger);
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
            StartCoroutine(Resolve(OrderJudge.Judge(order, CounterSceneSession.CookedBurger)));
        }
        private IEnumerator Resolve(ServiceResult result)
        {
            if (resolving) yield break;
            resolving = true;
            var reward = OrderJudge.GetReward(order, result);
            dayProgress.RegisterCustomer(reward);
            ui.SetTop(dayProgress, settings.CustomersPerDay);
            ui.SetCookedBurgerAvailable(false);
            ui.ShowResult(result, reward, settings.GetReaction(result));
            customer.Exit();
            yield return new WaitForSeconds(settings.ReactionSeconds);
            Destroy(customer.gameObject);
            CounterSceneSession.ClearOrder();
            order = null;
            resolving = false;
            if (dayProgress.ServedCustomerCount >= settings.CustomersPerDay)
            {
                Debug.Log("하루가 종료되었습니다. Day Result 씬 연결이 필요합니다.");
                yield break;
            }
            CreateNextCustomer();
        }
    }
}
