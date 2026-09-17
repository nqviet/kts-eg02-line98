using System;
using System.Collections.Generic;
using System.IO;
using Line98.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Line98.Services
{
    public static class SaveVersions
    {
        public const int Current = 2;
    }

    [Serializable]
    public sealed class SessionSave
    {
        public bool IsResumable;
        public byte[] BoardCells;
        public int[] PreviewColors;
        public ulong S0, S1, S2, S3;
        public int SelectedIndex = -1;
        public int Score, MoveCount, LinesCleared, LongestLine;
        public int FreeUndosRemaining, RewardedUndosUsed;
        public string ModeId = "classic";
        public string DailySeedDate;
        public int DailySeedVersion = 1;
        public long SavedUtcTicks;
    }

    [Serializable]
    public sealed class StatsSave
    {
        public int GamesPlayed;
        public int GamesCompleted;
        public int BestScore;
        public int TotalScore;
        public int TotalLinesCleared;
        public int LongestLine;
        public int TotalMoves;
        public int HighestCombo;
        public int CurrentDailyStreak;
        public int LongestDailyStreak;

        public int CurrentStreak
        {
            get => CurrentDailyStreak;
            set => CurrentDailyStreak = value;
        }

        public int LongestStreak
        {
            get => LongestDailyStreak;
            set => LongestDailyStreak = value;
        }
    }

    [Serializable]
    public sealed class ProgressSave
    {
        public List<string> UnlockedAchievementIds = new List<string>(10);
    }

    [Serializable]
    public sealed class SettingsSave
    {
        public bool MusicEnabled = true;
        public bool SfxEnabled = true;
        public bool VibrationEnabled = true;
        public bool ReduceEffects;
        public bool PatternHints;
        public string LocaleCode = "en";

        public bool Music
        {
            get => MusicEnabled;
            set => MusicEnabled = value;
        }

        public bool Sfx
        {
            get => SfxEnabled;
            set => SfxEnabled = value;
        }

        public bool Vibration
        {
            get => VibrationEnabled;
            set => VibrationEnabled = value;
        }
    }

    [Serializable]
    public sealed class DailySave
    {
        public List<int> RecentCompletionDays = new List<int>();
        public string LastCompletedDate;
        public int CurrentStreak;
        public int LongestStreak;
        public string LastScoreDate;
        public int LastDailyScore, LastDailyLines, LastDailyMoves;
        public int LastDailySeedVersion = 1;
        public int ContinuesUsedToday;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Version = SaveVersions.Current;
        public SessionSave Session;
        public StatsSave Stats = new StatsSave();
        public ProgressSave Progress = new ProgressSave();
        public SettingsSave Settings = new SettingsSave();
        public DailySave Daily = new DailySave();

        // Legacy flat properties preserved for backward migration from v1
        public byte[] BoardCells;
        public int[] PreviewColors;
        public ulong S0, S1, S2, S3;
        public int Score;
        public int MoveCount;
        public int LinesCleared;
        public int FreeUndos;
        public int BestScore;
        public int TotalGamesPlayed;
        public int LongestLine;
        public string LastDailyDate;
        public int DailyStreak;
    }

    public enum SaveLoadStatus : byte
    {
        Fresh,
        Loaded,
        Migrated,
        RecoveredFromBackup,
        Corrupt
    }

    public readonly struct SaveLoadReport
    {
        public readonly SaveLoadStatus Status;
        public readonly int Version;
        public readonly string Detail;

        public SaveLoadReport(SaveLoadStatus status, int version, string detail = null)
        {
            Status = status;
            Version = version;
            Detail = detail;
        }
    }

    public static class SaveMigrations
    {
        public static SaveData Migrate(SaveData data, int fromVersion)
        {
            if (data == null) return new SaveData();

            if (fromVersion < 2)
            {
                // Migrate v1 legacy flat scalars into v2 structured sub-DTOs
                if (data.Stats == null) data.Stats = new StatsSave();
                if (data.Progress == null) data.Progress = new ProgressSave();
                if (data.Settings == null) data.Settings = new SettingsSave();
                if (data.Daily == null) data.Daily = new DailySave();

                if (data.BestScore > data.Stats.BestScore) data.Stats.BestScore = data.BestScore;
                if (data.TotalGamesPlayed > data.Stats.GamesPlayed) data.Stats.GamesPlayed = data.TotalGamesPlayed;
                if (data.LinesCleared > data.Stats.TotalLinesCleared) data.Stats.TotalLinesCleared = data.LinesCleared;
                if (data.LongestLine > data.Stats.LongestLine) data.Stats.LongestLine = data.LongestLine;
                if (data.DailyStreak > data.Stats.CurrentDailyStreak) data.Stats.CurrentDailyStreak = data.DailyStreak;

                if (!string.IsNullOrEmpty(data.LastDailyDate))
                {
                    data.Daily.LastCompletedDate = data.LastDailyDate;
                }
                if (data.DailyStreak > data.Daily.CurrentStreak)
                {
                    data.Daily.CurrentStreak = data.DailyStreak;
                }
                if (data.DailyStreak > data.Daily.LongestStreak)
                {
                    data.Daily.LongestStreak = data.DailyStreak;
                }

                if (data.BoardCells != null && data.BoardCells.Length == BoardModel.CellCount)
                {
                    data.Session = new SessionSave
                    {
                        IsResumable = true,
                        BoardCells = data.BoardCells,
                        PreviewColors = data.PreviewColors ?? new int[3],
                        S0 = data.S0,
                        S1 = data.S1,
                        S2 = data.S2,
                        S3 = data.S3,
                        Score = data.Score,
                        MoveCount = data.MoveCount,
                        LinesCleared = data.LinesCleared,
                        LongestLine = data.LongestLine,
                        FreeUndosRemaining = data.FreeUndos > 0 ? data.FreeUndos : 3,
                        ModeId = "classic"
                    };
                }

                data.Version = SaveVersions.Current;
            }

            return data;
        }
    }

    public static class SessionSaveMapper
    {
        public static SessionSave ToSave(in SessionState state, long utcTicks)
        {
            int[] previewColors = new int[state.Preview != null ? state.Preview.Length : 0];
            if (state.Preview != null)
            {
                for (int i = 0; i < state.Preview.Length; i++)
                {
                    previewColors[i] = (int)state.Preview[i];
                }
            }

            return new SessionSave
            {
                IsResumable = state.Phase != GamePhase.GameOver && state.Phase != GamePhase.Boot,
                BoardCells = state.BoardCells != null ? (byte[])state.BoardCells.Clone() : null,
                PreviewColors = previewColors,
                S0 = state.Rng.S0,
                S1 = state.Rng.S1,
                S2 = state.Rng.S2,
                S3 = state.Rng.S3,
                SelectedIndex = state.SelectedIndex,
                Score = state.Score,
                MoveCount = state.MoveCount,
                LinesCleared = state.LinesCleared,
                LongestLine = state.LongestLine,
                FreeUndosRemaining = state.FreeUndosRemaining,
                RewardedUndosUsed = state.RewardedUndosUsed,
                ModeId = state.ModeId ?? "classic",
                DailySeedDate = state.DailySeedDate,
                DailySeedVersion = state.DailySeedVersion,
                SavedUtcTicks = utcTicks
            };
        }

        public static bool ToState(SessionSave save, out SessionState state)
        {
            if (save == null || save.BoardCells == null || save.BoardCells.Length != BoardModel.CellCount)
            {
                state = default;
                return false;
            }

            BallColor[] preview = new BallColor[save.PreviewColors != null ? save.PreviewColors.Length : 0];
            if (save.PreviewColors != null)
            {
                for (int i = 0; i < save.PreviewColors.Length; i++)
                {
                    preview[i] = (BallColor)save.PreviewColors[i];
                }
            }

            var rng = new XorShift128();
            rng.RestoreState(save.S0, save.S1, save.S2, save.S3);

            state = new SessionState(
                (byte[])save.BoardCells.Clone(),
                preview,
                in rng,
                save.Score,
                save.MoveCount,
                save.LinesCleared,
                save.LongestLine,
                save.FreeUndosRemaining,
                save.RewardedUndosUsed,
                save.SelectedIndex,
                save.IsResumable ? GamePhase.Playing : GamePhase.GameOver,
                save.ModeId ?? "classic",
                save.DailySeedDate,
                save.DailySeedVersion);

            return true;
        }
    }

    public interface ISaveBackend
    {
        bool Save(string key, string json);
        string Load(string key);
        bool Exists(string key);
        void Delete(string key);
    }

    public interface IBackupSaveBackend : ISaveBackend
    {
        bool HasBackup(string key);
        string LoadBackup(string key);
        void SetBackup(string key, string json);
    }

    public sealed class InMemorySaveBackend : IBackupSaveBackend
    {
        private readonly Dictionary<string, string> m_Storage = new Dictionary<string, string>();
        private readonly Dictionary<string, string> m_Backups = new Dictionary<string, string>();

        public bool Save(string key, string json)
        {
            if (m_Storage.TryGetValue(key, out string existing))
            {
                m_Backups[key] = existing;
            }
            else
            {
                m_Backups[key] = json;
            }
            m_Storage[key] = json;
            return true;
        }

        public string Load(string key)
        {
            return m_Storage.TryGetValue(key, out string json) ? json : null;
        }

        public bool Exists(string key) => m_Storage.ContainsKey(key);
        public void Delete(string key)
        {
            m_Storage.Remove(key);
            m_Backups.Remove(key);
        }

        public bool HasBackup(string key) => m_Backups.ContainsKey(key);
        public string LoadBackup(string key) => m_Backups.TryGetValue(key, out string json) ? json : null;
        public void SetBackup(string key, string json) => m_Backups[key] = json;
    }

    public sealed class FileSaveBackend : IBackupSaveBackend
    {
        private readonly string m_BasePath;

        public bool WasLoadedFromBackup { get; private set; }

        public FileSaveBackend(string basePath = null)
        {
            m_BasePath = basePath ?? Application.persistentDataPath;
        }

        public bool Save(string key, string json)
        {
            try
            {
                string targetPath = Path.Combine(m_BasePath, $"{key}.json");
                string tempPath = Path.Combine(m_BasePath, $"{key}.tmp");
                string bakPath = Path.Combine(m_BasePath, $"{key}.bak");

                File.WriteAllText(tempPath, json);

                if (File.Exists(targetPath))
                {
                    if (File.Exists(bakPath))
                    {
                        File.Delete(bakPath);
                    }
                    File.Replace(tempPath, targetPath, bakPath);
                }
                else
                {
                    File.Move(tempPath, targetPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveBackend] Failed to save {key}: {ex.Message}");
                return false;
            }
        }

        public string Load(string key)
        {
            WasLoadedFromBackup = false;
            try
            {
                string targetPath = Path.Combine(m_BasePath, $"{key}.json");
                string bakPath = Path.Combine(m_BasePath, $"{key}.bak");

                if (File.Exists(targetPath))
                {
                    return File.ReadAllText(targetPath);
                }

                if (File.Exists(bakPath))
                {
                    Debug.LogWarning($"[FileSaveBackend] Recovering from backup for {key}");
                    WasLoadedFromBackup = true;
                    return File.ReadAllText(bakPath);
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveBackend] Failed to load {key}: {ex.Message}");
                return null;
            }
        }

        public bool Exists(string key)
        {
            string targetPath = Path.Combine(m_BasePath, $"{key}.json");
            string bakPath = Path.Combine(m_BasePath, $"{key}.bak");
            return File.Exists(targetPath) || File.Exists(bakPath);
        }

        public void Delete(string key)
        {
            string targetPath = Path.Combine(m_BasePath, $"{key}.json");
            string bakPath = Path.Combine(m_BasePath, $"{key}.bak");
            if (File.Exists(targetPath)) File.Delete(targetPath);
            if (File.Exists(bakPath)) File.Delete(bakPath);
        }

        public bool HasBackup(string key)
        {
            string bakPath = Path.Combine(m_BasePath, $"{key}.bak");
            return File.Exists(bakPath);
        }

        public string LoadBackup(string key)
        {
            string bakPath = Path.Combine(m_BasePath, $"{key}.bak");
            return File.Exists(bakPath) ? File.ReadAllText(bakPath) : null;
        }

        public void SetBackup(string key, string json)
        {
            string bakPath = Path.Combine(m_BasePath, $"{key}.bak");
            File.WriteAllText(bakPath, json);
        }
    }

    public interface ISaveService
    {
        bool SaveGame(SaveData data);
        SaveData LoadGame();
        bool HasSave();
        void ClearSave();
        int LoadedVersion { get; }
        SaveLoadReport LastLoad { get; }
        bool TrySaveSession(in SessionState state, long utcTicks = 0);
    }

    public sealed class SaveService : ISaveService
    {
        private const string s_SaveKey = "line98_save";
        private readonly ISaveBackend m_Backend;
        private int m_LoadedVersion;
        private SaveLoadReport m_LastLoad;

        public int LoadedVersion => m_LoadedVersion;
        public SaveLoadReport LastLoad => m_LastLoad;

        public SaveService(ISaveBackend backend = null)
        {
            m_Backend = backend ?? new FileSaveBackend();
            m_LastLoad = new SaveLoadReport(SaveLoadStatus.Fresh, SaveVersions.Current);
        }

        public bool SaveGame(SaveData data)
        {
            if (data == null) return false;
            data.Version = SaveVersions.Current;
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            return m_Backend.Save(s_SaveKey, json);
        }

        public SaveData LoadGame()
        {
            string json = m_Backend.Load(s_SaveKey);
            bool loadedFromBackupInitially = m_Backend is FileSaveBackend fsb && fsb.WasLoadedFromBackup;

            if (string.IsNullOrEmpty(json))
            {
                if (m_Backend is IBackupSaveBackend backupBackend && backupBackend.HasBackup(s_SaveKey))
                {
                    string bakJson = backupBackend.LoadBackup(s_SaveKey);
                    if (!string.IsNullOrEmpty(bakJson))
                    {
                        try
                        {
                            SaveData bakData = JsonConvert.DeserializeObject<SaveData>(bakJson);
                            if (bakData != null)
                            {
                                if (bakData.Version < SaveVersions.Current)
                                {
                                    bakData = SaveMigrations.Migrate(bakData, bakData.Version);
                                }
                                m_LoadedVersion = bakData.Version;
                                m_LastLoad = new SaveLoadReport(SaveLoadStatus.RecoveredFromBackup, bakData.Version, "Recovered from backup");
                                SaveGame(bakData);
                                return bakData;
                            }
                        }
                        catch { }
                    }
                }

                m_LoadedVersion = SaveVersions.Current;
                m_LastLoad = new SaveLoadReport(SaveLoadStatus.Fresh, SaveVersions.Current, "No save found");
                return new SaveData();
            }

            try
            {
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
                if (data == null)
                {
                    throw new Exception("Deserialized null");
                }

                m_LoadedVersion = data.Version;

                if (loadedFromBackupInitially)
                {
                    if (data.Version < SaveVersions.Current)
                    {
                        data = SaveMigrations.Migrate(data, data.Version);
                    }
                    m_LastLoad = new SaveLoadReport(SaveLoadStatus.RecoveredFromBackup, data.Version, "Recovered from backup");
                    SaveGame(data);
                }
                else if (data.Version < SaveVersions.Current)
                {
                    data = SaveMigrations.Migrate(data, data.Version);
                    m_LastLoad = new SaveLoadReport(SaveLoadStatus.Migrated, data.Version, $"Migrated from {m_LoadedVersion}");
                    SaveGame(data);
                }
                else
                {
                    m_LastLoad = new SaveLoadReport(SaveLoadStatus.Loaded, data.Version, "Loaded successfully");
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Error deserializing save: {ex.Message}. Checking backup...");
                if (m_Backend is IBackupSaveBackend backupBackend && backupBackend.HasBackup(s_SaveKey))
                {
                    string bakJson = backupBackend.LoadBackup(s_SaveKey);
                    if (!string.IsNullOrEmpty(bakJson))
                    {
                        try
                        {
                            SaveData bakData = JsonConvert.DeserializeObject<SaveData>(bakJson);
                            if (bakData != null)
                            {
                                if (bakData.Version < SaveVersions.Current)
                                {
                                    bakData = SaveMigrations.Migrate(bakData, bakData.Version);
                                }
                                m_LoadedVersion = bakData.Version;
                                m_LastLoad = new SaveLoadReport(SaveLoadStatus.RecoveredFromBackup, bakData.Version, "Recovered from backup");
                                SaveGame(bakData);
                                return bakData;
                            }
                        }
                        catch { }
                    }
                }

                m_LoadedVersion = SaveVersions.Current;
                m_LastLoad = new SaveLoadReport(SaveLoadStatus.Corrupt, SaveVersions.Current, ex.Message);
                return new SaveData();
            }
        }

        public bool TrySaveSession(in SessionState state, long utcTicks = 0)
        {
            SaveData existing = LoadGame();
            if (utcTicks == 0) utcTicks = DateTime.UtcNow.Ticks;
            existing.Session = SessionSaveMapper.ToSave(state, utcTicks);
            return SaveGame(existing);
        }

        public bool HasSave() => m_Backend.Exists(s_SaveKey);
        public void ClearSave() => m_Backend.Delete(s_SaveKey);
    }
}
