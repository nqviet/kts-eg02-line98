using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>
    /// Idempotent authoring pass for the UI file-layout migration.
    /// It is intentionally Editor-only so prefab and scene YAML is produced by Unity APIs.
    /// </summary>
    public static class UiLayoutMigration
    {
        private const string s_UiRootPrefabPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";
        private const string s_MainMenuPrefabPath = "Assets/_Project/Content/Prefabs/UI/Screens/MainMenu/Screen_MainMenu.prefab";
        private const string s_MainMenuScenePath = "Assets/_Project/Content/Scenes/MainMenu.unity";
        private const string s_GameScenePath = "Assets/_Project/Content/Scenes/Game.unity";

        [MenuItem("Line98/UI/Apply File Layout Migration")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EnsureUiFolders();
            ConfigureUiRootPrefab();
            CreateMainMenuScreenPrefab();
            ConfigureMainMenuScene();
            ConsolidateLegacyVfx();
            CleanupLegacyUiFolders();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiLayoutMigration] UI file-layout migration applied.");
        }

        private static void EnsureUiFolders()
        {
            EnsureFolder("Assets/_Project/Content/Prefabs/UI/Screens");
            EnsureFolder("Assets/_Project/Content/Prefabs/UI/Screens/MainMenu");
            EnsureFolder("Assets/_Project/Presentation/UI/Screens");
            EnsureFolder("Assets/_Project/Presentation/UI/Screens/MainMenu");
            EnsureFolder("Assets/Art/Sprites/UI/Icons");
            EnsureFolder("Assets/Art/Sprites/UI/Frames");
            EnsureFolder("Assets/Art/Sprites/UI/Overlays");
            EnsureFolder("Assets/Art/Sprites/UI/Badges");
        }

        private static void ConfigureUiRootPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(s_UiRootPrefabPath);
            try
            {
                UiShell shell = root.GetComponent<UiShell>();
                if (shell == null)
                {
                    shell = root.AddComponent<UiShell>();
                }

                Canvas staticCanvas = FindNamedComponent<Canvas>(root, "Canvas_StaticHUD");
                Canvas dynamicCanvas = FindNamedComponent<Canvas>(root, "Canvas_DynamicHUD");
                Canvas popupCanvas = FindNamedComponent<Canvas>(root, "Canvas_Popups");
                Image scrimImage = FindNamedComponent<Image>(root, "ModalScrim");
                CanvasGroup scrimGroup = scrimImage != null
                    ? scrimImage.GetComponent<CanvasGroup>()
                    : FindNamedComponent<CanvasGroup>(root, "ModalScrim");

                SetObjectReference(shell, "m_StaticCanvas", staticCanvas);
                SetObjectReference(shell, "m_DynamicCanvas", dynamicCanvas);
                SetObjectReference(shell, "m_PopupCanvas", popupCanvas);
                SetObjectReference(shell, "m_ScrimImage", scrimImage);
                SetObjectReference(shell, "m_ScrimGroup", scrimGroup);
                SetObjectReference(shell, "m_GameOverPopup", root.GetComponentInChildren<GameOverPopup>(true));
                SetObjectReference(shell, "m_ConfirmPopup", root.GetComponentInChildren<ConfirmPopup>(true));
                SetObjectReference(shell, "m_SettingsPopup", root.GetComponentInChildren<SettingsPopup>(true));
                SetObjectReference(shell, "m_StatisticsPopup", root.GetComponentInChildren<StatisticsPopup>(true));

                UiCanvasStack canvasStack = root.GetComponent<UiCanvasStack>() ?? root.AddComponent<UiCanvasStack>();
                canvasStack.Configure(staticCanvas, dynamicCanvas, popupCanvas, scrimImage, scrimGroup);
                SetObjectReference(shell, "m_CanvasStack", canvasStack);

                UiPopupRegistry popupRegistry = root.GetComponent<UiPopupRegistry>() ?? root.AddComponent<UiPopupRegistry>();
                UiScreenRegistry screenRegistry = root.GetComponent<UiScreenRegistry>() ?? root.AddComponent<UiScreenRegistry>();
                SetObjectReference(shell, "m_PopupRegistry", popupRegistry);
                SetObjectReference(shell, "m_ScreenRegistry", screenRegistry);

                UiHudScreen hudScreen = root.GetComponent<UiHudScreen>() ?? root.AddComponent<UiHudScreen>();
                SetObjectReference(hudScreen, "m_Presenter", root.GetComponent<HudPresenter>());

                UiSceneNavigator sceneNavigator = root.GetComponent<UiSceneNavigator>() ?? root.AddComponent<UiSceneNavigator>();
                EnsureGameMenuButton(root, staticCanvas);
                EditorUtility.SetDirty(sceneNavigator);
                EditorUtility.SetDirty(shell);
                PrefabUtility.SaveAsPrefabAsset(root, s_UiRootPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureGameMenuButton(GameObject root, Canvas parentCanvas)
        {
            if (parentCanvas == null || FindChildRecursive(root.transform, "Button_Menu") != null)
            {
                return;
            }

            GameObject buttonObject = CreateButton("Button_Menu", parentCanvas.transform, "MENU", UiNavigationButton.UiDestination.Menu);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -36f);
            rect.sizeDelta = new Vector2(170f, 72f);
        }

        private static void CreateMainMenuScreenPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(s_MainMenuPrefabPath) != null)
            {
                return;
            }

            GameObject screen = new GameObject("Screen_MainMenu", typeof(RectTransform), typeof(CanvasGroup), typeof(MainMenuScreen), typeof(UiSceneNavigator), typeof(SafeAreaFitter));
            RectTransform screenRect = screen.GetComponent<RectTransform>();
            Stretch(screenRect);
            SetSerializedInt(screen.GetComponent<SafeAreaFitter>(), "m_Mode", (int)SafeAreaFitter.FitMode.Overlay);
            SetObjectReference(screen.GetComponent<SafeAreaFitter>(), "m_OverlayRoot", screenRect);
            SetSerializedString(screen.GetComponent<MainMenuScreen>(), "m_ScreenId", "MainMenu");

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(screen.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            Stretch(backgroundRect);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.035f, 0.055f, 0.095f, 1f);
            backgroundImage.raycastTarget = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(screen.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 40f);
            contentRect.sizeDelta = new Vector2(720f, 0f);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            AddMenuText(content.transform, "LINE 98", 72f, new Color(0.87f, 0.94f, 1f, 1f), 108f);
            AddMenuText(content.transform, "COLOR LINES", 28f, new Color(0.48f, 0.68f, 0.86f, 1f), 54f);
            AddMenuText(content.transform, "A calm classic puzzle of paths and patterns", 24f, new Color(0.68f, 0.74f, 0.82f, 1f), 80f);

            GameObject play = CreateButton("Button_Play", content.transform, "PLAY", UiNavigationButton.UiDestination.Game);
            GameObject settings = CreateButton("Button_Settings", content.transform, "SETTINGS", UiNavigationButton.UiDestination.Settings);
            GameObject statistics = CreateButton("Button_Statistics", content.transform, "STATISTICS", UiNavigationButton.UiDestination.Statistics);
            SetPreferredHeight(play, 104f);
            SetPreferredHeight(settings, 104f);
            SetPreferredHeight(statistics, 104f);

            GameObject footer = AddMenuText(content.transform, "v0.1  •  Classic mode", 18f, new Color(0.44f, 0.52f, 0.62f, 1f), 40f);
            footer.GetComponent<LayoutElement>().ignoreLayout = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(screen, s_MainMenuPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Unable to create Screen_MainMenu prefab.");
            }

            UnityEngine.Object.DestroyImmediate(screen);
        }

        private static void ConfigureMainMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene(s_MainMenuScenePath, OpenSceneMode.Single);
            GameObject uiRoot = GameObject.Find("UI_Root");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("UI_Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UiShell));
            }

            Canvas canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            UiShell shell = uiRoot.GetComponent<UiShell>();
            UiCanvasStack stack = uiRoot.GetComponent<UiCanvasStack>() ?? uiRoot.AddComponent<UiCanvasStack>();
            UiPopupRegistry popupRegistry = uiRoot.GetComponent<UiPopupRegistry>() ?? uiRoot.AddComponent<UiPopupRegistry>();
            UiScreenRegistry screenRegistry = uiRoot.GetComponent<UiScreenRegistry>() ?? uiRoot.AddComponent<UiScreenRegistry>();
            SetObjectReference(shell, "m_DynamicCanvas", canvas);
            SetObjectReference(shell, "m_CanvasStack", stack);
            SetObjectReference(shell, "m_PopupRegistry", popupRegistry);
            SetObjectReference(shell, "m_ScreenRegistry", screenRegistry);
            stack.Configure(null, canvas, null, null, null);

            MainMenuScreen existingScreen = uiRoot.GetComponentInChildren<MainMenuScreen>(true);
            if (existingScreen == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_MainMenuPrefabPath);
                if (prefab != null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.transform.SetParent(uiRoot.transform, false);
                }
            }

            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventObject, scene);
        }

        private static void ConsolidateLegacyVfx()
        {
            const string legacyDirectory = "Assets/Art/Prefabs/VFX";
            if (!AssetDatabase.IsValidFolder(legacyDirectory))
            {
                return;
            }

            string[] legacyGuids = AssetDatabase.FindAssets("t:Prefab", new[] { legacyDirectory });
            string[] allGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(allGuids[i]);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith(legacyDirectory, StringComparison.Ordinal))
                {
                    continue;
                }

                string[] dependencies = AssetDatabase.GetDependencies(path, false);
                for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                {
                    referenced.Add(dependencies[dependencyIndex]);
                }
            }

            for (int i = 0; i < legacyGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(legacyGuids[i]);
                if (referenced.Contains(path))
                {
                    Debug.LogWarning($"[UiLayoutMigration] Keeping referenced legacy VFX prefab: {path}");
                    continue;
                }

                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[UiLayoutMigration] Removed unreferenced legacy VFX prefab: {path}");
            }

            if (AssetDatabase.FindAssets("t:Prefab", new[] { legacyDirectory }).Length == 0)
            {
                AssetDatabase.DeleteAsset(legacyDirectory);
            }
        }

        private static void CleanupLegacyUiFolders()
        {
            string[] legacyFolders =
            {
                "Assets/_Project/Content/Prefabs/UI/Components",
                "Assets/_Project/Content/Prefabs/UI/Hud"
            };

            for (int i = 0; i < legacyFolders.Length; i++)
            {
                string folder = legacyFolders[i];
                if (AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets("", new[] { folder }).Length == 0)
                {
                    AssetDatabase.DeleteAsset(folder);
                    Debug.Log($"[UiLayoutMigration] Removed empty legacy UI folder: {folder}");
                }
            }
        }

        private static GameObject CreateButton(string name, Transform parent, string label, UiNavigationButton.UiDestination destination)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UiActionButton), typeof(UiNavigationButton));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.10f, 0.17f, 0.27f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.10f, 0.17f, 0.27f, 1f);
            colors.highlightedColor = new Color(0.15f, 0.28f, 0.42f, 1f);
            colors.pressedColor = new Color(0.22f, 0.38f, 0.55f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            UiActionButton action = buttonObject.GetComponent<UiActionButton>();
            SetObjectReference(action, "m_Button", button);
            UiNavigationButton navigation = buttonObject.GetComponent<UiNavigationButton>();
            SetObjectReference(navigation, "m_ActionButton", action);
            SetSerializedInt(navigation, "m_Destination", (int)destination);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            Stretch(textRect);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28f;
            text.color = new Color(0.88f, 0.94f, 1f, 1f);
            text.raycastTarget = false;
            return buttonObject;
        }

        private static GameObject AddMenuText(Transform parent, string content, float fontSize, Color color, float height)
        {
            GameObject textObject = new GameObject(content.Replace(' ', '_'), typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            SetPreferredHeight(textObject, height);
            return textObject;
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            LayoutElement element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static T FindNamedComponent<T>(GameObject root, string objectName) where T : Component
        {
            Transform child = FindChildRecursive(root.transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildRecursive(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedInt(UnityEngine.Object target, string propertyName, int value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedString(UnityEngine.Object target, string propertyName, string value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
