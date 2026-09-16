// ===============================================================================================
// Debug HUD - development-only IMGUI overlay (the whole file is behind DEBUG || UNITY_EDITOR).
//
// Hotkeys
//   F1  Show or hide the hub (it starts hidden).
//
// Imperative usage
//   Every IMGUI control is a thin layer over the public API below, so tools, automated
//   playthroughs and console commands can drive the hub the same way the buttons do:
//
//     DebugHud hud = DebugHud.Instance;          // null when no hub exists in the scene
//     hud?.SetHudVisible(true);                  // ToggleHud(), IsHudVisible, ActiveTab, SetTab(DebugTab.PopupHub)
//     hud.StatusMessage = "Pushed from a tool.";
//
//     hud?.StartStandaloneSession();             // starts a session where AppRoot is absent
//     hud?.StartZenGame();                       // StartClassicGame(), StartDailyChallenge()
//     hud?.RequestHint();                        // UndoMove(), ClearHint(), Revive(balls)
//
//     hud?.SelectMockFixture(3);                 // CycleMockFixture(), MockSummaryIndex
//     hud?.OpenMockPopup(UiPopupId.Settings);    // ToggleMockPopup(), CloseMockPopup(), CloseAllPopups()
//
//   Popup ids - Line98.Presentation.UiPopupId (the example above assumes a
//   `using Line98.Presentation;`). IsPopupOpen, ToggleMockPopup, OpenMockPopup and
//   CloseMockPopup accept any of them:
//     GameOver    mock session summary; SelectMockFixture / CycleMockFixture swap it live
//     Confirm     mock "Debug Confirm" prompt, its accept is reported in the status line
//     Statistics  mock stats: 7 games, best 3,480, 42 lines, 512 average score
//     Settings    opened through the shell so it receives the real catalog and selector
//     Cosmetics   the "Themes" button; opened through the shell for the real theme catalog
//
//   The hub lives on the persistent AppRoot when a Boot scene exists; otherwise
//   AutoEnsureDebugHud creates one after the first scene load. Calling into it is safe from
//   any scene: nothing throws, misses come back as false (or a null payload) and usually
//   leave a StatusMessage explaining why.
// ===============================================================================================
#if DEBUG || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using Line98.Core;
using Line98.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Line98.App
{
    /// <summary>
    /// Interactive IMGUI debug interface for Milestone 1.
    /// Provides immediate playable verification of all core mechanics:
    /// selection, BFS path traversal, line clears, preview queue, score, and game over.
    /// Also hosts the popup debug hub, which shows/hides every UiShell popup with mock data.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        /// <summary>Section of the hub, selectable from code through <see cref="SetTab"/>.</summary>
        public enum DebugTab
        {
            All,
            PopupHub,
            Gameplay
        }

        private static DebugHud s_Instance;

        private GameSession m_Session;
        private GridPos? m_HintFrom;
        private GridPos? m_HintTo;
        private string m_StatusMessage = "Ready";
        private Presentation.UiShell m_Shell;
        private int m_MockSummaryIndex;
        private bool m_IsHudVisible;
        private Vector2 m_ScrollPosition;
        private DebugTab m_ActiveTab = DebugTab.All;

        /// <summary>
        /// Mock game-over payloads covering every conditional branch of the popup:
        /// new-best badge, continue-button visibility, N0 thousands separators, and empty state.
        /// </summary>
        private static readonly SessionSummary[] s_MockSummaries =
        {
            new SessionSummary(finalScore: 1250, bestScore: 3480, longestLine: 6, linesCleared: 8, totalMoves: 41, canContinue: true),
            new SessionSummary(finalScore: 7890, bestScore: 7890, longestLine: 7, linesCleared: 11, totalMoves: 63, canContinue: true),
            new SessionSummary(finalScore: 430, bestScore: 980, longestLine: 5, linesCleared: 3, totalMoves: 22, canContinue: false),
            new SessionSummary(finalScore: 1234567, bestScore: 9999999, longestLine: 9, linesCleared: 14, totalMoves: 12345, canContinue: true),
            new SessionSummary(finalScore: 0, bestScore: 0, longestLine: 0, linesCleared: 0, totalMoves: 0, canContinue: false),
        };

        /// <summary>
        /// The hub currently running, or null when none exists. Resolved on demand so a hub
        /// that was created before a script reload is still found; imperative callers (tools,
        /// automated playthroughs, console commands) can reach it instead of searching the scene.
        /// </summary>
        public static DebugHud Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindAnyObjectByType<DebugHud>();
                }

                return s_Instance;
            }
        }

        // ---------------------------------------------------------------- visibility and layout

        /// <summary>
        /// Whether the hub is drawn. Starts hidden so it never covers the game unprompted;
        /// F1 (or the header button) toggles it.
        /// </summary>
        public bool IsHudVisible => m_IsHudVisible;

        /// <summary>Shows or hides the hub explicitly.</summary>
        public void SetHudVisible(bool visible)
        {
            m_IsHudVisible = visible;
        }

        /// <summary>Shows the hub if hidden, hides it if shown. Bound to F1.</summary>
        public void ToggleHud()
        {
            SetHudVisible(!m_IsHudVisible);
        }

        /// <summary>Section of the hub currently presented.</summary>
        public DebugTab ActiveTab => m_ActiveTab;

        /// <summary>Presents the given section. Bound to the tab buttons.</summary>
        public void SetTab(DebugTab tab)
        {
            m_ActiveTab = tab;
        }

        /// <summary>Message the hub shows in its status lines.</summary>
        public string StatusMessage
        {
            get => m_StatusMessage;
            set => m_StatusMessage = value;
        }

        // ---------------------------------------------------------------- session and gameplay

        /// <summary>The session the hub drives, or null when none is bound yet.</summary>
        public GameSession Session => m_Session;

        /// <summary>
        /// Creates and starts a standalone session for scenes played outside Boot.
        /// Does nothing when a session is already bound, so it cannot detach the hub from
        /// the session owned by <see cref="AppRoot"/>. Bound to the "Start Standalone Game Session" button.
        /// </summary>
        public void StartStandaloneSession()
        {
            if (m_Session != null)
            {
                return;
            }

            var session = new GameSession();
            Initialize(session);
            session.StartNewGame();
            m_StatusMessage = "Started standalone session.";
        }

        /// <summary>Starts a fresh Classic game. Bound to the "Classic" button.</summary>
        public void StartClassicGame()
        {
            StartGame(new ClassicMode(), "Started new Classic game.");
        }

        /// <summary>
        /// Starts a fresh Daily Challenge. Bound to the "Daily" button.
        /// <paramref name="dateUtc"/> defaults to today in UTC (yyyy-MM-dd).
        /// </summary>
        public void StartDailyChallenge(string dateUtc = null)
        {
            string date = string.IsNullOrEmpty(dateUtc) ? DateTime.UtcNow.ToString("yyyy-MM-dd") : dateUtc;
            StartGame(new DailyChallengeMode(date), $"Started Daily Challenge ({date}).");
        }

        /// <summary>Starts a fresh Zen game. Bound to the "Zen" button.</summary>
        public void StartZenGame()
        {
            StartGame(new ZenMode(), "Started Zen Mode.");
        }

        /// <summary>
        /// Undoes the last move when the bound session allows it.
        /// Returns false when there is no session, nothing to undo, or the mode forbids undos.
        /// Bound to the "Undo Move" button.
        /// </summary>
        public bool UndoMove()
        {
            if (!CanUndo(m_Session) || !m_Session.TryUndo())
            {
                return false;
            }

            m_StatusMessage = "Move undone.";
            return true;
        }

        /// <summary>
        /// Highlights a helpful move on the board and reports it in the status line.
        /// Returns false when there is no session or no move is available.
        /// Bound to the "Hint" button.
        /// </summary>
        public bool RequestHint()
        {
            if (m_Session == null)
            {
                return false;
            }

            if (!m_Session.RequestHint(out GridPos from, out GridPos to))
            {
                m_StatusMessage = "No helpful move found.";
                return false;
            }

            m_HintFrom = from;
            m_HintTo = to;
            m_StatusMessage = $"Hint: Move {m_Session.Board.ColorAt(from)} from {from} to {to}";
            return true;
        }

        /// <summary>Drops a highlighted hint without touching the session.</summary>
        public void ClearHint()
        {
            m_HintFrom = null;
            m_HintTo = null;
        }

        /// <summary>
        /// Revives a finished game by clearing balls. Bound to the "Revive (+3 Free)" button,
        /// which is only offered while the session is over.
        /// </summary>
        public void Revive(int balls = 3)
        {
            if (m_Session == null || m_Session.Phase != GamePhase.GameOver)
            {
                return;
            }

            m_Session.Revive(balls);
            m_StatusMessage = $"Revived! Cleared {balls} balls.";
        }

        // ---------------------------------------------------------------- popup debug hub

        /// <summary>Whether the given popup is currently open in the scene's shell.</summary>
        public bool IsPopupOpen(Presentation.UiPopupId id)
        {
            Presentation.UiShell shell = ResolveShell();
            return shell != null &&
                   shell.PopupRegistry != null &&
                   shell.PopupRegistry.TryGetRegistered(id, out Presentation.PopupView popup) &&
                   popup != null &&
                   popup.IsOpen;
        }

        /// <summary>
        /// Opens the popup when it is closed, closes it when it sits on top of the modal stack.
        /// Bound to every popup toggle button.
        /// </summary>
        public bool ToggleMockPopup(Presentation.UiPopupId id)
        {
            if (IsPopupOpen(id))
            {
                CloseMockPopup(id);
            }
            else
            {
                OpenMockPopup(id);
            }

            return IsPopupOpen(id);
        }

        /// <summary>
        /// Opens a popup filled with mock data.
        /// Returns false when the scene has no shell or the popup cannot be shown.
        /// </summary>
        public bool OpenMockPopup(Presentation.UiPopupId id)
        {
            Presentation.UiShell shell = ResolveShell();
            if (shell == null)
            {
                m_StatusMessage = "No UiShell in this scene - popups live in the Game scene.";
                return false;
            }

            return OpenMockPopupOn(shell, id);
        }

        /// <summary>Closes the popup when it is the top of the modal stack.</summary>
        public bool CloseMockPopup(Presentation.UiPopupId id)
        {
            Presentation.UiShell shell = ResolveShell();
            if (shell == null)
            {
                m_StatusMessage = "No UiShell in this scene - popups live in the Game scene.";
                return false;
            }

            if (!IsPopupOpen(id))
            {
                return false;
            }

            bool closed = shell.TryClosePopup(id);
            m_StatusMessage = closed
                ? $"Closed {id} popup."
                : $"{id} is open but not on top of the modal stack - use CloseAllPopups().";
            return closed;
        }

        /// <summary>Closes every popup in the scene's shell. Bound to the "Close All Popups" button.</summary>
        public void CloseAllPopups()
        {
            Presentation.UiShell shell = ResolveShell();
            if (shell == null)
            {
                m_StatusMessage = "No UiShell in this scene - popups live in the Game scene.";
                return;
            }

            shell.CloseAllPopups();
            m_StatusMessage = "All popups closed.";
        }

        // ---------------------------------------------------------------- mock fixtures

        /// <summary>Mock summaries used by the popup debug hub and by EditMode coverage.</summary>
        public static IReadOnlyList<SessionSummary> MockSummaries => s_MockSummaries;

        /// <summary>Index of the mock summary the hub currently presents.</summary>
        public int MockSummaryIndex => m_MockSummaryIndex;

        /// <summary>
        /// Presents the mock fixture at <paramref name="index"/>, wrapping out-of-range values,
        /// and re-populates an open game-over popup so the change shows without a reopen.
        /// </summary>
        public void SelectMockFixture(int index)
        {
            int count = s_MockSummaries.Length;
            m_MockSummaryIndex = ((index % count) + count) % count;

            Presentation.UiShell shell = ResolveShell();
            if (shell != null &&
                shell.PopupRegistry != null &&
                shell.PopupRegistry.TryGetRegistered(Presentation.UiPopupId.GameOver, out Presentation.PopupView popup) &&
                popup is Presentation.GameOverPopup gameOverPopup &&
                gameOverPopup.IsOpen)
            {
                // Re-populate in place so fixture differences show without a close/open cycle.
                gameOverPopup.Populate(s_MockSummaries[m_MockSummaryIndex]);
            }

            m_StatusMessage = $"Mock fixture {m_MockSummaryIndex + 1}/{count}: " +
                              DescribeSummary(s_MockSummaries[m_MockSummaryIndex]);
        }

        /// <summary>Advances to the next mock fixture. Bound to the "Cycle Next" button.</summary>
        public void CycleMockFixture()
        {
            SelectMockFixture(m_MockSummaryIndex + 1);
        }

        /// <summary>Builds the mock payload the popup hub feeds to a popup.</summary>
        public Presentation.IUiPopupPayload CreateMockPayload(Presentation.UiPopupId id)
        {
            return BuildMockPayload(id);
        }

        /// <summary>One-line description of a session summary, as shown in the hub's status line.</summary>
        public static string FormatSummary(in SessionSummary summary)
        {
            return DescribeSummary(summary);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDebugHud()
        {
            if (FindAnyObjectByType<DebugHud>(FindObjectsInactive.Include) == null && AppRoot.Instance == null)
            {
                var go = new GameObject("[DebugHud_Transient]");
                go.AddComponent<DebugHud>();
                DontDestroyOnLoad(go);
            }
        }

        public void Initialize(GameSession session)
        {
            if (m_Session != null)
            {
                m_Session.OnMoveCommitted -= OnMoveCommitted;
                m_Session.OnGameOver -= OnGameOver;
            }

            m_Session = session;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (m_Session != null)
            {
                m_Session.OnMoveCommitted += OnMoveCommitted;
                m_Session.OnGameOver += OnGameOver;
            }
        }

        private void OnEnable()
        {
            s_Instance = this;
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (m_Session != null)
            {
                m_Session.OnMoveCommitted -= OnMoveCommitted;
                m_Session.OnGameOver -= OnGameOver;
            }
        }

        private void OnMoveCommitted(MoveResult result)
        {
            m_HintFrom = null;
            m_HintTo = null;

            if (result.Outcome == MoveOutcome.Cleared)
            {
                m_StatusMessage = $"Cleared {result.Cleared.Count} balls (+{result.ScoreDelta} pts)!";
            }
            else if (result.Outcome == MoveOutcome.Moved)
            {
                m_StatusMessage = $"Moved. Spawned {result.Spawned.Count} balls.";
            }
            else if (result.Outcome == MoveOutcome.GameOver)
            {
                m_StatusMessage = "GAME OVER! No legal moves or board full.";
            }
        }

        private void OnGameOver(SessionSummary summary)
        {
            m_StatusMessage = $"GAME OVER! Final Score: {summary.FinalScore:N0}, Longest: {summary.LongestLine}";
        }

        private void OnGUI()
        {
            HandleHotkeys();

            if (!m_IsHudVisible)
            {
                // Hidden: draw nothing at all, so the hub is fully invisible until F1 brings it back.
                return;
            }

            int prevLabelSize = GUI.skin.label.fontSize;
            int prevButtonSize = GUI.skin.button.fontSize;
            bool prevWordWrap = GUI.skin.label.wordWrap;
            bool prevRichText = GUI.skin.label.richText;
            Color prevBg = GUI.backgroundColor;

            try
            {
                GUI.skin.label.fontSize = 13;
                GUI.skin.label.richText = true;
                GUI.skin.button.fontSize = 12;

                float maxWidth = 420f;
                float width = Mathf.Min(maxWidth, Screen.width - 20f);
                float maxHeight = Mathf.Max(240f, Screen.height - 20f);
                float targetHeight = m_ActiveTab == DebugTab.PopupHub ? Mathf.Min(430f, maxHeight) : maxHeight;
                Rect hudRect = new Rect(10, 10, width, targetHeight);

                GUILayout.BeginArea(hudRect, GUI.skin.box);

                // Window Header
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>LINE 98 — Debug Hub</b>", GUI.skin.label);
                if (GUILayout.Button(new GUIContent("—", "Hide the Debug Hub (F1)"), GUILayout.Width(26), GUILayout.Height(20)))
                {
                    m_IsHudVisible = false;
                }
                GUILayout.EndHorizontal();

                // Tab Switcher
                GUILayout.BeginHorizontal();
                DrawTabButton("All", DebugTab.All);
                DrawTabButton("Popup Hub", DebugTab.PopupHub);
                DrawTabButton("Gameplay", DebugTab.Gameplay);
                GUILayout.EndHorizontal();

                GUILayout.Space(4);

                m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, false, false);

                if (m_ActiveTab == DebugTab.All || m_ActiveTab == DebugTab.Gameplay)
                {
                    DrawGameplaySection();
                }

                if (m_ActiveTab == DebugTab.All)
                {
                    GUILayout.Space(8);
                }

                if (m_ActiveTab == DebugTab.All || m_ActiveTab == DebugTab.PopupHub)
                {
                    DrawPopupDebugSection();
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            finally
            {
                GUI.skin.label.fontSize = prevLabelSize;
                GUI.skin.button.fontSize = prevButtonSize;
                GUI.skin.label.wordWrap = prevWordWrap;
                GUI.skin.label.richText = prevRichText;
                GUI.backgroundColor = prevBg;
            }
        }

        private void DrawTabButton(string label, DebugTab tab)
        {
            Color prev = GUI.backgroundColor;
            if (m_ActiveTab == tab)
            {
                GUI.backgroundColor = new Color(0.35f, 0.7f, 1f);
            }
            if (GUILayout.Button(label, GUILayout.Height(22)))
            {
                m_ActiveTab = tab;
            }
            GUI.backgroundColor = prev;
        }

        private void DrawGameplaySection()
        {
            if (m_Session == null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("<color=#FFA726><b>Gameplay Session Inactive</b></color>\n(Scene tested in isolation outside Boot)");
                if (GUILayout.Button("Start Standalone Game Session"))
                {
                    StartStandaloneSession();
                }
                GUILayout.EndVertical();
                return;
            }

            // Session Stats
            GUILayout.Label($"Mode: {m_Session.Mode?.DisplayName ?? "None"} | Phase: {m_Session.Phase}");
            GUILayout.Label($"Score: <b>{m_Session.Score}</b> | Moves: {m_Session.MoveCount} | Cleared: {m_Session.LinesCleared}");
            GUILayout.Label($"Free Undos: {m_Session.FreeUndosRemaining} | Empty Cells: {m_Session.Board?.EmptyCount ?? 0}/81");

            // Preview Queue Display
            if (m_Session.PreviewQueue != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Next 3: ", GUILayout.Width(50));
                for (int i = 0; i < m_Session.PreviewQueue.Capacity; i++)
                {
                    BallColor color = m_Session.PreviewQueue[i];
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetBallUiColor(color);
                    GUILayout.Button(color.ToString(), GUILayout.Width(64), GUILayout.Height(22));
                    GUI.backgroundColor = oldColor;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
            GUILayout.Label($"Status: {m_StatusMessage}");
            GUILayout.Space(4);

            // 9x9 Board Grid
            float cellSize = 36f;
            for (int y = 0; y < BoardModel.Size; y++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    BallColor color = m_Session.Board.ColorAt(pos);

                    Color prevBg = GUI.backgroundColor;

                    bool isSelected = m_Session.HasSelection && m_Session.SelectedPos == pos;
                    bool isHint = (m_HintFrom.HasValue && m_HintFrom.Value == pos) ||
                                  (m_HintTo.HasValue && m_HintTo.Value == pos);

                    if (isSelected)
                    {
                        GUI.backgroundColor = Color.cyan;
                    }
                    else if (isHint)
                    {
                        GUI.backgroundColor = Color.yellow;
                    }
                    else if (color != BallColor.None)
                    {
                        GUI.backgroundColor = GetBallUiColor(color);
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                    }

                    string cellLabel = color != BallColor.None ? GetColorShortCode(color) : "";
                    if (GUILayout.Button(cellLabel, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        OnCellClicked(pos);
                    }

                    GUI.backgroundColor = prevBg;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            // Control Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Classic"))
            {
                StartClassicGame();
            }

            if (GUILayout.Button("Daily"))
            {
                StartDailyChallenge();
            }

            if (GUILayout.Button("Zen"))
            {
                StartZenGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = CanUndo(m_Session);
            if (GUILayout.Button("Undo Move"))
            {
                UndoMove();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Hint"))
            {
                RequestHint();
            }

            if (m_Session.Phase == GamePhase.GameOver)
            {
                Color prevCol = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Revive (+3 Free)"))
                {
                    Revive(3);
                }
                GUI.backgroundColor = prevCol;
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Applies a mode and starts a fresh game in it, creating a standalone session first
        /// when the scene was played without Boot/AppRoot.
        /// </summary>
        private void StartGame(IGameModeStrategy mode, string statusMessage)
        {
            if (m_Session == null)
            {
                StartStandaloneSession();
            }

            m_Session.SetMode(mode);
            m_Session.StartNewGame();
            m_StatusMessage = statusMessage;
        }

        /// <summary>Mirrors the enable rule of the "Undo Move" button.</summary>
        private static bool CanUndo(GameSession session)
        {
            return session != null &&
                   session.FreeUndosRemaining > 0 &&
                   session.Mode != null &&
                   session.Mode.UndoAllowed &&
                   session.Phase == GamePhase.Playing;
        }

        private Presentation.UiShell ResolveShell()
        {
            if (m_Shell == null)
            {
                m_Shell = FindAnyObjectByType<Presentation.UiShell>(FindObjectsInactive.Include);
            }

            return m_Shell;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // UiShell is scene-scoped, so a cached reference must not survive a scene load.
            m_Shell = null;
        }

        private void HandleHotkeys()
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.F1)
            {
                ToggleHud();
                current.Use();
            }
        }

        private void DrawPopupDebugSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b>Popup Debug Hub (Mock Data)</b>", GUI.skin.label);

            Presentation.UiShell shell = ResolveShell();
            if (shell == null)
            {
                GUILayout.Label("<color=#FF7043>No UiShell detected in scene.</color>");
                GUILayout.EndVertical();
                return;
            }

            int registered = shell.PopupRegistry != null ? shell.PopupRegistry.RegisteredCount : 0;
            string topPopupName = shell.IsAnyPopupOpen ? (shell.TopPopup != null ? shell.TopPopup.GetType().Name : "Open") : "None";
            GUILayout.Label($"<b>Modal State:</b> Open: <b>{shell.IsAnyPopupOpen}</b> | Reg: <b>{registered}</b> | Top: <b>{topPopupName}</b>");

            SessionSummary summary = s_MockSummaries[m_MockSummaryIndex];
            bool isNewBest = summary.FinalScore > 0 && summary.FinalScore >= summary.BestScore;

            // Structured Fixture Card with clean word wrap and N0 formatting
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Fixture {m_MockSummaryIndex + 1}/{s_MockSummaries.Length}</b> " +
                            (isNewBest ? "<color=#FFD54F>★ [NEW BEST]</color>" : ""),
                            new GUIStyle(GUI.skin.label) { richText = true });
            if (GUILayout.Button("Cycle Next ↻", GUILayout.Width(100), GUILayout.Height(22)))
            {
                CycleMockFixture();
            }
            GUILayout.EndHorizontal();

            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12, wordWrap = true };
            GUILayout.Label($"Score: <b>{summary.FinalScore:N0}</b>   |   Best: <b>{summary.BestScore:N0}</b>", statStyle);
            GUILayout.Label($"Lines: <b>{summary.LinesCleared}</b>   |   Longest: <b>{summary.LongestLine}</b>   |   Moves: <b>{summary.TotalMoves}</b>", statStyle);
            GUILayout.Label($"Can Continue: <b>{(summary.CanContinue ? "<color=#81C784>Yes</color>" : "<color=#E57373>No</color>")}</b>", statStyle);
            GUILayout.EndVertical();

            // Game-Over Main Toggle Button
            bool isGameOverOpen = IsPopupOpen(Presentation.UiPopupId.GameOver);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isGameOverOpen
                ? new Color(0.95f, 0.4f, 0.35f)
                : new Color(0.35f, 0.8f, 0.45f);
            if (GUILayout.Button(isGameOverOpen ? "Hide Game-Over Popup" : "Show Game-Over Popup", GUILayout.Height(30)))
            {
                ToggleMockPopup(Presentation.UiPopupId.GameOver);
            }
            GUI.backgroundColor = prevBg;

            // Secondary Popups
            GUILayout.BeginHorizontal();
            DrawPopupToggleButton(Presentation.UiPopupId.Confirm, "Confirm");
            DrawPopupToggleButton(Presentation.UiPopupId.Settings, "Settings");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawPopupToggleButton(Presentation.UiPopupId.Statistics, "Stats");
            DrawPopupToggleButton(Presentation.UiPopupId.Cosmetics, "Themes");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Close All Popups", GUILayout.Height(24)))
            {
                CloseAllPopups();
            }

            // Inline Feedback inside the Popup Debug Hub
            if (!string.IsNullOrEmpty(m_StatusMessage))
            {
                GUIStyle feedbackStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 11,
                    wordWrap = true,
                    fontStyle = FontStyle.Italic
                };
                GUILayout.Label($"<color=#90A4AE>Status: {m_StatusMessage}</color>", feedbackStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawPopupToggleButton(Presentation.UiPopupId id, string label)
        {
            bool isOpen = IsPopupOpen(id);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isOpen
                ? new Color(0.95f, 0.4f, 0.35f)
                : new Color(0.45f, 0.65f, 0.85f);

            string btnText = isOpen ? $"Hide {label} (Open)" : $"Show {label}";
            if (GUILayout.Button(btnText, GUILayout.Height(26)))
            {
                ToggleMockPopup(id);
            }
            GUI.backgroundColor = prevBg;
        }

        private bool OpenMockPopupOn(Presentation.UiShell shell, Presentation.UiPopupId id)
        {
            // Settings and Themes read shell-owned state (catalog, selector, active theme id),
            // so they go through the shell wrappers that build a complete payload.
            switch (id)
            {
                case Presentation.UiPopupId.Settings:
                    shell.OpenSettings();
                    m_StatusMessage = "Opened Settings popup.";
                    return true;
                case Presentation.UiPopupId.Cosmetics:
                    shell.OpenCosmetics();
                    m_StatusMessage = "Opened Themes popup.";
                    return true;
            }

            bool opened = shell.PopupRegistry != null &&
                          shell.PopupRegistry.Open(id, BuildMockPayload(id));

            m_StatusMessage = opened
                ? $"Opened {id} popup with mock data."
                : $"Cannot open {id}: not placed in the scene and not in the popup catalog.";
            return opened;
        }

        private Presentation.IUiPopupPayload BuildMockPayload(Presentation.UiPopupId id)
        {
            switch (id)
            {
                case Presentation.UiPopupId.GameOver:
                    return new Presentation.GameOverPopupPayload(s_MockSummaries[m_MockSummaryIndex]);
                case Presentation.UiPopupId.Confirm:
                    return new Presentation.ConfirmPopupPayload(
                        "Debug Confirm",
                        "Mock confirmation raised by the debug HUD.",
                        OnMockConfirmAccepted);
                case Presentation.UiPopupId.Statistics:
                    return new Presentation.StatisticsPopupPayload(7, 3480, 42, 512);
                default:
                    return null;
            }
        }

        private void OnMockConfirmAccepted()
        {
            m_StatusMessage = "Mock confirm accepted.";
        }

        private static string DescribeSummary(in SessionSummary summary)
        {
            string bestTag = summary.FinalScore > 0 && summary.FinalScore >= summary.BestScore ? " [NEW BEST]" : "";
            return $"score {summary.FinalScore:N0}, best {summary.BestScore:N0}{bestTag}, " +
                   $"lines {summary.LinesCleared}, longest {summary.LongestLine}, moves {summary.TotalMoves}, continue {summary.CanContinue}";
        }

        private void OnCellClicked(GridPos pos)
        {
            if (m_Session == null || m_Session.Phase != GamePhase.Playing)
            {
                return;
            }

            if (m_Session.Board != null && !m_Session.Board.IsEmpty(pos))
            {
                // Select or switch selection
                m_Session.TrySelect(pos);
            }
            else
            {
                // If a ball is selected, move it to this empty cell
                if (m_Session.HasSelection)
                {
                    if (!m_Session.ExecuteMove(pos))
                    {
                        m_StatusMessage = $"Cannot move to {pos}: Path blocked or unreachable!";
                    }
                }
            }
        }

        private static Color GetBallUiColor(BallColor color)
        {
            switch (color)
            {
                case BallColor.Red: return new Color(0.95f, 0.2f, 0.2f);
                case BallColor.Orange: return new Color(1f, 0.55f, 0.1f);
                case BallColor.Yellow: return new Color(0.98f, 0.88f, 0.2f);
                case BallColor.Green: return new Color(0.2f, 0.8f, 0.25f);
                case BallColor.Cyan: return new Color(0.15f, 0.8f, 0.95f);
                case BallColor.Purple: return new Color(0.65f, 0.25f, 0.9f);
                case BallColor.Blue: return new Color(0.08f, 0.4f, 0.75f);
                default: return Color.gray;
            }
        }

        private static string GetColorShortCode(BallColor color)
        {
            switch (color)
            {
                case BallColor.Red: return "R";
                case BallColor.Orange: return "O";
                case BallColor.Yellow: return "Y";
                case BallColor.Green: return "G";
                case BallColor.Cyan: return "C";
                case BallColor.Purple: return "P";
                case BallColor.Blue: return "B";
                default: return "";
            }
        }
    }
}
#endif
