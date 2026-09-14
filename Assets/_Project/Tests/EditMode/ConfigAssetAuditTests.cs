using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.App;
using Line98.Core;
using Line98.Data;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class ConfigAssetAuditTests
    {
        private const string s_ScoreTablePath = "Assets/_Project/Content/Definitions/ScoreTable_Default.asset";
        private const string s_SpawnPolicyPath = "Assets/_Project/Content/Definitions/SpawnColorPolicy_Default.asset";
        private const string s_GameConfigPath = "Assets/_Project/Content/Definitions/GameConfig_Default.asset";

        [Test]
        public void ScoreTableAsset_Exists_IsNonScriptless_AndMatchesGddSpec()
        {
            var scoreTable = AssetDatabase.LoadAssetAtPath<ScoreTableSO>(s_ScoreTablePath);
            Assert.IsNotNull(scoreTable, $"ScoreTableSO asset must exist at {s_ScoreTablePath}");

            var so = new SerializedObject(scoreTable);
            var scriptProp = so.FindProperty("m_Script");
            Assert.IsNotNull(scriptProp.objectReferenceValue, "ScoreTableSO asset must not be scriptless (m_Script != null)");

            Assert.AreEqual(100, scoreTable.Score5, "Score for line length 5 must be 100 per GDD §7");
            Assert.AreEqual(180, scoreTable.Score6, "Score for line length 6 must be 180 per GDD §7");
            Assert.AreEqual(300, scoreTable.Score7, "Score for line length 7 must be 300 per GDD §7");
            Assert.AreEqual(500, scoreTable.Score8, "Score for line length 8 must be 500 per GDD §7");
            Assert.AreEqual(800, scoreTable.Score9, "Score for line length 9 must be 800 per GDD §7");
            Assert.AreEqual(0.25f, scoreTable.ComboStep, 0.001f, "Combo step must be 0.25 per GDD §7 / Gap #4");
            Assert.AreEqual(2.0f, scoreTable.MaxComboMultiplier, 0.001f, "Max combo multiplier must be 2.0 per GDD §7 / Gap #4");

            ScoreRules rules = scoreTable.ToRules();
            Assert.AreEqual(100, rules.GetBaseScoreForLength(5));
            Assert.AreEqual(180, rules.GetBaseScoreForLength(6));
            Assert.AreEqual(300, rules.GetBaseScoreForLength(7));
            Assert.AreEqual(500, rules.GetBaseScoreForLength(8));
            Assert.AreEqual(800, rules.GetBaseScoreForLength(9));
        }

        [Test]
        public void SpawnColorPolicyAsset_Exists_IsNonScriptless_AndMatchesGddSpec()
        {
            var spawnPolicy = AssetDatabase.LoadAssetAtPath<SpawnColorPolicySO>(s_SpawnPolicyPath);
            Assert.IsNotNull(spawnPolicy, $"SpawnColorPolicySO asset must exist at {s_SpawnPolicyPath}");

            var so = new SerializedObject(spawnPolicy);
            var scriptProp = so.FindProperty("m_Script");
            Assert.IsNotNull(scriptProp.objectReferenceValue, "SpawnColorPolicySO asset must not be scriptless (m_Script != null)");

            Assert.AreEqual(5, spawnPolicy.InitialActiveColors, "Initial active colors must be 5 per Gap #3");
            Assert.AreEqual(10, spawnPolicy.LinesForSixColors, "Lines for 6th color must be 10 per Gap #3");
            Assert.AreEqual(25, spawnPolicy.LinesForSevenColors, "Lines for 7th color must be 25 per Gap #3");
            Assert.AreEqual(3, spawnPolicy.SpawnCount, "Spawn count must be 3 per GDD §2");

            SpawnRules rules = spawnPolicy.ToRules();
            Assert.AreEqual(5, rules.GetActiveColorCount(0));
            Assert.AreEqual(6, rules.GetActiveColorCount(10));
            Assert.AreEqual(7, rules.GetActiveColorCount(25));
        }

        [Test]
        public void GameConfigAsset_Exists_IsNonScriptless_AndMatchesGddSpec()
        {
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfigSO>(s_GameConfigPath);
            Assert.IsNotNull(gameConfig, $"GameConfigSO asset must exist at {s_GameConfigPath}");

            var so = new SerializedObject(gameConfig);
            var scriptProp = so.FindProperty("m_Script");
            Assert.IsNotNull(scriptProp.objectReferenceValue, "GameConfigSO asset must not be scriptless (m_Script != null)");

            Assert.AreEqual(9, gameConfig.BoardSize, "Board size must be 9 per GDD §2");
            Assert.AreEqual(3, gameConfig.SpawnCount, "Spawn count must be 3 per GDD §2");
            Assert.AreEqual(3, gameConfig.PreviewCount, "Preview count must be 3 per GDD §2");
            Assert.AreEqual(3, gameConfig.FreeUndoCount, "Free undo count must be 3 per GDD §12");
            Assert.AreEqual(3, gameConfig.MaxRewardedUndos, "Max rewarded undos must be 3 per GDD §12 / Gap #6");
        }

        [Test]
        public void ConfigService_SingleAccessPoint_ServesRulesAccurately()
        {
            var scoreTable = AssetDatabase.LoadAssetAtPath<ScoreTableSO>(s_ScoreTablePath);
            var spawnPolicy = AssetDatabase.LoadAssetAtPath<SpawnColorPolicySO>(s_SpawnPolicyPath);
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfigSO>(s_GameConfigPath);

            var configService = new ConfigService(scoreTable, spawnPolicy, gameConfig);

            ScoreRules scoreRules = configService.GetScoreRules();
            Assert.AreEqual(100, scoreRules.Score5);
            Assert.AreEqual(800, scoreRules.Score9);

            SpawnRules spawnRules = configService.GetSpawnRules();
            Assert.AreEqual(5, spawnRules.InitialActiveColors);
            Assert.AreEqual(3, spawnRules.SpawnCount);
        }
    }
}
