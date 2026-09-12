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

        public GameManager(GameSession session = null)
        {
            s_Instance = this;
            m_ActiveSession = session ?? new GameSession();
        }

        public void StartClassicGame(uint? customSeed = null)
        {
            m_ActiveSession.SetMode(new ClassicMode());
            m_ActiveSession.StartNewGame(customSeed);
        }

        public void StartDailyChallenge(string dateString = null)
        {
            string date = dateString ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            m_ActiveSession.SetMode(new DailyChallengeMode(date));
            m_ActiveSession.StartNewGame();
        }

        public void StartZenMode()
        {
            m_ActiveSession.SetMode(new ZenMode());
            m_ActiveSession.StartNewGame();
        }
    }
}
