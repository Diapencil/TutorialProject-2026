using UnityEngine;

namespace SheepSheepBurger.Economy
{
    public static class PlayerPrefsEconomyStore
    {
        private const string SaveKey = "SheepSheepBurger.Economy.State.v1";

        public static PlayerEconomyState LoadOrDefault()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return PlayerEconomyState.CreateNewGame();
            }

            string json = PlayerPrefs.GetString(SaveKey);
            PlayerEconomyState state = JsonUtility.FromJson<PlayerEconomyState>(json);
            if (state == null)
            {
                return PlayerEconomyState.CreateNewGame();
            }

            state.Sanitize();
            return state;
        }

        public static void Save(PlayerEconomyState state)
        {
            if (state == null)
            {
                return;
            }

            state.Sanitize();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
