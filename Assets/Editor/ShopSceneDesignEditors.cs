using SheepSheepBurger.Shop;
using UnityEditor;
using UnityEngine;

namespace SheepSheepBurger.EditorTools
{
    [CustomEditor(typeof(ShopSceneDesignPreset))]
    public sealed class ShopSceneDesignPresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty applyLayout = serializedObject.FindProperty("applyRectTransformLayout");
            DrawPropertiesExcluding(serializedObject, "m_Script", "layerOrder", "layout");

            if (applyLayout != null && applyLayout.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("layerOrder"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("layout"), true);
            }
            else
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    "UI 위치와 크기는 Scene/Hierarchy의 RectTransform으로 직접 수정합니다. " +
                    "숫자 프리셋으로 다시 제어하려면 'RectTransform 위치/크기 자동 적용'을 켜세요.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("열린 상점 씬에 디자인 적용"))
            {
                ((ShopSceneDesignPreset)target).ApplyToOpenScenes();
            }
        }
    }

    [CustomEditor(typeof(ShopSceneDesignController))]
    public sealed class ShopSceneDesignControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("디자인 프리셋 적용"))
            {
                ((ShopSceneDesignController)target).ApplyDesign();
            }
        }
    }

    [CustomEditor(typeof(ShopSlotDesignPresenter))]
    public sealed class ShopSlotDesignPresenterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("슬롯 디자인 적용"))
            {
                ((ShopSlotDesignPresenter)target).ApplyDesign();
            }
        }
    }
}
