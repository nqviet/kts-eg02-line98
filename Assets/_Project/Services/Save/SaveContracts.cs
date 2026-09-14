using System;
using System.IO;
using Line98.Core;
using UnityEngine;

namespace Line98.Services
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
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

    public interface ISaveBackend
    {
        bool Save(string key, string json);
        string Load(string key);
        bool Exists(string key);
        void Delete(string key);
    }

    public sealed class InMemorySaveBackend : ISaveBackend
    {
        private readonly System.Collections.Generic.Dictionary<string, string> m_Storage
            = new System.Collections.Generic.Dictionary<string, string>();

        public bool Save(string key, string json)
        {
            m_Storage[key] = json;
            return true;
        }

        public string Load(string key)
        {
            return m_Storage.TryGetValue(key, out string json) ? json : null;
        }

        public bool Exists(string key) => m_Storage.ContainsKey(key);
        public void Delete(string key) => m_Storage.Remove(key);
    }

    public sealed class FileSaveBackend : ISaveBackend
    {
        private readonly string m_BasePath;

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
    }

    public interface ISaveService
    {
        bool SaveGame(SaveData data);
        SaveData LoadGame();
        bool HasSave();
        void ClearSave();
    }

    public sealed class SaveService : ISaveService
    {
        private const string s_SaveKey = "line98_save";
        private readonly ISaveBackend m_Backend;

        public SaveService(ISaveBackend backend = null)
        {
            m_Backend = backend ?? new FileSaveBackend();
        }

        public bool SaveGame(SaveData data)
        {
            if (data == null) return false;
            string json = JsonUtility.ToJson(data, true);
            return m_Backend.Save(s_SaveKey, json);
        }

        public SaveData LoadGame()
        {
            string json = m_Backend.Load(s_SaveKey);
            if (string.IsNullOrEmpty(json))
            {
                return new SaveData();
            }
            try
            {
                return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch
            {
                return new SaveData();
            }
        }

        public bool HasSave() => m_Backend.Exists(s_SaveKey);
        public void ClearSave() => m_Backend.Delete(s_SaveKey);
    }
}
