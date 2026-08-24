using System;
using System.Collections;
using SheepSheepBurger.Core;
using SheepSheepBurger.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.Counter
{
    public sealed class CounterSceneUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text speechBubbleText;
        [SerializeField] private TMP_Text patienceText;
        [SerializeField] private Button confirmOrderButton;
        [SerializeField] private TMP_Text confirmOrderButtonText;
        [SerializeField] private Button whatButton;
        [SerializeField] private TMP_Text whatButtonText;
        [SerializeField] private BurgerDragServeHandle burgerDragHandle;
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private float typingCharInterval = 0.03f;
        [SerializeField] private float resultRiseDistance = 40f;
        [SerializeField] private float resultRiseDuration = 0.35f;

        public event Action ConfirmClicked;
        public event Action ServeClicked;
        /// <summary>"네?" 버튼(힌트 요청)을 눌렀을 때 발생. 서빙 판정에서 Perfect/Good 배제에 쓰인다.</summary>
        public event Action ClarificationRequested;
        private string clarificationRequest;
        private bool clarificationShown;
        private Coroutine typingCoroutine;
        private Coroutine resultRiseCoroutine;
        private Vector2 resultTextBasePosition;

        private void Awake()
        {
            confirmOrderButton.onClick.AddListener(() => ConfirmClicked?.Invoke());
            whatButton.onClick.AddListener(ShowClarification);
            burgerDragHandle.Dropped += () => ServeClicked?.Invoke();
            resultTextBasePosition = resultText.rectTransform.anchoredPosition;
        }

        private void OnDestroy()
        {
            confirmOrderButton.onClick.RemoveAllListeners();
            whatButton.onClick.RemoveAllListeners();
        }

        public void ShowOrder(OrderInstance order)
        {
            var dialogue = order.order.dialogue;
            var line = dialogue != null && dialogue.orderLines != null && dialogue.orderLines.Count > 0
                ? dialogue.orderLines[UnityEngine.Random.Range(0, dialogue.orderLines.Count)]
                : order.order.recipe.recipeName;
            TypeText(line);
            clarificationRequest = dialogue != null ? dialogue.hintLine : string.Empty;
            clarificationShown = false;
            SetConfirmOrderButtonInteractable(true);
            SetWhatButtonInteractable(!string.IsNullOrWhiteSpace(clarificationRequest));
            resultRoot.SetActive(false);
        }

        public void HideOrder()
        {
            StopTyping();
            speechBubbleText.text = string.Empty;
            clarificationRequest = string.Empty;
            clarificationShown = false;
            SetConfirmOrderButtonInteractable(false);
            SetWhatButtonInteractable(false);
            resultRoot.SetActive(false);
        }

        private void ShowClarification()
        {
            if (clarificationShown) return;
            clarificationShown = true;
            ClarificationRequested?.Invoke();
            TypeText(clarificationRequest);
            SetWhatButtonInteractable(false);
        }

        private void SetConfirmOrderButtonInteractable(bool interactable)
        {
            confirmOrderButton.interactable = interactable;
            confirmOrderButtonText.gameObject.SetActive(interactable);
        }

        private void SetWhatButtonInteractable(bool interactable)
        {
            whatButton.interactable = interactable;
            whatButtonText.gameObject.SetActive(interactable);
        }

        private void TypeText(string text)
        {
            StopTyping();
            typingCoroutine = StartCoroutine(TypeTextRoutine(text));
        }

        private void StopTyping()
        {
            if (typingCoroutine == null) return;
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        private IEnumerator TypeTextRoutine(string text)
        {
            speechBubbleText.text = string.Empty;
            if (string.IsNullOrEmpty(text)) yield break;

            for (var i = 0; i < text.Length; i++)
            {
                speechBubbleText.text += text[i];
                yield return new WaitForSeconds(typingCharInterval);
            }

            typingCoroutine = null;
        }

        public void SetTop(DayProgressRuntime day, int customersPerDay)
        {
            dayText.text = $"D + {day.CurrentDay}";
            // 금액은 10배 정수로 저장되므로 표시할 때만 CurrencyUtil로 환산한다.
            revenueText.text = CurrencyUtil.ToDisplay(day.DailyRevenue);
            progressText.text = $"{day.ServedCustomerCount} / {customersPerDay}";
        }

        public void SetPatience(float seconds) => patienceText.text = $"Patience: {Mathf.CeilToInt(seconds)}s";
        public void SetOrderConfirmed(bool confirmed)
        {
            SetConfirmOrderButtonInteractable(!confirmed);
            SetWhatButtonInteractable(!confirmed && !clarificationShown && !string.IsNullOrWhiteSpace(clarificationRequest));
        }
        public void SetCookedBurgerAvailable(bool available) => burgerDragHandle.gameObject.SetActive(available);
        public void ShowResult(Grade result, int reward, string reaction)
        {
            resultRoot.SetActive(true);
            resultText.text = $"{result}\n+{CurrencyUtil.ToDisplay(reward)}";
            TypeText(reaction);
            PlayResultRiseAnimation();
        }

        private void PlayResultRiseAnimation()
        {
            if (resultRiseCoroutine != null) StopCoroutine(resultRiseCoroutine);
            resultRiseCoroutine = StartCoroutine(ResultRiseRoutine());
        }

        // 빠르게 올라가다 점점 느려지도록 ease-out(quad) 곡선을 사용한다.
        private IEnumerator ResultRiseRoutine()
        {
            var rt = resultText.rectTransform;
            rt.anchoredPosition = resultTextBasePosition;

            var elapsed = 0f;
            while (elapsed < resultRiseDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / resultRiseDuration);
                var eased = 1f - (1f - t) * (1f - t);
                rt.anchoredPosition = resultTextBasePosition + Vector2.up * (resultRiseDistance * eased);
                yield return null;
            }

            rt.anchoredPosition = resultTextBasePosition + Vector2.up * resultRiseDistance;
            resultRiseCoroutine = null;
        }
    }
}
