using System;
using Line98.Core;
using Line98.Gameplay;
using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class SaveServiceTests
    {
        [Test]
        public void RoundTripSave_PreservesData()
        {
            var backend = new InMemorySaveBackend();
            var service = new SaveService(backend);

            var originalData = new SaveData
            {
                Score = 1500,
                MoveCount = 42,
                LinesCleared = 7,
                FreeUndos = 2,
                BestScore = 3200
            };

            bool saved = service.SaveGame(originalData);
            Assert.IsTrue(saved);

            SaveData loaded = service.LoadGame();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1500, loaded.Score);
            Assert.AreEqual(42, loaded.MoveCount);
            Assert.AreEqual(7, loaded.LinesCleared);
            Assert.AreEqual(2, loaded.FreeUndos);
            Assert.AreEqual(3200, loaded.BestScore);
        }

        [Test]
        public void FullSessionState_RoundTripsExactly()
        {
            var session = new GameSession();
            session.StartNewGame(12345U);

            SessionState originalState = session.CaptureState();
            SessionSave saveDto = SessionSaveMapper.ToSave(originalState, 1000000L);

            Assert.IsTrue(SessionSaveMapper.ToState(saveDto, out SessionState restoredState));

            CollectionAssert.AreEqual(originalState.BoardCells, restoredState.BoardCells);
            CollectionAssert.AreEqual(originalState.Preview, restoredState.Preview);
            Assert.AreEqual(originalState.Rng.S0, restoredState.Rng.S0);
            Assert.AreEqual(originalState.Rng.S1, restoredState.Rng.S1);
            Assert.AreEqual(originalState.Score, restoredState.Score);
            Assert.AreEqual(originalState.MoveCount, restoredState.MoveCount);
            Assert.AreEqual(originalState.LinesCleared, restoredState.LinesCleared);
            Assert.AreEqual(originalState.LongestLine, restoredState.LongestLine);
            Assert.AreEqual(originalState.FreeUndosRemaining, restoredState.FreeUndosRemaining);
            Assert.AreEqual(originalState.Phase, restoredState.Phase);
            Assert.AreEqual(originalState.ModeId, restoredState.ModeId);
        }

        [Test]
        public void SaveMigrations_V1toV2_FlattensScalarsIntoSubDtos()
        {
            var v1Data = new SaveData
            {
                Version = 1,
                Score = 500,
                MoveCount = 20,
                LinesCleared = 5,
                BestScore = 2000,
                TotalGamesPlayed = 15,
                DailyStreak = 4,
                LongestLine = 6
            };

            SaveData v2Data = SaveMigrations.Migrate(v1Data, 1);

            Assert.AreEqual(SaveVersions.Current, v2Data.Version);
            Assert.IsNotNull(v2Data.Stats);
            Assert.AreEqual(2000, v2Data.Stats.BestScore);
            Assert.AreEqual(15, v2Data.Stats.GamesPlayed);
            Assert.AreEqual(4, v2Data.Stats.CurrentStreak);
            Assert.AreEqual(6, v2Data.Stats.LongestLine);
            Assert.IsNotNull(v2Data.Daily);
            Assert.AreEqual(4, v2Data.Daily.CurrentStreak);
        }

        [Test]
        public void CorruptSave_RecoversFromBackup()
        {
            var backend = new InMemorySaveBackend();
            var service = new SaveService(backend);

            // 1. Initial valid save
            var validData = new SaveData { BestScore = 5000, Version = 2 };
            service.SaveGame(validData);

            // 2. Corrupt primary storage with invalid JSON
            backend.Save("line98_save", "{ invalid json content !!");

            // 3. Load should detect corruption and recover from backup
            SaveData loaded = service.LoadGame();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(5000, loaded.BestScore);
            Assert.AreEqual(SaveLoadStatus.RecoveredFromBackup, service.LastLoad.Status);
        }

        [Test]
        public void MissingSave_RecoversFromBackup()
        {
            var backend = new InMemorySaveBackend();
            var service = new SaveService(backend);

            var validData = new SaveData { BestScore = 4000, Version = 2 };
            service.SaveGame(validData);

            // Delete primary, leaving backup intact
            string backupContent = backend.LoadBackup("line98_save");
            backend.Delete("line98_save");
            backend.SetBackup("line98_save", backupContent);

            SaveData loaded = service.LoadGame();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(4000, loaded.BestScore);
            Assert.AreEqual(SaveLoadStatus.RecoveredFromBackup, service.LastLoad.Status);
        }

        [Test]
        public void CosmeticsExcluded_FromSaveData()
        {
            // Assert that SaveData does not contain cosmetic selection fields (ADR D29 / P3.1)
            Type saveData = typeof(SaveData);
            Assert.IsNull(saveData.GetField("ActiveBallTheme"));
            Assert.IsNull(saveData.GetField("ActiveBoardTheme"));
            Assert.IsNull(saveData.GetField("ActiveUiTheme"));
            Assert.IsNull(saveData.GetProperty("ActiveBallTheme"));
        }

        [Test]
        public void AutosaveMatrix_SetsIsResumableFalseOnGameOver()
        {
            var session = new GameSession();
            session.StartNewGame(42U);

            SessionState playingState = session.CaptureState();
            SessionSave playingSave = SessionSaveMapper.ToSave(playingState, 100L);
            Assert.IsTrue(playingSave.IsResumable);

            // Trigger game over
            session.Board.ImportCells(new byte[81]);
            for (int i = 0; i < 81; i++)
            {
                session.Board.Set(i, BallColor.Red);
            }

            var dummyPlan = new MovePlan
            {
                Outcome = MoveOutcome.GameOver,
                IsGameOver = true
            };
            session.Commit(dummyPlan);

            Assert.AreEqual(GamePhase.GameOver, session.Phase);
            SessionState gameOverState = session.CaptureState();
            SessionSave gameOverSave = SessionSaveMapper.ToSave(gameOverState, 200L);
            Assert.IsFalse(gameOverSave.IsResumable);
        }
    }
}
