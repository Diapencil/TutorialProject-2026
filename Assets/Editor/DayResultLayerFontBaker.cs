using System;
using SheepSheepBurger.Results;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SheepSheepBurger.EditorTools
{
    [InitializeOnLoad]
    public static class DayResultLayerFontBaker
    {
        private const string ShopFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Shop Korean SDF.asset";
        private const string FullKoreanFontAssetPath = "Assets/Fonts/NanumGothic SDF.asset";

        static DayResultLayerFontBaker()
        {
            EditorApplication.delayCall -= EnsureResultLayerFont;
            EditorApplication.delayCall += EnsureResultLayerFont;
        }

        [MenuItem("SheepSheep/Refresh Result Layer Font")]
        public static void EnsureResultLayerFont()
        {
            TMP_FontAsset shopFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ShopFontAssetPath);
            TMP_FontAsset fullKoreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FullKoreanFontAssetPath);

            if (shopFont == null && fullKoreanFont == null)
            {
                return;
            }

            bool changed = false;
            changed |= AddCharactersIfNeeded(shopFont, DayResultLayerController.RequiredFontCharacters);
            changed |= AddCharactersIfNeeded(fullKoreanFont, DayResultLayerController.RequiredFontCharacters);
            changed |= AddFallbackIfNeeded(shopFont, fullKoreanFont);

            if (!changed)
            {
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool AddCharactersIfNeeded(TMP_FontAsset fontAsset, string characters)
        {
            if (fontAsset == null || string.IsNullOrEmpty(characters))
            {
                return false;
            }

            _ = fontAsset.characterLookupTable;

            if (fontAsset.HasCharacters(characters, out _))
            {
                return false;
            }

            AtlasPopulationMode originalMode = fontAsset.atlasPopulationMode;
            int originalCharacterCount = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            try
            {
                if (!fontAsset.TryAddCharacters(characters, out string missingCharacters) &&
                    !string.IsNullOrEmpty(missingCharacters))
                {
                    Debug.LogWarning($"[DayResultLayerFontBaker] 결과창 폰트에 넣지 못한 문자가 있습니다: {missingCharacters}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[DayResultLayerFontBaker] 결과창 폰트 보정 중 오류: {exception.Message}");
            }
            finally
            {
                fontAsset.atlasPopulationMode = originalMode;
            }

            int updatedCharacterCount = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
            if (updatedCharacterCount == originalCharacterCount)
            {
                return false;
            }

            EditorUtility.SetDirty(fontAsset);
            return true;
        }

        private static bool AddFallbackIfNeeded(TMP_FontAsset targetFont, TMP_FontAsset fallbackFont)
        {
            if (targetFont == null || fallbackFont == null || targetFont == fallbackFont)
            {
                return false;
            }

            targetFont.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();

            if (targetFont.fallbackFontAssetTable.Contains(fallbackFont))
            {
                return false;
            }

            targetFont.fallbackFontAssetTable.Add(fallbackFont);
            EditorUtility.SetDirty(targetFont);
            return true;
        }
    }
}
