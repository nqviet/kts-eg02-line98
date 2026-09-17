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

        [SerializeField] private Data.ThemeCatalogSO m_ThemeCatalog;
        private GameSession m_Session;
        private GridPos? m_HintFrom;
        private GridPos? m_HintTo;
        private string m_StatusMessage = "Ready";
        private Presentation.UiShell m_Shell;
        private int m_MockSummaryIndex;
        private bool m_IsHudVisible;
        private Vector2 m_ScrollPosition;
        private DebugTab m_ActiveTab = DebugTab.All;
        private bool m_IsThemeDropdownOpen;
        private string m_CurrentSelectedThemeId;

        private static Texture2D s_SolidBgTexture;
        private static Texture2D s_SolidBorderTexture;

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

        // ---------------------------------------------------------------- theme selection

        /// <summary>Allows configuring or overriding the ThemeCatalog used by the HUD.</summary>
        public Data.ThemeCatalogSO ThemeCatalog
        {
            get => ResolveCatalog();
            set => m_ThemeCatalog = value;
        }

        /// <summary>Theme id currently selected or active.</summary>
        public string ActiveThemeId => GetActiveThemeId();

        /// <summary>Whether the theme selection dropdown in the HUD is currently expanded.</summary>
        public bool IsThemeDropdownOpen
        {
            get => m_IsThemeDropdownOpen;
            set => m_IsThemeDropdownOpen = value;
        }

        /// <summary>Selects a theme pack across all active services and presentation components.</summary>
        public void SelectTheme(string themeId)
        {
            if (string.IsNullOrEmpty(themeId))
            {
                return;
            }

            m_CurrentSelectedThemeId = themeId;
            bool applied = false;

            // 1. ICosmeticService via ServiceRegistry
            if (ServiceRegistry.Instance.TryResolve<Services.ICosmeticService>(out var cosmeticService) && cosmeticService != null)
            {
                cosmeticService.SetBoardTheme(themeId);
                cosmeticService.SetBallTheme(themeId);
                cosmeticService.SetClearEffectTheme(themeId);
                applied = true;
            }

            // 2. IThemeSelector via ServiceRegistry
            if (ServiceRegistry.Instance.TryResolve<Presentation.IThemeSelector>(out var selector) && selector != null)
            {
                selector.RequestTheme(Data.ThemeCategory.Board, themeId);
                selector.RequestTheme(Data.ThemeCategory.Ball, themeId);
                selector.RequestTheme(Data.ThemeCategory.ClearEffect, themeId);
                applied = true;
            }

            // 3. PresentationRoot in scene
            var presRoot = FindAnyObjectByType<Presentation.PresentationRoot>();
            if (presRoot != null && presRoot.ThemeSelector != null && presRoot.ThemeSelector != selector)
            {
                presRoot.ThemeSelector.RequestTheme(Data.ThemeCategory.Board, themeId);
                presRoot.ThemeSelector.RequestTheme(Data.ThemeCategory.Ball, themeId);
                presRoot.ThemeSelector.RequestTheme(Data.ThemeCategory.ClearEffect, themeId);
                applied = true;
            }

            // 4. UiShell in scene
            var shell = ResolveShell();
            if (shell != null && shell.ThemeSelector != null && shell.ThemeSelector != selector)
            {
                shell.ThemeSelector.RequestTheme(Data.ThemeCategory.Board, themeId);
                shell.ThemeSelector.RequestTheme(Data.ThemeCategory.Ball, themeId);
                shell.ThemeSelector.RequestTheme(Data.ThemeCategory.ClearEffect, themeId);
                applied = true;
            }

            // 5. Direct catalog fallback application if needed
            var catalog = ResolveCatalog();
            if (catalog != null && catalog.TryGetTheme(themeId, out var themeDef) && themeDef != null)
            {
                if (presRoot != null && !applied)
                {
                    if (themeDef.BoardTheme != null && themeDef.UiTheme != null)
                    {
                        presRoot.ApplyBoardAndUi(themeDef.BoardTheme, themeDef.UiTheme);
                    }
                    if (themeDef.BallTheme != null)
                    {
                        presRoot.ApplyBallTheme(themeDef.BallTheme);
                    }
                    if (themeDef.ClearEffect != null)
                    {
                        presRoot.ApplyClearEffect(themeDef.ClearEffect);
                    }
                    applied = true;
                }

                if (shell != null)
                {
                    if (themeDef.UiTheme != null) shell.ApplyTheme(themeDef.UiTheme);
                    if (themeDef.BallTheme != null) shell.ApplyBallTheme(themeDef.BallTheme);
                }
            }

            m_StatusMessage = applied
                ? $"Switched theme to '{themeId}'."
                : $"Selected theme '{themeId}'.";
        }

        /// <summary>Resolves the currently active theme identifier.</summary>
        public string GetActiveThemeId()
        {
            if (ServiceRegistry.Instance.TryResolve<Services.ICosmeticService>(out var cosmeticService) && cosmeticService != null)
            {
                return cosmeticService.ActiveBoardThemeId;
            }

            if (ServiceRegistry.Instance.TryResolve<Presentation.IThemeSelector>(out var selector) && selector != null)
            {
                return selector.ActiveBoardThemeId;
            }

            var presRoot = FindAnyObjectByType<Presentation.PresentationRoot>();
            if (presRoot != null && presRoot.ThemeSelector != null)
            {
                return presRoot.ThemeSelector.ActiveBoardThemeId;
            }

            var shell = ResolveShell();
            if (shell != null && shell.ThemeSelector != null)
            {
                return shell.ThemeSelector.ActiveBoardThemeId;
            }

            if (!string.IsNullOrEmpty(m_CurrentSelectedThemeId))
            {
                return m_CurrentSelectedThemeId;
            }

            var catalog = ResolveCatalog();
            return catalog != null ? catalog.DefaultThemeId : Data.ThemeIds.Crystal;
        }

        /// <summary>Resolves the display name of the currently active theme.</summary>
        public string GetActiveThemeDisplayName()
        {
            string currentId = GetActiveThemeId();
            var catalog = ResolveCatalog();
            if (catalog != null && catalog.TryGetTheme(currentId, out var themeDef) && themeDef != null)
            {
                return themeDef.DisplayName;
            }

            if (string.Equals(currentId, Data.ThemeIds.Crystal, StringComparison.OrdinalIgnoreCase))
            {
                return "Crystal";
            }

            if (string.Equals(currentId, Data.ThemeIds.Classic, StringComparison.OrdinalIgnoreCase))
            {
                return "Classic";
            }

            return currentId;
        }

        private Data.ThemeCatalogSO ResolveCatalog()
        {
            if (m_ThemeCatalog != null)
            {
                return m_ThemeCatalog;
            }

            Presentation.UiShell shell = ResolveShell();
            if (shell != null && shell.ThemeCatalog != null)
            {
                return shell.ThemeCatalog;
            }

            if (AppRoot.Instance != null && AppRoot.Instance.ThemeCatalog != null)
            {
                return AppRoot.Instance.ThemeCatalog;
            }

#if UNITY_EDITOR
            m_ThemeCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Data.ThemeCatalogSO>("Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset");
            if (m_ThemeCatalog != null)
            {
                return m_ThemeCatalog;
            }
#endif

            return null;
        }

        private List<(string id, string name)> GetAvailableThemes()
        {
            var list = new List<(string id, string name)>();
            var catalog = ResolveCatalog();
            if (catalog != null && catalog.Count > 0)
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    var theme = catalog.ThemeAt(i);
                    if (theme != null)
                    {
                        list.Add((theme.ThemeId, theme.DisplayName));
                    }
                }
            }

            if (list.Count == 0)
            {
                list.Add((Data.ThemeIds.Crystal, "Crystal"));
                list.Add((Data.ThemeIds.Classic, "Classic"));
            }

            return list;
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

            EnsureSolidTextures();

            int prevLabelSize = GUI.skin.label.fontSize;
            int prevButtonSize = GUI.skin.button.fontSize;
            bool prevWordWrap = GUI.skin.label.wordWrap;
            bool prevRichText = GUI.skin.label.richText;
            Color prevBg = GUI.backgroundColor;
            Color prevColor = GUI.color;

            try
            {
                GUI.skin.label.fontSize = 13;
                GUI.skin.label.richText = true;
                GUI.skin.button.fontSize = 12;

                var themes = GetAvailableThemes();
                int themeCount = themes.Count;
                float dropdownHeight = m_IsThemeDropdownOpen ? (themeCount * 26f + 8f) : 0f;
                float width = 280f;
                float baseHeight = 150f;
                float height = baseHeight + dropdownHeight;

                Rect hudRect = new Rect(10, 10, width, height);

                // 1. Draw solid, 100% opaque background and border - NO TRANSPARENCY
                GUI.color = Color.white;
                GUI.DrawTexture(hudRect, s_SolidBorderTexture);
                Rect innerRect = new Rect(hudRect.x + 1, hudRect.y + 1, hudRect.width - 2, hudRect.height - 2);
                GUI.DrawTexture(innerRect, s_SolidBgTexture);

                // 2. Draw content inside area
                Rect contentRect = new Rect(hudRect.x + 10, hudRect.y + 8, hudRect.width - 20, hudRect.height - 16);
                GUILayout.BeginArea(contentRect, GUIStyle.none);

                // Window Header
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>LINE 98 — Debug HUD</b>", GUI.skin.label);
                if (GUILayout.Button(new GUIContent("—", "Hide Debug HUD (F1)"), GUILayout.Width(24), GUILayout.Height(20)))
                {
                    m_IsHudVisible = false;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Game-Over Popup
                GUILayout.Label("<b>Game Over Popup</b>");
                GUILayout.BeginHorizontal();
                bool isGameOverOpen = IsPopupOpen(Presentation.UiPopupId.GameOver);
                GUI.backgroundColor = isGameOverOpen
                    ? new Color(0.95f, 0.4f, 0.35f, 1f)
                    : new Color(0.35f, 0.8f, 0.45f, 1f);
                string toggleText = isGameOverOpen ? "Hide Popup" : "Show Popup";
                if (GUILayout.Button(toggleText, GUILayout.Height(28)))
                {
                    ToggleMockPopup(Presentation.UiPopupId.GameOver);
                }
                GUI.backgroundColor = prevBg;

                if (GUILayout.Button($"Fixture {m_MockSummaryIndex + 1}/{s_MockSummaries.Length} ↻", GUILayout.Width(92), GUILayout.Height(28)))
                {
                    CycleMockFixture();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);

                // Theme Select Box
                GUILayout.Label("<b>Theme</b>");
                string currentId = GetActiveThemeId();
                string currentName = GetActiveThemeDisplayName();

                string arrow = m_IsThemeDropdownOpen ? "▲" : "▼";
                if (GUILayout.Button($" {currentName}  {arrow}", GUILayout.Height(28)))
                {
                    m_IsThemeDropdownOpen = !m_IsThemeDropdownOpen;
                }

                if (m_IsThemeDropdownOpen)
                {
                    GUILayout.Space(2);
                    for (int i = 0; i < themes.Count; i++)
                    {
                        var (id, name) = themes[i];
                        bool isSelected = string.Equals(id, currentId, StringComparison.OrdinalIgnoreCase);
                        GUI.backgroundColor = isSelected
                            ? new Color(0.35f, 0.75f, 1f, 1f)
                            : new Color(0.22f, 0.25f, 0.30f, 1f);

                        string marker = isSelected ? "● " : "   ";
                        if (GUILayout.Button($"{marker}{name}", GUILayout.Height(24)))
                        {
                            SelectTheme(id);
                            m_IsThemeDropdownOpen = false;
                        }
                    }
                    GUI.backgroundColor = prevBg;
                }

                GUILayout.EndArea();
            }
            finally
            {
                GUI.skin.label.fontSize = prevLabelSize;
                GUI.skin.button.fontSize = prevButtonSize;
                GUI.skin.label.wordWrap = prevWordWrap;
                GUI.skin.label.richText = prevRichText;
                GUI.backgroundColor = prevBg;
                GUI.color = prevColor;
            }
        }

        private static void EnsureSolidTextures()
        {
            if (s_SolidBgTexture == null)
            {
                s_SolidBgTexture = new Texture2D(1, 1);
                s_SolidBgTexture.hideFlags = HideFlags.DontSave;
                s_SolidBgTexture.SetPixel(0, 0, new Color(0.12f, 0.13f, 0.16f, 1.0f));
                s_SolidBgTexture.Apply();
            }

            if (s_SolidBorderTexture == null)
            {
                s_SolidBorderTexture = new Texture2D(1, 1);
                s_SolidBorderTexture.hideFlags = HideFlags.DontSave;
                s_SolidBorderTexture.SetPixel(0, 0, new Color(0.30f, 0.34f, 0.40f, 1.0f));
                s_SolidBorderTexture.Apply();
            }
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
    }
}
#endif
