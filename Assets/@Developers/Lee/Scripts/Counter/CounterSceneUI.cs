using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lee.Counter
{
    /// <summary>UI 표시와 버튼 입력만 담당합니다. 게임 규칙은 Coordinator에 두지 않습니다.</summary>
    public sealed class CounterSceneUI : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private TMP_Text progressText;
        [Header("Customer / Order")]
        [SerializeField] private TMP_Text speechBubbleText;
        [SerializeField] private TMP_Text orderTitleText;
        [SerializeField] private TMP_Text ingredientListText;
        [SerializeField] private TMP_Text patienceText;
        [SerializeField] private Button confirmOrderButton;
        [SerializeField] private Button serveButton;
        [SerializeField] private GameObject servingBurgerRoot;
        [Header("Result")]
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TMP_Text resultText;

        public event Action ConfirmClicked;
        public event Action ServeClicked;

        private void Awake()
        {
            confirmOrderButton.onClick.AddListener(() => ConfirmClicked?.Invoke());
            serveButton.onClick.AddListener(() => ServeClicked?.Invoke());
        }
        private void OnDestroy()
        {
            confirmOrderButton.onClick.RemoveAllListeners();
            serveButton.onClick.RemoveAllListeners();
        }

        public void ShowOrder(OrderData order)
        {
            orderTitleText.text = order.RequestedRecipe.RecipeName;
            speechBubbleText.text = order.RequestedRecipe.CustomerRequest;
            var builder = new StringBuilder();
            foreach (var ingredient in order.RequestedRecipe.Ingredients) builder.AppendLine("- " + ingredient);
            ingredientListText.text = builder.ToString();
            resultRoot.SetActive(false);
        }
        public void SetTop(DayProgressRuntime day, int customersPerDay)
        {
            dayText.text = $"{day.CurrentDay}일차";
            revenueText.text = $"수익: {day.DailyRevenue:N0}";
            progressText.text = $"손님: {day.ServedCustomerCount}/{customersPerDay}";
        }
        public void SetPatience(float seconds) => patienceText.text = $"남은 시간: {Mathf.CeilToInt(seconds)}초";
        public void SetOrderConfirmed(bool confirmed) => confirmOrderButton.interactable = !confirmed;
        public void SetCookedBurgerAvailable(bool available)
        {
            servingBurgerRoot.SetActive(available);
            serveButton.interactable = available;
        }
        public void ShowResult(ServiceResult result, int reward, string reaction)
        {
            resultRoot.SetActive(true);
            resultText.text = $"{result}\n보상: {reward:N0}\n{reaction}";
            speechBubbleText.text = reaction;
        }
    }
}
