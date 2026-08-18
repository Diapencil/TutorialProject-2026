using System;
using SheepSheepBurger.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lee.Counter
{
    public sealed class CounterSceneUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text speechBubbleText;
        [SerializeField] private TMP_Text patienceText;
        [SerializeField] private Button confirmOrderButton;
        [SerializeField] private Button whatButton;
        [SerializeField] private Button serveButton;
        [SerializeField] private GameObject servingBurgerRoot;
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

        public void ShowOrder(OrderInstance order)
        {
            var dialogue = order.order.dialogue;
            speechBubbleText.text = dialogue != null && dialogue.orderLines != null && dialogue.orderLines.Count > 0
                ? dialogue.orderLines[0]
                : order.order.recipe.recipeName;
            clarificationRequest = dialogue != null ? dialogue.hintLine : string.Empty;
            clarificationShown = false;
            confirmOrderButton.interactable = true;
            whatButton.interactable = !string.IsNullOrWhiteSpace(clarificationRequest);
            resultRoot.SetActive(false);
        }

        public void HideOrder()
        {
            speechBubbleText.text = string.Empty;
            clarificationRequest = string.Empty;
            clarificationShown = false;
            confirmOrderButton.interactable = false;
            whatButton.interactable = false;
            resultRoot.SetActive(false);
        }

        private void ShowClarification()
        {
            if (clarificationShown) return;
            clarificationShown = true;
            speechBubbleText.text = clarificationRequest;
            whatButton.interactable = false;
        }

        public void SetTop(DayProgressRuntime day, int customersPerDay)
        {
            dayText.text = $"D + {day.CurrentDay}";
            revenueText.text = $"{day.DailyRevenue:N0} C";
            progressText.text = $"{day.ServedCustomerCount} / {customersPerDay}";
        }

        public void SetPatience(float seconds) => patienceText.text = $"Patience: {Mathf.CeilToInt(seconds)}s";
        public void SetOrderConfirmed(bool confirmed)
        {
            confirmOrderButton.interactable = !confirmed;
            whatButton.interactable = !confirmed && !clarificationShown && !string.IsNullOrWhiteSpace(clarificationRequest);
        }
        public void SetCookedBurgerAvailable(bool available)
        {
            servingBurgerRoot.SetActive(available);
            serveButton.interactable = available;
        }
        public void ShowResult(Grade result, int reward, string reaction)
        {
            resultRoot.SetActive(true);
            resultText.text = $"{result}\nReward: {reward:N0}\n{reaction}";
            speechBubbleText.text = reaction;
        }
    }
}
