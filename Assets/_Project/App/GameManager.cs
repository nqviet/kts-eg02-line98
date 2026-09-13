using System;
using Line98.Core;
using Line98.Gameplay;
using UnityEngine;

namespace Line98.App
{
    /// <summary>
    /// Coordinates high-level game lifecycle and mode switching.
    /// Does not contain puzzle simulation logic (which belongs strictly to GameSession/Core).
    /// </summary>
    public sealed class GameManager
    {
        private static GameManager s_Instance;
        public static GameManager Instance => s_Instance;

        private GameSession m_ActiveSession;

        public GameSession ActiveSession => m_ActiveSession;

        public event Action<IGameModeStrategy> OnModeChanged;

        public GameManager(GameSession session = null)
        {
            s_Instance = this;
            m_ActiveSession = session ?? new GameSession();
        }

        public void StartClassicGame(uint? customSeed = null)
        {
            var mode = new ClassicMode();
            m_ActiveSession.SetMode(mode);
            m_ActiveSession.StartNewGame(customSeed);
            OnModeChanged?.Invoke(mode);
        }

        public void StartDailyChallenge(string dateString = null)
        {
            string date = dateString ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            var mode = new DailyChallengeMode(date);
            m_ActiveSession.SetMode(mode);
            m_ActiveSession.StartNewGame();
            OnModeChanged?.Invoke(mode);
        }

        public void StartZenMode()
        {
            var mode = new ZenMode();
            m_ActiveSession.SetMode(mode);
            m_ActiveSession.StartNewGame();
            OnModeChanged?.Invoke(mode);
        }
    }
}
