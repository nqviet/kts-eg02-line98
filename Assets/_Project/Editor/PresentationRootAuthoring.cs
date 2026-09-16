using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Line98.Data;
using Line98.Presentation;

namespace Line98.Editor
{
    public static class PresentationRootAuthoring
    {
        public const string MotionProfilePath = "Assets/_Project/Content/Definitions/MotionProfile_Default.asset";
        public const string FeedbackProfilePath = "Assets/_Project/Content/Definitions/FeedbackProfile_Tiers.asset";
        public const string GameScenePath = "Assets/_Project/Content/Scenes/Game.unity";

        [MenuItem("Line98/Wire PresentationRoot Profiles")]
        public static void WirePresentationRootProfiles()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath);
            var root = Object.FindAnyObjectByType<PresentationRoot>();
            if (root == null)
            {
                Debug.LogError($"[PresentationRootAuthoring] Could not find PresentationRoot in {GameScenePath}");
                return;
            }

            var motionProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfilePath);
            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfilePath);

            if (motionProfile == null)
            {
                Debug.LogError($"[PresentationRootAuthoring] Missing MotionProfile at {MotionProfilePath}");
                return;
            }

            if (feedbackProfile == null)
            {
                Debug.LogError($"[PresentationRootAuthoring] Missing FeedbackProfile at {FeedbackProfilePath}");
                return;
            }

            var so = new SerializedObject(root);
            var propMotion = so.FindProperty("m_MotionProfile");
            var propFeedback = so.FindProperty("m_FeedbackProfile");

            if (propMotion != null)
            {
                propMotion.objectReferenceValue = motionProfile;
            }

            if (propFeedback != null)
            {
                propFeedback.objectReferenceValue = feedbackProfile;
            }

            so.ApplyModifiedProperties();

            var propVfx = so.FindProperty("m_VfxCatalog");
            var propAudio = so.FindProperty("m_AudioCatalog");
            var propMixer = so.FindProperty("m_MainMixer");

            if (propVfx?.objectReferenceValue == null)
            {
                Debug.LogWarning("[PresentationRootAuthoring] m_VfxCatalog is null on PresentationRoot.");
            }
            if (propAudio?.objectReferenceValue == null)
            {
                Debug.LogWarning("[PresentationRootAuthoring] m_AudioCatalog is null on PresentationRoot.");
            }
            if (propMixer?.objectReferenceValue == null)
            {
                Debug.LogWarning("[PresentationRootAuthoring] m_MainMixer is null on PresentationRoot.");
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PresentationRootAuthoring] Successfully wired profiles to PresentationRoot in Game.unity!");
        }
    }
}
