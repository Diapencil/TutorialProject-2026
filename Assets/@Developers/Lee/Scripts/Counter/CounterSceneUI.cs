using System;
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
        [SerializeField] private TMP_Text patienceText;
        [SerializeField] private Button confirmOrderButton;
        [SerializeField] private Button whatButton;
        [SerializeField] private Button serveButton;
        [SerializeField] private GameObject servingBurgerRoot;
        [Header("Result")]
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TMP_Text resultText;

        public event Action ConfirmClicked;
        public event Action ServeClicked;

        private string clarificationRequest;
        private bool clarificationShown;

        private void Awake()
        {
            confirmOrderButton.onClick.AddListener(() => ConfirmClicked?.Invoke());
            whatButton.onClick.AddListener(ShowClarification);
            serveButton.onClick.AddListener(() => ServeClicked?.Invoke());
        }
        private void OnDestroy()
        {
            confirmOrderButton.onClick.RemoveAllListeners();
            whatButton.onClick.RemoveAllListeners();
            serveButton.onClick.RemoveAllListeners();
        }

        public void ShowOrder(OrderData order)
        {
            speechBubbleText.text = order.CustomerRequest;
            clarificationRequest = order.CustomerClarificationRequest;
            clarificationShown = false;
            confirmOrderButton.interactable = true;
            whatButton.interactable = true;
            resultRoot.SetActive(false);
        }

        private void ShowClarification()
        {
            if (clarificationShown) return;

            clarificationShown = true;
            speechBubbleText.text = clarificationRequest;
            whatButton.interactable = false;
            confirmOrderButton.interactable = true;
        }
        public void SetTop(DayProgressRuntime day, int customersPerDay)
        {
            dayText.text = $"{day.CurrentDay}일차";
            revenueText.text = $"수익: $ {day.DailyRevenue:N0}";
            progressText.text = $"손님: {day.ServedCustomerCount}/{customersPerDay}";
        }
        public void SetPatience(float seconds) => patienceText.text = $"남은 시간: {Mathf.CeilToInt(seconds)}초";
        public void SetOrderConfirmed(bool confirmed)
        {
            confirmOrderButton.interactable = !confirmed;
            whatButton.interactable = !confirmed && !clarificationShown;
        }
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
