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
        [Tooltip("대사가 표시되는 말풍선 패널입니다. 대사가 떠 있지 않을 때는 비활성화됩니다.")]
        [SerializeField] private GameObject customerAreaPanel;
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
            clarificationRequest = dialogue != null ? dialogue.hintLine : string.Empty;
            clarificationShown = false;
            var hasClarification = !string.IsNullOrWhiteSpace(clarificationRequest);
            // 주문 대사가 다 출력되기 전에는 버튼을 숨겨둔다.
            SetConfirmOrderButtonInteractable(false);
            SetWhatButtonInteractable(false);
            resultRoot.SetActive(false);
            if (customerAreaPanel != null) customerAreaPanel.SetActive(true);
            TypeText(line, () =>
            {
                SetConfirmOrderButtonInteractable(true);
                SetWhatButtonInteractable(hasClarification);
            });
        }

        public void HideOrder()
        {
            HideSpeechBubble();
            clarificationRequest = string.Empty;
            clarificationShown = false;
            SetConfirmOrderButtonInteractable(false);
            SetWhatButtonInteractable(false);
            resultRoot.SetActive(false);
        }

        /// <summary>말풍선과 대사를 감춘다. 대사가 떠 있지 않은 동안(시작 시점, 수령 직후 등)에는 CustomerArea 패널 자체를 비활성화한다.
        /// 결과 텍스트(resultText)도 말풍선과 같은 시점에 함께 사라진다.</summary>
        public void HideSpeechBubble()
        {
            StopTyping();
            speechBubbleText.text = string.Empty;
            if (customerAreaPanel != null) customerAreaPanel.SetActive(false);
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

        private void TypeText(string text, Action onComplete = null)
        {
            StopTyping();
            typingCoroutine = StartCoroutine(TypeTextRoutine(text, onComplete));
        }

        private void StopTyping()
        {
            if (typingCoroutine == null) return;
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        private IEnumerator TypeTextRoutine(string text, Action onComplete)
        {
            speechBubbleText.text = string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                for (var i = 0; i < text.Length; i++)
                {
                    speechBubbleText.text += text[i];
                    yield return new WaitForSeconds(typingCharInterval);
                }
            }

            typingCoroutine = null;
            onComplete?.Invoke();
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
        /// <summary>결과 표시 후 수령 대사가 다 출력될 때까지 대기하는 코루틴입니다. 호출부에서 대사 출력이 끝난 뒤의 연출(예: 손님 퇴장)을 이어붙일 때 사용합니다.</summary>
        public IEnumerator ShowResultRoutine(Grade result, int reward, string reaction)
        {
            resultRoot.SetActive(true);
            resultText.text = $"{result}\n+{CurrencyUtil.ToDisplay(reward)}";
            PlayResultRiseAnimation();

            if (customerAreaPanel != null) customerAreaPanel.SetActive(true);
            var typingDone = false;
            TypeText(reaction, () => typingDone = true);
            while (!typingDone) yield return null;
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
