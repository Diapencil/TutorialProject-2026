using SheepSheepBurger.Util;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Runtime/Game Startup Settings", fileName = ResourceName)]
    public sealed class GameStartupSettings : ScriptableObject
    {
        private const string ResourceName = "GameStartupSettings";

        [Header("Editor Play Mode")]
        [Tooltip("켜면 에디터 플레이도 실제 플레이어처럼 저장된 진행 상태를 불러오고 자동 저장합니다.")]
        [SerializeField] private bool useSavedStateInEditor;
        [Tooltip("useSavedStateInEditor를 끈 상태에서 에디터 플레이를 시작할 날짜입니다.")]
        [Min(1), SerializeField] private int editorStartDay = 1;
        [Tooltip("useSavedStateInEditor를 끈 상태에서 시작할 보유 금액(C 단위)입니다. 예: 9999 = 9999.0C")]
        [Min(0f), SerializeField] private float editorStartingGold = 9999f;
        [Tooltip("개발자 시작 모드에서 플레이가 저장 파일을 덮어쓰지 않게 합니다.")]
        [SerializeField] private bool disableAutoSaveInEditorDevMode = true;

        public bool UseSavedStateInEditor => useSavedStateInEditor;
        public int EditorStartDay => Mathf.Max(1, editorStartDay);
        public int EditorStartingGoldStored => CurrencyUtil.ToStored(editorStartingGold);
        public bool DisableAutoSaveInEditorDevMode => disableAutoSaveInEditorDevMode;

        public static GameStartupSettings LoadDefault()
        {
            return Resources.Load<GameStartupSettings>(ResourceName);
        }

        public GameState CreateEditorStartState()
        {
            GameState gameState = new GameState
            {
                currentDay = EditorStartDay,
                gold = EditorStartingGoldStored,
                currentDayState = DayState.CreateForDay(EditorStartDay)
            };
            gameState.EnsureRuntimeCollections();
            return gameState;
        }
    }
}
