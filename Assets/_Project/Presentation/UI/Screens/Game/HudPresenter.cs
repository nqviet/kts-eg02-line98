using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Master HUD presenter: the single authority binding GameSession state to UI views.
    /// Manages score/best counters, preview queue, undo states, mode variants (Zen/Daily),
    /// action button callbacks, and crown pop celebrations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudPresenter : MonoBehaviour
    {
        [Header("Score & Best")]
        [SerializeField] private RollingNumber m_ScoreNumber;
        [SerializeField] private RollingNumber m_BestNumber;
        [SerializeField] private RectTransform m_BestCrown;
        [SerializeField] private GameObject m_ScoreCard;
        [SerializeField] private GameObject m_BestCard;

        [Header("Preview Queue")]
        [SerializeField] private PreviewQueueView m_PreviewView;

        [Header("Action Controls")]
        [SerializeField] private UndoButtonView m_UndoButtonView;
        [SerializeField] private Button m_HintButton;
        [SerializeField] private Button m_NewGameButton;
        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private Button m_StatsButton;

        [Header("Semantic Action Controls")]
        [SerializeField] private UiActionButton m_UndoActionButton;
        [SerializeField] private UiActionButton m_HintActionButton;
        [SerializeField] private UiActionButton m_NewGameActionButton;
        [SerializeField] private UiActionButton m_SettingsActionButton;
        [SerializeField] private UiActionButton m_StatsActionButton;
        [SerializeField] private UiActionButton m_GameOverNewGameActionButton;

        [Header("Sub-Systems & Theme")]
        [SerializeField] private UiShell m_UIRouter;
        [SerializeField] private UiThemeSO m_Theme;

        private GameSession m_Session;
        private TweenRunner m_TweenRunner;
        private int m_BestScore;
        private bool m_HasPoppedCrownThisSession;
        private bool m_AreButtonsBound;

        public RollingNumber ScoreNumber => m_ScoreNumber;
        public RollingNumber BestNumber => m_BestNumber;
        public PreviewQueueView PreviewView => m_PreviewView;
        public UndoButtonView UndoButtonView => m_UndoButtonView;
        public Button HintButton => m_HintButton;
        public Button NewGameButton => m_NewGameButton;
        public Button SettingsButton => m_SettingsButton;
        public Button StatsButton => m_StatsButton;

        public void Initialize(GameSession session, UiShell uiRouter, TweenRunner tweenRunner, UiThemeSO theme = null)
        {
            UnbindSession();
            UnbindButtons();

            m_Session = session;
            m_UIRouter = uiRouter;
            m_TweenRunner = tweenRunner;
            if (theme != null) m_Theme = theme;

            BindButtons();
            BindSession();
            RefreshAllViews();
        }

        public void ApplyTheme(UiThemeSO theme, BallThemeSO ballTheme = null)
        {
            if (theme != null)
            {
                m_Theme = theme;
                if (m_PreviewView != null && theme.PreviewSpriteSet != null)
                {
                    m_PreviewView.SetSpriteSet(theme.PreviewSpriteSet);
                }
            }

            if (ballTheme != null && m_PreviewView != null)
            {
                m_PreviewView.SetSpriteTints(ballTheme.PreviewTints);
            }

            RefreshAllViews();
        }

        private void BindButtons()
        {
            Subscribe(m_UndoActionButton, m_UndoButtonView != null ? m_UndoButtonView.Button : null, OnUndoClicked);
            Subscribe(m_HintActionButton, m_HintButton, OnHintClicked);
            Subscribe(m_NewGameActionButton, m_NewGameButton, OnNewGameClicked);
            Subscribe(m_SettingsActionButton, m_SettingsButton, OnSettingsClicked);
            Subscribe(m_StatsActionButton, m_StatsButton, OnStatsClicked);
            Subscribe(m_GameOverNewGameActionButton, m_UIRouter != null && m_UIRouter.GameOverPopup != null ? m_UIRouter.GameOverPopup.NewGameButton : null, OnGameOverNewGameClicked);
            m_AreButtonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!m_AreButtonsBound)
            {
                return;
            }

            Unsubscribe(m_UndoActionButton, m_UndoButtonView != null ? m_UndoButtonView.Button : null, OnUndoClicked);
            Unsubscribe(m_HintActionButton, m_HintButton, OnHintClicked);
            Unsubscribe(m_NewGameActionButton, m_NewGameButton, OnNewGameClicked);
            Unsubscribe(m_SettingsActionButton, m_SettingsButton, OnSettingsClicked);
            Unsubscribe(m_StatsActionButton, m_StatsButton, OnStatsClicked);
            Unsubscribe(m_GameOverNewGameActionButton, m_UIRouter != null && m_UIRouter.GameOverPopup != null ? m_UIRouter.GameOverPopup.NewGameButton : null, OnGameOverNewGameClicked);
            m_AreButtonsBound = false;
        }

        private static void Subscribe(UiActionButton semanticButton, Button legacyButton, UnityAction callback)
        {
            if (semanticButton != null)
            {
                semanticButton.OnPressed += callback;
            }
            else if (legacyButton != null)
            {
                legacyButton.onClick.AddListener(callback);
            }
        }

        private static void Unsubscribe(UiActionButton semanticButton, Button legacyButton, UnityAction callback)
        {
            if (semanticButton != null)
            {
                semanticButton.OnPressed -= callback;
            }
            else if (legacyButton != null)
            {
                legacyButton.onClick.RemoveListener(callback);
            }
        }

        private void BindSession()
        {
            if (m_Session == null) return;

            m_Session.OnScoreChanged += HandleScoreChanged;
            m_Session.OnMoveCommitted += HandleMoveCommitted;
            m_Session.OnStateRestored += HandleStateRestored;
            m_Session.OnPhaseChanged += HandlePhaseChanged;
            m_Session.OnGameOver += HandleGameOver;
        }

        private void UnbindSession()
        {
            if (m_Session == null) return;

            m_Session.OnScoreChanged -= HandleScoreChanged;
            m_Session.OnMoveCommitted -= HandleMoveCommitted;
            m_Session.OnStateRestored -= HandleStateRestored;
            m_Session.OnPhaseChanged -= HandlePhaseChanged;
            m_Session.OnGameOver -= HandleGameOver;
        }

        public void RefreshAllViews()
        {
            if (m_Session == null) return;

            bool isZen = !m_Session.Mode.ScoringEnabled;
            if (m_ScoreCard != null) m_ScoreCard.SetActive(!isZen);
            if (m_BestCard != null) m_BestCard.SetActive(!isZen);

            if (m_ScoreNumber != null)
            {
                m_ScoreNumber.SetValue(m_Session.Score, animate: false);
            }

            if (m_BestNumber != null)
            {
                if (m_Session.Score > m_BestScore) m_BestScore = m_Session.Score;
                m_BestNumber.SetValue(m_BestScore, animate: false);
            }

            if (m_PreviewView != null)
            {
                m_PreviewView.SetQueue(m_Session.PreviewQueue, m_Session.Board.EmptyCount);
            }

            UpdateActionButtonsState();
        }

        private void HandleScoreChanged(int newScore)
        {
            if (m_ScoreNumber != null)
            {
                m_ScoreNumber.SetValue(newScore, animate: true);
            }

            if (newScore > m_BestScore)
            {
                m_BestScore = newScore;
                if (m_BestNumber != null)
                {
                    m_BestNumber.SetValue(m_BestScore, animate: true);
                }

                if (!m_HasPoppedCrownThisSession)
                {
                    TriggerCrownCelebration();
                }
            }
        }

        private void HandleMoveCommitted(MoveResult result)
        {
            if (m_PreviewView != null && m_Session != null)
            {
                m_PreviewView.SetQueue(m_Session.PreviewQueue, m_Session.Board.EmptyCount);
            }

            UpdateActionButtonsState();
        }

        private void HandleStateRestored(GameSnapshot snapshot)
        {
            if (m_ScoreNumber != null)
            {
                m_ScoreNumber.SetValue(snapshot.Score, animate: true);
            }

            if (m_PreviewView != null && m_Session != null)
            {
                m_PreviewView.SetQueue(m_Session.PreviewQueue, m_Session.Board.EmptyCount);
            }

            UpdateActionButtonsState();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            UpdateActionButtonsState();
        }

        private void HandleGameOver(SessionSummary summary)
        {
            UpdateActionButtonsState();

            if (m_UIRouter != null)
            {
                if (m_BestScore > summary.BestScore)
                {
                    summary = new SessionSummary(
                        summary.FinalScore,
                        m_BestScore,
                        summary.LongestLine,
                        summary.LinesCleared,
                        summary.TotalMoves,
                        summary.CanContinue);
                }

                m_UIRouter.OpenGameOver(summary);
            }
        }

        private void UpdateActionButtonsState()
        {
            if (m_Session == null) return;

            bool isPlaying = m_Session.Phase == GamePhase.Playing;
            bool undoAllowed = m_Session.Mode.UndoAllowed;

            if (m_UndoButtonView != null)
            {
                bool canUndo = isPlaying && m_Session.MoveCount > 0;
                m_UndoButtonView.SetUndoState(
                    m_Session.FreeUndosRemaining,
                    canUndo,
                    isAdAvailable: true,
                    isModeAllowed: undoAllowed);
            }

            if (m_HintButton != null)
            {
                bool interactable = isPlaying && m_Session.Mode.HintsAllowed;
                if (m_HintActionButton != null) m_HintActionButton.SetInteractable(interactable);
                else m_HintButton.interactable = interactable;
            }

            if (m_NewGameButton != null)
            {
                bool interactable = isPlaying || m_Session.Phase == GamePhase.GameOver;
                if (m_NewGameActionButton != null) m_NewGameActionButton.SetInteractable(interactable);
                else m_NewGameButton.interactable = interactable;
            }
        }

        private void TriggerCrownCelebration()
        {
            m_HasPoppedCrownThisSession = true;

            if (m_BestCrown != null && m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(m_BestCrown);

                // Pop scale: 1.0 -> 1.35 -> 1.0 over 0.90s
                var popTween = new Tween
                {
                    From = 1.0f,
                    To = 1.35f,
                    Duration = 0.40f,
                    Ease = Easing.OutBack,
                    Owner = m_BestCrown,
                    OnUpdate = val => m_BestCrown.localScale = new Vector3(val, val, 1.0f),
                    OnComplete = () =>
                    {
                        var settleTween = new Tween
                        {
                            From = 1.35f,
                            To = 1.0f,
                            Duration = 0.50f,
                            Ease = Easing.OutCubic,
                            Owner = m_BestCrown,
                            OnUpdate = val => m_BestCrown.localScale = new Vector3(val, val, 1.0f)
                        };
                        m_TweenRunner.Play(in settleTween);
                    }
                };
                m_TweenRunner.Play(in popTween);
            }
        }

        private void OnUndoClicked()
        {
            if (m_Session == null || m_Session.Phase != GamePhase.Playing) return;
            m_Session.TryUndo();
        }

        private void OnHintClicked()
        {
            if (m_Session == null || m_Session.Phase != GamePhase.Playing) return;

            if (m_Session.RequestHint(out GridPos from, out GridPos to))
            {
                // Select hint ball
                m_Session.TrySelect(from);
            }
        }

        private void OnNewGameClicked()
        {
            if (m_Session == null) return;

            if (m_Session.MoveCount > 0 && m_Session.Phase == GamePhase.Playing && m_UIRouter != null)
            {
                m_UIRouter.OpenConfirm(
                    "New Game?",
                    "Are you sure you want to restart? Current progress will be lost.",
                    () =>
                    {
                        m_HasPoppedCrownThisSession = false;
                        m_Session.StartNewGame();
                        RefreshAllViews();
                    });
            }
            else
            {
                m_HasPoppedCrownThisSession = false;
                m_Session.StartNewGame();
                RefreshAllViews();
            }
        }

        private void OnSettingsClicked()
        {
            if (m_UIRouter != null)
            {
                m_UIRouter.OpenSettings();
            }
        }

        private void OnGameOverNewGameClicked()
        {
            // Close through the shell so the modal stack and the scrim stay consistent:
            // calling PopupView.Hide() directly leaves the popup on the stack.
            if (m_UIRouter != null && !m_UIRouter.TryClosePopup(UiPopupId.GameOver))
            {
                m_UIRouter.CloseAllPopups();
            }

            m_Session?.StartNewGame();
        }

        private void OnStatsClicked()
        {
            if (m_UIRouter != null && m_Session != null)
            {
                int avgScore = m_Session.MoveCount > 0 ? (m_Session.Score / m_Session.MoveCount) : 0;
                m_UIRouter.OpenStatistics(
                    gamesPlayed: 1,
                    bestScore: m_BestScore,
                    totalLines: m_Session.LinesCleared,
                    avgScore: avgScore);
            }
        }

        private void OnDestroy()
        {
            UnbindSession();
            UnbindButtons();
        }
    }
}
