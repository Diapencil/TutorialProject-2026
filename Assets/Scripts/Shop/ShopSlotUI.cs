// 상점 그리드의 한 칸. 아이콘/이름/가격 표시와 구매 버튼 상태만 책임진다.
using System;
using SheepSheepBurger.Core;
using SheepSheepBurger.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.Shop
{
    public class ShopSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button purchaseButton;

        [Tooltip("구매 완료 시 덮는 회색 오버레이.")]
        [SerializeField] private GameObject soldOutOverlay;

        [Tooltip("공사중 등 구매 자체가 불가능할 때 덮는 오버레이.")]
        [SerializeField] private GameObject lockedOverlay;

        [Header("문구")]
        [SerializeField] private string unavailableLabel = "구매 불가";

        public ShopTabType Tab { get; private set; }
        public int TargetId { get; private set; }

        public void Setup(ShopTabType tab, int targetId, Sprite icon, string displayName,
                          int cost, bool isPurchased, bool isAvailable,
                          Action<ShopSlotUI> callback)
        {
            Tab = tab;
            TargetId = targetId;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                // 아트 리소스가 아직 없으므로 스프라이트가 없으면 아이콘 칸을 숨긴다.
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (costText != null)
            {
                costText.text = isAvailable ? CurrencyUtil.ToDisplay(cost) : unavailableLabel;
            }

            if (soldOutOverlay != null)
            {
                soldOutOverlay.SetActive(isPurchased);
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!isAvailable);
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = isAvailable && !isPurchased;

                // 슬롯을 재사용하므로 이전 탭의 리스너가 남지 않도록 반드시 먼저 비운다.
                purchaseButton.onClick.RemoveAllListeners();

                if (callback != null)
                {
                    ShopSlotUI self = this;
                    purchaseButton.onClick.AddListener(() => callback(self));
                }
            }
        }

        /// <summary>구매 직후 그리드 전체를 다시 그리지 않고 이 칸만 회색 처리한다.</summary>
        public void MarkPurchased()
        {
            if (soldOutOverlay != null)
            {
                soldOutOverlay.SetActive(true);
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = false;
            }
        }
    }
}
