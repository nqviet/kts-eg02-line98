using System;
using System.IO;
using Line98.App;
using Line98.Services;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.PlayMode
{
    [SetUpFixture]
    public sealed class PlayModeSaveIsolation
    {
        private const string s_CosmeticSaveFileName = "line98_cosmetics.json";
        private const string s_CosmeticBackupFileName = "line98_cosmetics.bak";

        private Func<ISaveBackend> m_PreviousFactory;
        private FileSnapshot m_CosmeticSaveSnapshot;
        private FileSnapshot m_CosmeticBackupSnapshot;

        [OneTimeSetUp]
        public void OnOneTimeSetUp()
        {
            m_PreviousFactory = AppRoot.SaveBackendFactory;
            m_CosmeticSaveSnapshot = FileSnapshot.Capture(Path.Combine(Application.persistentDataPath, s_CosmeticSaveFileName));
            m_CosmeticBackupSnapshot = FileSnapshot.Capture(Path.Combine(Application.persistentDataPath, s_CosmeticBackupFileName));
            AppRoot.SaveBackendFactory = () => new InMemorySaveBackend();
        }

        [OneTimeTearDown]
        public void OnOneTimeTearDown()
        {
            AppRoot.SaveBackendFactory = m_PreviousFactory;
            m_CosmeticSaveSnapshot.AssertUnchanged();
            m_CosmeticBackupSnapshot.AssertUnchanged();
        }

        private readonly struct FileSnapshot
        {
            private readonly string m_Path;
            private readonly bool m_Exists;
            private readonly byte[] m_Contents;

            private FileSnapshot(string path, bool exists, byte[] contents)
            {
                m_Path = path;
                m_Exists = exists;
                m_Contents = contents;
            }

            public static FileSnapshot Capture(string path)
            {
                bool exists = File.Exists(path);
                return new FileSnapshot(path, exists, exists ? File.ReadAllBytes(path) : Array.Empty<byte>());
            }

            public void AssertUnchanged()
            {
                Assert.AreEqual(m_Exists, File.Exists(m_Path), $"PlayMode tests changed whether the real save exists: {m_Path}");
                if (m_Exists)
                {
                    CollectionAssert.AreEqual(m_Contents, File.ReadAllBytes(m_Path), $"PlayMode tests modified the real save: {m_Path}");
                }
            }
        }
    }
}
