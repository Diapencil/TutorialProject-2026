using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.Settings
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SettingsArtSliderHandle : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private RectTransform fullCanvasHandle;
        [SerializeField] private float sourceCenterX;
        [SerializeField] private float minimumCenterX;
        [SerializeField] private float maximumCenterX;

        private float lastValue = float.NaN;

        public void Configure(
            Slider boundSlider,
            RectTransform boundHandle,
            float handleSourceCenterX,
            float minimumX,
            float maximumX)
        {
            Unhook();
            slider = boundSlider;
            fullCanvasHandle = boundHandle;
            sourceCenterX = handleSourceCenterX;
            minimumCenterX = minimumX;
            maximumCenterX = maximumX;
            Hook();
            Refresh();
        }

        private void OnEnable()
        {
            Hook();
            Refresh();
        }

        private void OnDisable()
        {
            Unhook();
        }

        private void Update()
        {
            if (slider != null && !Mathf.Approximately(lastValue, slider.value))
            {
                Refresh();
            }
        }

        private void OnValidate()
        {
            Refresh();
        }

        private void Hook()
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(HandleValueChanged);
            slider.onValueChanged.AddListener(HandleValueChanged);
        }

        private void Unhook()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(float value)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (slider == null || fullCanvasHandle == null)
            {
                return;
            }

            float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value);
            float targetCenterX = Mathf.Lerp(minimumCenterX, maximumCenterX, normalized);
            Vector2 position = fullCanvasHandle.anchoredPosition;
            position.x = targetCenterX - sourceCenterX;
            position.y = 0f;
            fullCanvasHandle.anchoredPosition = position;
            lastValue = slider.value;
        }
    }
}
