using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonClickSoundEmitter : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string sfxId = AudioCueIds.UiClick;
        [SerializeField] private bool requireInteractable = true;
        [SerializeField] private string[] excludedSceneNames = { "Cooking", "BurgerAssembly" };

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Configure(string clickSfxId, string[] sceneExclusions)
        {
            sfxId = string.IsNullOrWhiteSpace(clickSfxId) ? AudioCueIds.UiClick : clickSfxId;
            excludedSceneNames = sceneExclusions ?? excludedSceneNames;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!ShouldPlayClickSound())
            {
                return;
            }

            AudioManager.GetOrCreate().PlaySfx(sfxId);
        }

        private bool ShouldPlayClickSound()
        {
            if (AudioSceneFilter.IsActiveSceneExcluded(excludedSceneNames) ||
                AudioSceneFilter.IsExcludedScene(gameObject.scene.name, excludedSceneNames))
            {
                return false;
            }

            if (!requireInteractable)
            {
                return true;
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button == null || button.IsInteractable();
        }
    }
}
