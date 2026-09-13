using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>Applies the mockup art direction to the existing scene without rebuilding its UI.</summary>
    public static class MockupStylePass
    {
        private const string Folder = "Assets/_Project/Content/Materials/Mockup";
        private static readonly Color s_Ink = new Color(.13f,.20f,.36f,1f);

        [MenuItem("Line98/UI/Apply Mockup Style")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Apply styling outside Play mode.");
            var root = GameObject.Find("UI_Root");
            var presentation = Object.FindAnyObjectByType<PresentationRoot>();
            if (root == null || presentation == null) throw new System.InvalidOperationException("Open the authored Game scene first.");
            System.IO.Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Undo.RegisterFullObjectHierarchyUndo(root, "Match mockup styling");
            var card = Panel("Card",318,176,34,3,new Color(.81f,.91f,1f,.50f),new Color(1,1,1,.96f));
            var button = Panel("Action",296,162,34,3,new Color(.86f,.91f,1f,.72f),new Color(1,1,1,.96f));
            var square = Panel("Square",108,108,28,2.5f,new Color(.85f,.92f,1f,.72f),Color.white);
            var tray = Panel("Tray",86,86,15,0,new Color(.61f,.70f,.80f,.32f),Color.clear,.10f);
            var hint = Panel("Hint",200,200,46,4,new Color(.63f,.80f,1f,.8f),Color.white);
            foreach (var img in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (img.name.StartsWith("Card_")) Style(img,card);
                if (img.name == "BtnSettings" || img.name == "BtnStats") Style(img,square);
                if (img.name == "BtnUndo" || img.name == "BtnNewGame")
                {
                    Style(img,button);
                    img.rectTransform.sizeDelta = new Vector2(296,162);
                    var control = img.GetComponent<UnityEngine.UI.Button>();
                    var buttonColors = control.colors;
                    buttonColors.disabledColor = new Color(.86f,.90f,.96f,.85f);
                    control.colors = buttonColors;
                }
                if (img.name == "Tray_Next") img.enabled = false;
                if (img.name == "BtnHint")
                {
                    Style(img,hint);
                    var glow = Child(img.transform,"GlowRim").gameObject;
                    var glowImage = glow.GetComponent<UnityEngine.UI.Image>() ?? glow.AddComponent<UnityEngine.UI.Image>();
                    Style(glowImage,Panel("HintGlow",216,216,54,6,new Color(.3f,.8f,1f,.12f),new Color(.22f,.9f,1f,.75f)));
                    glowImage.rectTransform.sizeDelta = new Vector2(216,216);
                    glowImage.raycastTarget = false;
                    glow.transform.SetAsFirstSibling();
                    Icon(img.transform,"DirectionIcon",HudIconGraphic.IconKind.Directions,124,124,new Color(0,.32f,1,1));
                }
            }
            var staticRoot = root.transform.Find("Canvas_StaticHUD");
            foreach (var panel in staticRoot.GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                if (panel.name.StartsWith("Card_") || panel.name == "BtnSettings" || panel.name == "BtnStats"
                    || panel.name == "BtnUndo" || panel.name == "BtnNewGame") AddShadow(panel);
            }
            var brand = staticRoot.Find("BrandBar");
            var logo = brand.Find("LogoText");
            logo.GetComponent<UnityEngine.UI.Image>().enabled = false;
            var title = Text(logo,"Title","<color=#001040>Line</color> <color=#0056FF>98</color>",116,s_Ink);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = -5f;
            title.rectTransform.sizeDelta = new Vector2(440,128);
            title.rectTransform.anchoredPosition = new Vector2(27,16);
            var subtitle = Text(logo,"Subtitle","Color Lines",44,new Color(.28f,.40f,.61f));
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.rectTransform.sizeDelta = new Vector2(400,56);
            subtitle.rectTransform.anchoredPosition = new Vector2(0,-58);
            brand.Find("BallQuad").GetComponent<RectTransform>().sizeDelta = new Vector2(132,132);
            ReplaceIcon(brand.Find("BtnSettings"),HudIconGraphic.IconKind.Settings,62,62);
            ReplaceIcon(brand.Find("BtnStats"),HudIconGraphic.IconKind.Statistics,60,60);
            var actions = staticRoot.Find("ActionBar");
            actions.Find("BtnHint").GetComponent<RectTransform>().anchoredPosition = new Vector2(0,-10);
            ReplaceIcon(actions.Find("BtnUndo"),HudIconGraphic.IconKind.Undo,76,70);
            ReplaceIcon(actions.Find("BtnNewGame"),HudIconGraphic.IconKind.Restart,76,70);
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.enableAutoSizing = false;
                if (tmp.name == "Label")
                {
                    tmp.characterSpacing = 0;
                    tmp.fontSize = tmp.transform.parent.name.StartsWith("Card_") ? 28 : 29;
                    tmp.color = tmp.transform.parent.name.StartsWith("Card_") ? new Color(.28f,.39f,.57f) : s_Ink;
                    tmp.rectTransform.sizeDelta = new Vector2(280,38);
                }
            }
            foreach (var slot in root.transform.Find("Canvas_DynamicHUD/PreviewQueue").GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                if (!slot.name.StartsWith("Slot_")) continue;
                var well = Child(slot.transform.parent,"Recess_"+slot.name);
                var img = well.GetComponent<UnityEngine.UI.Image>() ?? well.gameObject.AddComponent<UnityEngine.UI.Image>();
                Style(img,tray);
                img.raycastTarget = false;
                well.sizeDelta = new Vector2(86,86);
                // Wells are siblings behind all of the preview balls, so they never cover a face.
                well.anchoredPosition = slot.rectTransform.anchoredPosition;
                well.SetAsFirstSibling();
            }
            var backdrop = GameObject.Find("Background_Backdrop");
            if (backdrop == null) throw new System.InvalidOperationException("Missing authored background.");
            var bgMesh = Quad("Backdrop",1,1,false,0);
            backdrop.GetComponent<MeshFilter>().sharedMesh = bgMesh;
            string texturePath = "Assets/Art/Textures/T_Background_MockupLake.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var bgMat = Material("Backdrop",Shader.Find("Universal Render Pipeline/Unlit"));
            bgMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            bgMat.SetColor("_BaseColor",Color.white);
            bgMat.SetFloat("_Cull",0);
            EditorUtility.SetDirty(bgMat);
            backdrop.GetComponent<Renderer>().sharedMaterial = bgMat;
            Set(presentation,"m_Backdrop",backdrop.GetComponent<Renderer>());
            var cellMat = Panel("BoardCell",100,100,15,0,new Color(.70f,.78f,.87f,.94f),Color.clear,.13f);
            var frameMat = Panel("BoardFrame",940,940,34,4,new Color(.87f,.93f,.98f,.84f),new Color(1,1,1,.98f));
            frameMat.renderQueue = 2990;
            cellMat.renderQueue = 3000;
            var serializedPresentation = new SerializedObject(presentation);
            var theme = serializedPresentation.FindProperty("m_BoardTheme").objectReferenceValue;
            var profile = serializedPresentation.FindProperty("m_CameraProfile").objectReferenceValue as Line98.Data.CameraProfileSO;
            float rowScale = 1f / Mathf.Sin((profile != null ? profile.PitchAngle : 58f) * Mathf.Deg2Rad);
            if (theme == null) throw new System.InvalidOperationException("Missing board theme.");
            var serializedTheme = new SerializedObject(theme);
            serializedTheme.FindProperty("m_RowPitchScale").floatValue = rowScale;
            serializedTheme.ApplyModifiedProperties();
            Set(presentation,"m_CellMesh",Quad("Cell",.97f,.97f*rowScale,true,.015f));
            Set(presentation,"m_FrameMesh",Quad("Frame",9.4f,9.4f*rowScale,true,-.045f));
            Set(presentation,"m_CellMaterial",cellMat);
            Set(presentation,"m_FrameMaterial",frameMat);
            // Keep the floor-pivot animation contract while centering the raised gem in
            // its projected cell. The imported source stays untouched.
            var originalGem = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/SM_Ball_Gem.obj").OfType<Mesh>().First();
            string gemPath = Folder+"/Gem.asset";
            var gem = AssetDatabase.LoadAssetAtPath<Mesh>(gemPath);
            if (gem == null) { gem = Object.Instantiate(originalGem); AssetDatabase.CreateAsset(gem,gemPath); }
            else EditorUtility.CopySerialized(originalGem,gem);
            float restOffset = .30f / Mathf.Tan((profile != null ? profile.PitchAngle : 58f)*Mathf.Deg2Rad);
            var vertices = gem.vertices;
            for(int i=0;i<vertices.Length;i++) vertices[i].z -= restOffset;
            gem.vertices = vertices; gem.name = "Gem_Centered"; gem.RecalculateBounds();
            EditorUtility.SetDirty(gem);
            Set(presentation,"m_BallMesh",gem);
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline,"Antialias polished board edges");
                pipeline.msaaSampleCount = 4;
                EditorUtility.SetDirty(pipeline);
            }
            string[] colors = { "EF1224", "FC691C", "FFCE08", "00AD43", "10C5EE", "B512ED", "0052FF" };
            var ballArray = new SerializedObject(presentation);
            var ballMaterials = ballArray.FindProperty("m_BallMaterials");
            for (int i=0;i<ballMaterials.arraySize && i<colors.Length;i++)
            {
                ColorUtility.TryParseHtmlString("#"+colors[i],out Color color);
                var ball = Material("Ball_"+i,Shader.Find("Line98/PolishedBall"));
                ball.SetColor("_BaseColor",color);
                EditorUtility.SetDirty(ball);
                ballMaterials.GetArrayElementAtIndex(i).objectReferenceValue = ball;
            }
            ballArray.ApplyModifiedProperties();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                Undo.RecordObject(light,"Soft studio lighting");
                light.color = Color.white;
                light.intensity = 1.3f;
                light.transform.rotation = Quaternion.Euler(48,-35,0);
            }
            root.GetComponent<SafeAreaFitter>().RefreshSafeArea(true);
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.MarkSceneDirty(presentation.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(presentation.gameObject.scene);
        }

        private static void Set(Object target,string field,Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        private static Material Material(string name,Shader shader)
        {
            string path = Folder+"/"+name+".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat,path); }
            mat.shader = shader;
            return mat;
        }
        private static Material Panel(string name,float w,float h,float radius,float border,Color fill,Color rim,float inset=0)
        {
            var mat = Material(name,Shader.Find("Line98/FrostedPanel"));
            mat.SetVector("_Size",new Vector4(w,h,0,0)); mat.SetFloat("_Radius",radius);
            mat.SetFloat("_Border",border); mat.SetColor("_Fill",fill); mat.SetColor("_Rim",rim); mat.SetFloat("_Inset",inset);
            EditorUtility.SetDirty(mat);
            return mat;
        }
        private static void Style(UnityEngine.UI.Image image,Material material)
        {
            image.sprite = null; image.type = UnityEngine.UI.Image.Type.Simple;
            image.material = material; image.color = Color.white;
        }
        private static void AddShadow(UnityEngine.UI.Image panel)
        {
            var source = panel.rectTransform;
            var rect = Child(source.parent,"Shadow_"+source.name);
            rect.anchorMin=source.anchorMin; rect.anchorMax=source.anchorMax; rect.pivot=source.pivot;
            rect.sizeDelta=source.sizeDelta+new Vector2(32,32);
            rect.anchoredPosition=source.anchoredPosition+new Vector2(0,-12);
            rect.SetSiblingIndex(source.GetSiblingIndex());
            var image=rect.GetComponent<UnityEngine.UI.Image>() ?? rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var material=Panel("Shadow_"+source.name,rect.sizeDelta.x,rect.sizeDelta.y,34,0,new Color(.12f,.23f,.43f,.15f),Color.clear);
            material.SetFloat("_Padding",16); material.SetFloat("_Feather",16);
            Style(image,material); image.raycastTarget=false;
        }
        private static RectTransform Child(Transform parent,string name)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null) { child = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); child.SetParent(parent,false); }
            return child;
        }
        private static TextMeshProUGUI Text(Transform parent,string name,string value,float size,Color color)
        {
            var rect = Child(parent,name);
            var text = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            text.text = value; text.fontSize = size; text.color = color; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }
        private static void ReplaceIcon(Transform button,HudIconGraphic.IconKind kind,float w,float h)
        {
            button.Find("Icon").GetComponent<UnityEngine.UI.Image>().enabled = false;
            var icon = Icon(button,"VectorIcon",kind,w,h,s_Ink);
            icon.rectTransform.anchoredPosition = button.name == "BtnUndo" || button.name == "BtnNewGame" ? new Vector2(0,16) : Vector2.zero;
        }
        private static HudIconGraphic Icon(Transform parent,string name,HudIconGraphic.IconKind kind,float w,float h,Color color)
        {
            var rect = Child(parent,name);
            if (rect.GetComponent<CanvasRenderer>() == null) rect.gameObject.AddComponent<CanvasRenderer>();
            var icon = rect.GetComponent<HudIconGraphic>() ?? rect.gameObject.AddComponent<HudIconGraphic>();
            icon.Kind = kind; icon.color = color; icon.raycastTarget = false; rect.sizeDelta = new Vector2(w,h);
            return icon;
        }
        private static Mesh Quad(string name,float w,float h,bool floor,float y)
        {
            string path=Folder+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=new Mesh(); AssetDatabase.CreateAsset(mesh,path); }
            mesh.Clear(); mesh.name=name;
            mesh.vertices=floor ? new[] {new Vector3(-w/2,y,-h/2),new Vector3(w/2,y,-h/2),new Vector3(w/2,y,h/2),new Vector3(-w/2,y,h/2)}
                : new[] {new Vector3(-w/2,-h/2,0),new Vector3(w/2,-h/2,0),new Vector3(w/2,h/2,0),new Vector3(-w/2,h/2,0)};
            mesh.uv=new[] {Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            mesh.colors=new[] {Color.white,Color.white,Color.white,Color.white};
            mesh.triangles=new[] {0,2,1,0,3,2}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh); return mesh;
        }
    }
}
