using SheepSheepBurger.Counter;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.Audio
{
    [DisallowMultipleComponent]
    public sealed class CounterAreaBgmDirector : MonoBehaviour
    {
        [Header("카운터 영역")]
        [SerializeField] private string[] counterAreaSceneNames = { "Counter", "ShopScene" };
        [SerializeField] private bool stopWhenLeavingCounterArea = true;

        [Header("하루별 BGM")]
        [SerializeField] private string[] counterBgmIds =
        {
            AudioCueIds.CounterDay01,
            AudioCueIds.CounterDay02,
            AudioCueIds.CounterDay03,
            AudioCueIds.CounterDay04
        };

        [SerializeField, Min(0f)] private float fadeSeconds = 0.35f;
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

        private int playingDay = -1;
        private string playingBgmId;
        private bool ownsCurrentBgm;
        private float nextRefreshTime;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EvaluateActiveScene();
        }

        private void Start()
        {
            EvaluateActiveScene();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + refreshInterval;
            EvaluateActiveScene();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void Refresh()
        {
            EvaluateActiveScene();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            nextRefreshTime = 0f;
            EvaluateScene(scene.name);
        }

        private void EvaluateActiveScene()
        {
            EvaluateScene(SceneManager.GetActiveScene().name);
        }

        private void EvaluateScene(string sceneName)
        {
            if (IsCounterAreaScene(sceneName))
            {
                PlayCounterTrackForCurrentDay();
                return;
            }

            StopCounterTrackIfNeeded();
        }

        private void PlayCounterTrackForCurrentDay()
        {
            if (counterBgmIds == null || counterBgmIds.Length == 0)
            {
                return;
            }

            DayProgressRuntime dayProgress = DayProgressRuntime.GetOrCreate();
            int currentDay = Mathf.Max(1, dayProgress.CurrentDay);
            AudioManager audioManager = AudioManager.GetOrCreate();
            string bgmId = GetBgmIdForDay(currentDay);

            if (string.IsNullOrWhiteSpace(bgmId))
            {
                return;
            }

            bool alreadyPlaying = ownsCurrentBgm &&
                playingDay == currentDay &&
                playingBgmId == bgmId &&
                audioManager.CurrentBgmId == bgmId &&
                audioManager.IsBgmPlaying;

            if (alreadyPlaying)
            {
                return;
            }

            audioManager.PlayBgm(bgmId, false, fadeSeconds);
            playingDay = currentDay;
            playingBgmId = bgmId;
            ownsCurrentBgm = true;
        }

        private void StopCounterTrackIfNeeded()
        {
            if (!stopWhenLeavingCounterArea || !ownsCurrentBgm)
            {
                return;
            }

            AudioManager audioManager = AudioManager.GetOrCreate();
            if (!string.IsNullOrWhiteSpace(playingBgmId) &&
                audioManager.CurrentBgmId == playingBgmId)
            {
                audioManager.StopBgm(fadeSeconds);
            }

            ownsCurrentBgm = false;
        }

        private string GetBgmIdForDay(int day)
        {
            int index = Mathf.Abs(day - 1) % counterBgmIds.Length;
            return counterBgmIds[index];
        }

        private bool IsCounterAreaScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || counterAreaSceneNames == null)
            {
                return false;
            }

            for (int i = 0; i < counterAreaSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, counterAreaSceneNames[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
