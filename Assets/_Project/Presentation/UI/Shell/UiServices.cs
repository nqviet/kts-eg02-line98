using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Animation;
using Line98.Presentation.Audio;

namespace Line98.Presentation
{
    /// <summary>
    /// Immutable dependency context supplied once to UI behaviours at presentation startup.
    /// Prefabs use this context rather than static UI state.
    /// </summary>
    public sealed class UiServices
    {
        public UiServices(
            TweenRunner tweenRunner,
            AudioService audioService,
            UiThemeSO theme,
            UiShell router,
            GameSession session)
        {
            TweenRunner = tweenRunner;
            AudioService = audioService;
            Theme = theme;
            Router = router;
            Session = session;
        }

        public TweenRunner TweenRunner { get; }
        public AudioService AudioService { get; }
        public UiThemeSO Theme { get; }
        public UiShell Router { get; }
        public GameSession Session { get; }
    }
}
