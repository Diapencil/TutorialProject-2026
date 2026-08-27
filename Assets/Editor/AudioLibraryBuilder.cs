using System.IO;
using SheepSheepBurger.Audio;
using UnityEditor;
using UnityEngine;

namespace SheepSheepBurger.EditorTools
{
    public static class AudioLibraryBuilder
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string AudioResourcesFolder = ResourcesFolder + "/Audio";
        private const string AudioLibraryPath = AudioResourcesFolder + "/AudioLibrary.asset";

        [MenuItem("SheepSheep/Audio/Create Audio Library")]
        public static void CreateAudioLibrary()
        {
            EnsureFolder(ResourcesFolder);
            EnsureFolder(AudioResourcesFolder);

            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath);

            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, AudioLibraryPath);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(library);
            Selection.activeObject = library;
            Debug.Log($"[AudioLibraryBuilder] Audio library ready: {AudioLibraryPath}");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
