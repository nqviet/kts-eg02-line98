using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Line98.Data;

namespace Line98.Editor
{
    public static class VfxPrefabAuthoring
    {
        private const string PrefabDir = "Assets/_Project/Content/Prefabs/Vfx";
        private const string CatalogPath = "Assets/_Project/Content/Definitions/VfxCatalog_Default.asset";
        private const string GlowMatPath = "Assets/Art/Materials/M_Ball_GlowShell.mat";

        [MenuItem("Line98/Build VFX Prefabs and Catalog")]
        public static void BuildAll()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
                AssetDatabase.Refresh();
            }

            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);
            if (glowMat == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                glowMat = new Material(shader);
                AssetDatabase.CreateAsset(glowMat, "Assets/_Project/Content/Shaders/M_VfxDefault.mat");
            }

            // 1. VFX_Ball_Select_Pulse
            BuildBallSelectPulse(glowMat);

            // 2. VFX_Ball_Trail
            BuildBallTrail(glowMat);

            // 3. VFX_Placement_Settle
            BuildPlacementSettle(glowMat);

            // 4. VFX_Invalid_Shake
            BuildInvalidShake();

            // 5. VFX_Clear_Tier1_5Balls
            BuildClearTier(1, 0.6f, 30, 0.5f, 2.0f, 0.3f, glowMat);

            // 6. VFX_Clear_Tier2_6_7Balls
            BuildClearTier(2, 0.8f, 50, 0.7f, 2.5f, 0.35f, glowMat);

            // 7. VFX_Clear_Tier3_8Balls
            BuildClearTier(3, 1.1f, 80, 0.9f, 3.0f, 0.4f, glowMat);

            // 8. VFX_Clear_Tier4_Perfect
            BuildClearTier4(glowMat);

            // 9. VFX_Score_Popup
            BuildScorePopup();

            // 10. VFX_Game_Over_Frost
            BuildGameOverFrost();

            // 11. VFX_Clear_Combo
            BuildCombo(glowMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Populate Catalog
            PopulateCatalog();

            Debug.Log("[VfxPrefabAuthoring] Successfully authored 11 VFX prefabs and populated VfxCatalog_Default!");
        }

        private static void BuildBallSelectPulse(Material mat)
        {
            var go = new GameObject("VFX_Ball_Select_Pulse");
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.loop = true;
            main.duration = 1.0f;
            main.startLifetime = 1.0f;
            main.startSpeed = 0f;
            main.startSize = 0.85f;
            main.startColor = new Color(0f, 0.9f, 1f, 0.85f);
            main.maxParticles = 1;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 1f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            string path = $"{PrefabDir}/VFX_Ball_Select_Pulse.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildBallTrail(Material mat)
        {
            var go = new GameObject("VFX_Ball_Trail");
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.22f;
            tr.startWidth = 0.34f;
            tr.endWidth = 0.02f;
            tr.minVertexDistance = 0.05f;
            tr.sharedMaterial = mat;
            tr.autodestruct = false;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;

            string path = $"{PrefabDir}/VFX_Ball_Trail.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildPlacementSettle(Material mat)
        {
            var go = new GameObject("VFX_Placement_Settle");
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = 0.25f;
            main.startSpeed = 0.6f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 1f, 1f, 0.8f);
            main.maxParticles = 12;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.25f;

            string path = $"{PrefabDir}/VFX_Placement_Settle.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildInvalidShake()
        {
            var go = new GameObject("VFX_Invalid_Shake");
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.duration = 0.18f;
            main.startLifetime = 0.18f;
            main.maxParticles = 0;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            string path = $"{PrefabDir}/VFX_Invalid_Shake.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildClearTier(int tier, float duration, short burstCount, float lifetime, float speed, float radius, Material mat)
        {
            string name = tier switch
            {
                1 => "VFX_Clear_Tier1_5Balls",
                2 => "VFX_Clear_Tier2_6_7Balls",
                3 => "VFX_Clear_Tier3_8Balls",
                _ => $"VFX_Clear_Tier{tier}"
            };

            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.loop = false;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            main.maxParticles = burstCount;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            string path = $"{PrefabDir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildClearTier4(Material mat)
        {
            var go = new GameObject("VFX_Clear_Tier4_Perfect");
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.loop = false;
            main.duration = 1.8f;
            main.startLifetime = 1.5f;
            main.startSpeed = 3.5f;
            main.startSize = 0.35f;
            main.startColor = new Color(1f, 0.84f, 0f, 1f); // Gold confetti
            main.maxParticles = 120;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var bannerAnchor = new GameObject("BannerAnchor");
            bannerAnchor.transform.SetParent(go.transform, false);
            bannerAnchor.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            var bannerTextGo = new GameObject("BannerText");
            bannerTextGo.transform.SetParent(bannerAnchor.transform, false);
            var bannerTmp = bannerTextGo.AddComponent<TextMeshPro>();
            bannerTmp.text = "PERFECT LINE";
            bannerTmp.fontSize = 5.0f;
            bannerTmp.alignment = TextAlignmentOptions.Center;
            bannerTmp.color = new Color(1f, 0.84f, 0f, 1f);
            var bannerRect = bannerTextGo.GetComponent<RectTransform>();
            if (bannerRect != null)
            {
                bannerRect.sizeDelta = new Vector2(8f, 2f);
            }

            string path = $"{PrefabDir}/VFX_Clear_Tier4_Perfect.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildCombo(Material mat)
        {
            var go = new GameObject("VFX_Clear_Combo");
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.9f;
            main.startLifetime = 0.8f;
            main.startSpeed = 2.8f;
            main.startSize = 0.45f;
            main.startColor = new Color(1f, 0.6f, 0.1f, 1f); // Orange-gold combo burst
            main.maxParticles = 60;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            string path = $"{PrefabDir}/VFX_Clear_Combo.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildScorePopup()
        {
            var go = new GameObject("VFX_Score_Popup");
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "+100";
            tmp.fontSize = 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.84f, 0f, 1f);

            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(4f, 2f);
            }

            string path = $"{PrefabDir}/VFX_Score_Popup.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void BuildGameOverFrost()
        {
            var go = new GameObject("VFX_Game_Over_Frost");
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            // Assign primitive quad mesh
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempQuad);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/UI/M_Ui_Card.mat");
            if (mat == null)
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);
            }
            mr.sharedMaterial = mat;
            go.transform.localScale = new Vector3(12f, 12f, 1f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            string path = $"{PrefabDir}/VFX_Game_Over_Frost.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        public static void PopulateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VfxCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = new[]
            {
                new VfxEntry("BallSelect", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Ball_Select_Pulse.prefab"), poolSize: 2, lifetime: 1.0f),
                new VfxEntry("BallTrail", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Ball_Trail.prefab"), poolSize: 2, lifetime: 0.22f),
                new VfxEntry("PlacementSettle", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Placement_Settle.prefab"), poolSize: 4, lifetime: 0.3f),
                new VfxEntry("InvalidShake", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Invalid_Shake.prefab"), poolSize: 2, lifetime: 0.18f),
                new VfxEntry("ClearTier1", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Clear_Tier1_5Balls.prefab"), poolSize: 4, lifetime: 0.6f),
                new VfxEntry("ClearTier2", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Clear_Tier2_6_7Balls.prefab"), poolSize: 4, lifetime: 0.8f),
                new VfxEntry("ClearTier3", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Clear_Tier3_8Balls.prefab"), poolSize: 4, lifetime: 1.1f),
                new VfxEntry("ClearTier4", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Clear_Tier4_Perfect.prefab"), poolSize: 4, lifetime: 1.8f),
                new VfxEntry("ComboBurst", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Clear_Combo.prefab"), poolSize: 4, lifetime: 0.9f),
                new VfxEntry("ScorePopup", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Score_Popup.prefab"), poolSize: 8, lifetime: 0.75f),
                new VfxEntry("GameOverFrost", AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/VFX_Game_Over_Frost.prefab"), poolSize: 2, lifetime: 1.2f)
            };

            catalog.SetEntries(entries, prewarmPoolCapacity: 4);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Line98/Wire PresentationRoot VfxCatalog")]
        public static void WirePresentationRootInGameScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Content/Scenes/Game.unity");
            var root = Object.FindAnyObjectByType<Line98.Presentation.PresentationRoot>();
            if (root != null)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
                var so = new SerializedObject(root);
                var prop = so.FindProperty("m_VfxCatalog");
                if (prop != null)
                {
                    prop.objectReferenceValue = catalog;
                    so.ApplyModifiedProperties();
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    Debug.Log("[VfxPrefabAuthoring] Successfully wired m_VfxCatalog to PresentationRoot in Game.unity!");
                }
            }
        }
    }
}
