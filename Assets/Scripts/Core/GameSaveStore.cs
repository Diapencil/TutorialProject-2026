// GameState를 persistentDataPath의 JSON 파일로 안전하게 저장하고 불러온다.
using System;
using System.IO;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    internal static class GameSaveStore
    {
        private const string SaveFileName = "SheepSheepBurgerSave.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool TryLoad(out GameState state)
        {
            state = null;

            try
            {
                if (!File.Exists(SavePath))
                {
                    return false;
                }

                string json = File.ReadAllText(SavePath);
                state = JsonUtility.FromJson<GameState>(json);
                state?.EnsureRuntimeCollections();
                return state != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSaveStore] 저장 파일을 불러오지 못했습니다: {exception.Message}");
                state = null;
                return false;
            }
        }

        public static bool TrySave(GameState state)
        {
            if (state == null)
            {
                return false;
            }

            try
            {
                state.EnsureRuntimeCollections();
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true));

                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                File.Move(temporaryPath, SavePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSaveStore] 게임을 저장하지 못했습니다: {exception.Message}");
                return false;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSaveStore] 저장 파일을 삭제하지 못했습니다: {exception.Message}");
            }
        }
    }
}
