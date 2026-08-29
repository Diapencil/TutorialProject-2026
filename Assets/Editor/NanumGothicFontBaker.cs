using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SheepSheepBurger.Results;
using SheepSheepBurger.Settings;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace SheepSheepBurger.EditorTools
{
    public static class NanumGothicFontBaker
    {
        public const string SourceFontPath = "Assets/Fonts/NanumGothic.ttf";
        public const string FontAssetPath = "Assets/Fonts/NanumGothic SDF.asset";

        private const string TemporaryFontAssetPath = "Assets/Fonts/__NanumGothicRebuilt.asset";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 2048;

        private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".asset", ".cs", ".csv", ".json", ".prefab", ".txt", ".unity", ".uss", ".uxml"
        };

        [MenuItem("SheepSheep/Fonts/Rebuild and Apply NanumGothic")]
        public static void RebuildAndApplyNanumGothic()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException($"나눔고딕 원본 폰트를 찾을 수 없습니다: {SourceFontPath}");
            }

            string originalGuid = AssetDatabase.AssetPathToGUID(FontAssetPath);
            if (string.IsNullOrEmpty(originalGuid))
            {
                throw new InvalidOperationException($"기존 나눔고딕 SDF 메타 파일을 찾을 수 없습니다: {FontAssetPath}.meta");
            }

            TMP_FontAsset rebuiltFont = CreateBakedFont(sourceFont, CollectProjectCharacters());
            ReplaceFontAssetFile(rebuiltFont);

            TMP_FontAsset nanumGothic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (nanumGothic == null)
            {
                throw new InvalidOperationException("재생성한 나눔고딕 SDF를 불러오지 못했습니다.");
            }

            ConfigureFontAsset(nanumGothic);
            ConfigureTmpSettings(nanumGothic);
            ApplyToSettingsPresets(nanumGothic);
            int prefabCount = ApplyToPrefabs(nanumGothic);
            int sceneCount = ApplyToScenes(nanumGothic);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string updatedGuid = AssetDatabase.AssetPathToGUID(FontAssetPath);
            if (!string.Equals(originalGuid, updatedGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("나눔고딕 SDF GUID가 변경되어 기존 참조를 유지할 수 없습니다.");
            }

            if (!nanumGothic.HasCharacters(DayResultLayerController.RequiredFontCharacters, out List<char> missingCharacters))
            {
                Debug.LogWarning($"[NanumGothicFontBaker] 나눔고딕에 포함되지 않은 결과창 문자가 있습니다: {new string(missingCharacters.ToArray())}");
            }

            Debug.Log($"[NanumGothicFontBaker] 나눔고딕 고정 완료. 프리팹 {prefabCount}개, 씬 {sceneCount}개 갱신.");
        }

        private static TMP_FontAsset CreateBakedFont(Font sourceFont, string characters)
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont,
                                                                    SamplingPointSize,
                                                                    AtlasPadding,
                                                                    GlyphRenderMode.SDFAA,
                                                                    AtlasSize,
                                                                    AtlasSize,
                                                                    AtlasPopulationMode.Dynamic,
                                                                    true);
            if (fontAsset == null)
            {
                throw new InvalidOperationException("나눔고딕 SDF 생성에 실패했습니다.");
            }

            fontAsset.name = "NanumGothic SDF";
            fontAsset.material.name = "NanumGothic SDF Material";
            fontAsset.isMultiAtlasTexturesEnabled = true;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            if (!fontAsset.TryAddCharacters(characters, out string missingCharacters, true) &&
                !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"[NanumGothicFontBaker] 원본 나눔고딕에 없는 문자는 제외합니다: {missingCharacters}");
            }

            return fontAsset;
        }

        private static void ReplaceFontAssetFile(TMP_FontAsset rebuiltFont)
        {
            AssetDatabase.DeleteAsset(TemporaryFontAssetPath);
            AssetDatabase.CreateAsset(rebuiltFont, TemporaryFontAssetPath);
            AddSubAssetIfNeeded(rebuiltFont.material, rebuiltFont);

            Texture2D[] atlasTextures = rebuiltFont.atlasTextures;
            for (int index = 0; index < atlasTextures.Length; index++)
            {
                Texture2D texture = atlasTextures[index];
                if (texture == null)
                {
                    continue;
                }

                texture.name = index == 0 ? "NanumGothic SDF Atlas" : $"NanumGothic SDF Atlas {index}";
                AddSubAssetIfNeeded(texture, rebuiltFont);
            }

            ConfigureFontAsset(rebuiltFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TemporaryFontAssetPath, ImportAssetOptions.ForceSynchronousImport);

            File.Copy(TemporaryFontAssetPath, FontAssetPath, true);
            AssetDatabase.DeleteAsset(TemporaryFontAssetPath);
            AssetDatabase.ImportAsset(FontAssetPath,
                                      ImportAssetOptions.ForceSynchronousImport |
                                      ImportAssetOptions.ForceUpdate);
        }

        private static void AddSubAssetIfNeeded(UnityEngine.Object subAsset, TMP_FontAsset owner)
        {
            if (subAsset == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(subAsset)))
            {
                return;
            }

            AssetDatabase.AddObjectToAsset(subAsset, owner);
        }

        private static void ConfigureFontAsset(TMP_FontAsset fontAsset)
        {
            fontAsset.name = "NanumGothic SDF";
            fontAsset.isMultiAtlasTexturesEnabled = true;
            fontAsset.fallbackFontAssetTable?.Clear();

            SerializedObject serializedFont = new SerializedObject(fontAsset);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            serializedFont.FindProperty("m_SourceFontFileGUID").stringValue = AssetDatabase.AssetPathToGUID(SourceFontPath);
            serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue = sourceFont;
            serializedFont.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Dynamic;

            SerializedProperty clearOnBuild = serializedFont.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild != null)
            {
                clearOnBuild.boolValue = false;
            }

            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fontAsset);
        }

        private static void ConfigureTmpSettings(TMP_FontAsset fontAsset)
        {
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null)
            {
                throw new InvalidOperationException("TMP Settings를 불러오지 못했습니다.");
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
            serializedSettings.FindProperty("m_defaultFontAssetPath").stringValue = string.Empty;

            SerializedProperty fallbackFonts = serializedSettings.FindProperty("m_fallbackFontAssets");
            fallbackFonts.ClearArray();

            SerializedProperty clearOnBuild = serializedSettings.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild != null)
            {
                clearOnBuild.boolValue = false;
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void ApplyToSettingsPresets(TMP_FontAsset fontAsset)
        {
            string[] presetGuids = AssetDatabase.FindAssets("t:SettingsLayerDesignPreset");
            foreach (string guid in presetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SettingsLayerDesignPreset preset = AssetDatabase.LoadAssetAtPath<SettingsLayerDesignPreset>(path);
                if (preset == null || preset.fontAsset == fontAsset)
                {
                    continue;
                }

                preset.fontAsset = fontAsset;
                EditorUtility.SetDirty(preset);
            }
        }

        private static int ApplyToPrefabs(TMP_FontAsset fontAsset)
        {
            int changedPrefabCount = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    if (!ApplyToTexts(root.GetComponentsInChildren<TMP_Text>(true), fontAsset))
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedPrefabCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return changedPrefabCount;
        }

        private static int ApplyToScenes(TMP_FontAsset fontAsset)
        {
            int changedSceneCount = 0;
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    TMP_Text[] texts = scene.GetRootGameObjects()
                                             .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                                             .ToArray();

                    if (!ApplyToTexts(texts, fontAsset))
                    {
                        continue;
                    }

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedSceneCount++;
                }
            }
            finally
            {
                if (originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            return changedSceneCount;
        }

        private static bool ApplyToTexts(IEnumerable<TMP_Text> texts, TMP_FontAsset fontAsset)
        {
            bool changed = false;

            foreach (TMP_Text text in texts)
            {
                if (text == null ||
                    (text.font == fontAsset && text.fontSharedMaterial == fontAsset.material))
                {
                    continue;
                }

                text.font = fontAsset;
                text.fontSharedMaterial = fontAsset.material;
                EditorUtility.SetDirty(text);
                changed = true;
            }

            return changed;
        }

        private static string CollectProjectCharacters()
        {
            HashSet<char> characters = new HashSet<char>();

            for (char character = ' '; character <= '~'; character++)
            {
                characters.Add(character);
            }

            AddCharacters(characters, DayResultLayerController.RequiredFontCharacters);
            AddCharacters(characters, "₩℃°×…·←↑→↓");

            foreach (string path in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories))
            {
                if (string.Equals(path, FontAssetPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(path, TemporaryFontAssetPath, StringComparison.OrdinalIgnoreCase) ||
                    !TextExtensions.Contains(Path.GetExtension(path)))
                {
                    continue;
                }

                if (Path.GetExtension(path).Equals(".asset", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileNameWithoutExtension(path).IndexOf("SDF", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                try
                {
                    foreach (char character in File.ReadAllText(path))
                    {
                        if (IsHangul(character))
                        {
                            characters.Add(character);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[NanumGothicFontBaker] 문자 수집을 건너뜁니다: {path} ({exception.Message})");
                }
            }

            return new string(characters.OrderBy(character => character).ToArray());
        }

        private static bool IsHangul(char character)
        {
            return character >= '\u1100' && character <= '\u11FF' ||
                   character >= '\u3130' && character <= '\u318F' ||
                   character >= '\uAC00' && character <= '\uD7A3';
        }

        private static void AddCharacters(ISet<char> destination, string source)
        {
            foreach (char character in source)
            {
                destination.Add(character);
            }
        }
    }
}
