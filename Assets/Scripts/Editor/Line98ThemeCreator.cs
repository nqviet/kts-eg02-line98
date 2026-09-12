#if UNITY_EDITOR
using System.IO;
using Line98.Design;
using UnityEditor;
using UnityEngine;

namespace Line98.Editor
{
    public static class Line98ThemeCreator
    {
        [MenuItem("Line98/Create Default Theme Asset")]
        public static void CreateDefaultThemeAsset()
        {
            const string folderPath = "Assets/Settings";
            const string assetPath = folderPath + "/DefaultLine98Theme.asset";

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            Line98ThemeData existingAsset = AssetDatabase.LoadAssetAtPath<Line98ThemeData>(assetPath);
            if (existingAsset == null)
            {
                Line98ThemeData newTheme = ScriptableObject.CreateInstance<Line98ThemeData>();
                newTheme.name = "DefaultLine98Theme";
                AssetDatabase.CreateAsset(newTheme, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Created Line98ThemeData asset at {assetPath}");
            }
            else
            {
                Debug.Log($"Line98ThemeData already exists at {assetPath}");
            }

            // Assign to Line98Game in the current scene if present
            Line98Game game = Object.FindFirstObjectByType<Line98Game>();
            if (game != null)
            {
                SerializedObject serializedGame = new SerializedObject(game);
                SerializedProperty themeProp = serializedGame.FindProperty("m_Theme");
                if (themeProp != null)
                {
                    themeProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Line98ThemeData>(assetPath);
                    serializedGame.ApplyModifiedProperties();
                    EditorUtility.SetDirty(game);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
                    Debug.Log("Assigned DefaultLine98Theme to Line98Game in scene.");
                }
            }
        }
    }
}
#endif
