using SheepSheepBurger.Settings;
using UnityEditor;
using UnityEngine;

namespace SheepSheepBurger.EditorTools
{
    [CustomEditor(typeof(SettingsLayerDesignPreset))]
    public sealed class SettingsLayerDesignPresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty applyLayout = serializedObject.FindProperty("applyRectTransformLayout");
            DrawPropertiesExcluding(serializedObject, "m_Script", "layout");

            if (applyLayout != null && applyLayout.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("layout"), true);
            }
            else
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    "UI 위치와 크기는 프리팹/씬의 RectTransform으로 직접 수정합니다. " +
                    "숫자 프리셋으로 다시 제어하려면 'RectTransform 위치/크기 자동 적용'을 켜세요.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("열린 설정 레이어에 디자인 적용"))
            {
                ((SettingsLayerDesignPreset)target).ApplyToOpenLayers();
            }

            if (GUILayout.Button("설정 레이어 프리팹 다시 만들기"))
            {
                SettingsLayerBuilder.BuildSettingsLayerPrefab();
            }
        }
    }

    [CustomEditor(typeof(SettingsLayerDesignPresenter))]
    public sealed class SettingsLayerDesignPresenterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("디자인 프리셋 적용"))
            {
                ((SettingsLayerDesignPresenter)target).ApplyDesign();
            }
        }
    }

    [CustomEditor(typeof(SettingsLayerController))]
    public sealed class SettingsLayerControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            SettingsLayerController controller = (SettingsLayerController)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("열기"))
                {
                    controller.Open();
                }

                if (GUILayout.Button("닫기"))
                {
                    controller.Close();
                }

                if (GUILayout.Button("토글"))
                {
                    controller.Toggle();
                }
            }
        }
    }
}
