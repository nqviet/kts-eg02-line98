using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class ArchitectureTests
    {
        private string m_ProjectRoot;

        [SetUp]
        public void SetUp()
        {
            m_ProjectRoot = Application.dataPath;
        }

        [Test]
        public void CoreAssembly_HasNoEngineReferences()
        {
            string coreAsmdefPath = Path.Combine(m_ProjectRoot, "_Project/Core/Line98.Core.asmdef");
            Assert.IsTrue(File.Exists(coreAsmdefPath), $"Core asmdef must exist at {coreAsmdefPath}");

            string content = File.ReadAllText(coreAsmdefPath);
            StringAssert.Contains("\"noEngineReferences\": true", content);
        }

        [Test]
        public void GameplayAssembly_NeverReferencesPresentation()
        {
            string gameplayAsmdefPath = Path.Combine(m_ProjectRoot, "_Project/Gameplay/Line98.Gameplay.asmdef");
            Assert.IsTrue(File.Exists(gameplayAsmdefPath), $"Gameplay asmdef must exist at {gameplayAsmdefPath}");

            string content = File.ReadAllText(gameplayAsmdefPath);
            StringAssert.DoesNotContain("Line98.Presentation", content);
        }

        [Test]
        public void NoBannedTypeNames_ExistInProjectSource()
        {
            string[] bannedNames = { "CurrencyService", "EnergyService", "LevelGraph", "Wallet", "ProgressionService" };
            string projectDir = Path.Combine(m_ProjectRoot, "_Project");

            string[] csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);

            foreach (string file in csFiles)
            {
                // Skip this test file itself
                if (file.Contains("ArchitectureTests.cs")) continue;

                string text = File.ReadAllText(file);
                foreach (string banned in bannedNames)
                {
                    Assert.IsFalse(text.Contains($"class {banned}") || text.Contains($"interface {banned}"),
                        $"Banned type '{banned}' detected in {file} (GDD §34 guardrail)");
                }
            }
        }

        [Test]
        public void NoUnityRandom_InCoreOrGameplay()
        {
            string coreDir = Path.Combine(m_ProjectRoot, "_Project/Core");
            string gameplayDir = Path.Combine(m_ProjectRoot, "_Project/Gameplay");

            AssertNoUnityRandomInDir(coreDir);
            AssertNoUnityRandomInDir(gameplayDir);
        }

        private static void AssertNoUnityRandomInDir(string dir)
        {
            if (!Directory.Exists(dir)) return;

            string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                Assert.IsFalse(text.Contains("UnityEngine.Random") || (text.Contains("Random.") && !text.Contains("System.Random")),
                    $"UnityEngine.Random reference found in {file}. Use XorShift128 for determinism per Architecture §0 & §4.");
            }
        }
    }
}
