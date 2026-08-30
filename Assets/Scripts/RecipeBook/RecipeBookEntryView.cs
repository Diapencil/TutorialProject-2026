using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.RecipeBook
{
    /// <summary>
    /// 도감 격자의 항목 1칸.
    ///  - 해금 : 완성 음식 이미지 + 레시피 이름
    ///  - 미해금 : "???"  (버튼이 비활성이라 눌러도 반응 없음)
    ///
    /// 이 스크립트가 붙은 작은 Button 오브젝트를 프리팹으로 만들어
    /// RecipeBookLayerController 의 entryPrefab 칸에 넣는다.
    ///
    /// 특정 스키마 타입에 의존하지 않는다. 컨트롤러가 이름/해금여부/콜백만 넘겨준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RecipeBookEntryView : MonoBehaviour
    {
        [Header("표시")]
        [SerializeField] private Button button;
        [SerializeField] private Image artwork;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject lockedBadge; // 자물쇠 아이콘 등 (선택)

        [Header("색상")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.35f);

        [Tooltip("미해금 항목에 표시할 문자열")]
        [SerializeField] private string lockedText = "???";

        private Action clickCallback;

        public void Bind(string displayName, Sprite illustration, bool unlocked, Action onClick)
        {
            clickCallback = onClick;

            EnsureBindings();

            if (artwork != null)
            {
                artwork.sprite = unlocked ? illustration : null;
                artwork.enabled = unlocked && illustration != null;
                artwork.color = unlocked ? Color.white : lockedColor;
            }

            if (label != null)
            {
                label.text = unlocked ? displayName : lockedText;
                label.color = unlocked ? unlockedColor : lockedColor;
            }

            if (lockedBadge != null)
            {
                lockedBadge.SetActive(!unlocked);
            }

            if (button != null)
            {
                button.interactable = unlocked; // 미해금이면 클릭 자체가 안 됨
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            clickCallback?.Invoke();
        }

        private void EnsureBindings()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }

            if (artwork == null)
            {
                Transform artworkTransform = transform.Find("Artwork");
                if (artworkTransform != null)
                {
                    artwork = artworkTransform.GetComponent<Image>();
                }
            }
        }
    }
}
