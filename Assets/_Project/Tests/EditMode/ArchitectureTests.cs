using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Line98.Core;
using Line98.Gameplay;
using Line98.Services;

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
        public void AsmdefEdges_StrictlyMatchArchitectureSpecification()
        {
            AssertAsmdefReferences("Line98.Core", new string[0]);
            AssertAsmdefReferences("Line98.Data", new[] { "Line98.Core" });
            AssertAsmdefReferences("Line98.Gameplay", new[] { "Line98.Core", "Line98.Data" });
            AssertAsmdefReferences("Line98.Services", new[] { "Line98.Core", "Line98.Data" });
            AssertAsmdefReferences("Line98.Presentation", new[] { "Line98.Core", "Line98.Data", "Line98.Gameplay", "Unity.TextMeshPro", "Unity.InputSystem", "UnityEngine.UI" });
            AssertAsmdefReferences("Line98.App", new[] { "Line98.Core", "Line98.Data", "Line98.Gameplay", "Line98.Presentation", "Line98.Services" });
        }

        private void AssertAsmdefReferences(string assemblyName, string[] expectedRefs)
        {
            string[] files = Directory.GetFiles(Path.Combine(m_ProjectRoot, "_Project"), $"{assemblyName}.asmdef", SearchOption.AllDirectories);
            Assert.AreEqual(1, files.Length, $"Assembly {assemblyName}.asmdef not found exactly once");

            string json = File.ReadAllText(files[0]);
            HashSet<string> expected = new HashSet<string>(expectedRefs);

            int refIdx = json.IndexOf("\"references\":", StringComparison.Ordinal);
            Assert.IsTrue(refIdx >= 0, $"references not found in {files[0]}");
            int startBracket = json.IndexOf('[', refIdx);
            int endBracket = json.IndexOf(']', startBracket);
            string refsContent = json.Substring(startBracket + 1, endBracket - startBracket - 1);

            string[] tokens = refsContent.Split(new[] { ',', '\r', '\n', '\t', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> actual = new HashSet<string>(tokens);

            CollectionAssert.AreEquivalent(expected, actual, $"References mismatch in {assemblyName}.asmdef");
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
        public void NoUnityRandom_InCoreGameplayOrPresentation()
        {
            AssertNoUnityRandomInDir(Path.Combine(m_ProjectRoot, "_Project/Core"));
            AssertNoUnityRandomInDir(Path.Combine(m_ProjectRoot, "_Project/Gameplay"));
            AssertNoUnityRandomInDir(Path.Combine(m_ProjectRoot, "_Project/Presentation"));
        }

        [Test]
        public void AnimationGuardrails_NoAnimationReferencesInCoreGameplayOrServices()
        {
            string[] dirs =
            {
                Path.Combine(m_ProjectRoot, "_Project/Core"),
                Path.Combine(m_ProjectRoot, "_Project/Gameplay"),
                Path.Combine(m_ProjectRoot, "_Project/Services")
            };

            string[] bannedKeywords = { "UnityEngine.AnimationModule", "AnimationClip", "AnimatorController" };

            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string text = File.ReadAllText(file);
                    foreach (string banned in bannedKeywords)
                    {
                        Assert.IsFalse(text.Contains(banned),
                            $"Animation reference '{banned}' found in {file}. Animation is banned outside Presentation per Animation plan §12.");
                    }
                }
            }
        }

        [Test]
        public void MoveResolver_Resolve_ZeroAllocations()
        {
            BoardModel board = new BoardModel();
            board.Set(new GridPos(0, 0), BallColor.Red);
            PreviewQueue preview = new PreviewQueue();
            XorShift128 rng = new XorShift128(12345);
            MoveResolver resolver = new MoveResolver();
            MovePlan targetPlan = new MovePlan();
            MoveRequest request = new MoveRequest(new GridPos(0, 0), new GridPos(1, 1));
            ScoreRules scoreRules = ScoreRules.Default;
            SpawnRules spawnRules = SpawnRules.Default;

            // Warm up
            resolver.Resolve(board, preview, in rng, in request, in scoreRules, in spawnRules, 0, targetPlan);

            long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                resolver.Resolve(board, preview, in rng, in request, in scoreRules, in spawnRules, 0, targetPlan);
            }
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;

            Assert.AreEqual(0, allocatedBytes, $"MoveResolver.Resolve must allocate 0 bytes per call, but allocated {allocatedBytes} bytes over 100 iterations");
        }

        [Test]
        public void GddFolderMapping_ResolvesToExistingArchitectureFolders()
        {
            var mapping = new Dictionary<string, string[]>
            {
                { "Core", new[] { "Core" } },
                { "Gameplay", new[] { "Gameplay" } },
                { "UI", new[] { "Presentation/UI" } },
                { "Audio", new[] { "Presentation/Audio" } },
                { "VFX", new[] { "Presentation/Vfx" } },
                { "Data", new[] { "Data" } },
                { "Services", new[] { "Services" } },
                { "Monetization", new[] { "Services/Ads", "Services/Iap" } },
                { "Analytics", new[] { "Services/Analytics" } },
                { "Editor", new[] { "Editor" } },
                { "Tests", new[] { "Tests" } },
            };

            string projectRoot = Path.Combine(m_ProjectRoot, "_Project");
            foreach (var kvp in mapping)
            {
                foreach (string subPath in kvp.Value)
                {
                    string fullPath = Path.Combine(projectRoot, subPath);
                    Assert.IsTrue(Directory.Exists(fullPath),
                        $"GDD §4 folder '{kvp.Key}' must map to existing architecture folder at '{subPath}' (ADR D25)");
                }
            }
        }

        [Test]
        public void FrozenInterfaces_AllTenDeclared()
        {
            Type[] frozenInterfaces = new[]
            {
                typeof(IRandomSource),
                typeof(IScoreConfig),
                typeof(ISaveService),
                typeof(IDailyChallengeProvider),
                typeof(ILeaderboardProvider),
                typeof(IThemeProvider),
                typeof(IAchievementService),
                typeof(IAnalyticsService),
                typeof(IAdService),
                typeof(IPurchaseService)
            };

            foreach (Type iface in frozenInterfaces)
            {
                Assert.IsNotNull(iface, "Frozen interface type must not be null");
                Assert.IsTrue(iface.IsInterface, $"Type {iface.Name} must be declared as an interface per GDD P0.1");
            }

            Assert.IsTrue(typeof(IIapService).IsAssignableFrom(typeof(IPurchaseService)),
                "IPurchaseService must inherit or alias IIapService per ADR D26");
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
