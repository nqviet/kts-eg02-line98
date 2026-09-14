using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Scene-owned main menu screen root. Navigation is composed from child UiNavigationButton widgets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuScreen : UiScreenBase
    {
        public override string ScreenId => string.IsNullOrWhiteSpace(base.ScreenId) ? "MainMenu" : base.ScreenId;
    }
}
