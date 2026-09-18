using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Single source of truth for how a disabled control looks. Every inert widget dims to the same
    /// alpha so "grayed out" reads identically across the HUD, settings, and popups.
    /// </summary>
    public static class UiDimState
    {
        /// <summary>Alpha applied to a control that is present but not usable.</summary>
        public const float DisabledAlpha = 0.55f;

        public static void Apply(CanvasGroup group, bool interactable)
        {
            if (group == null) return;
            group.alpha = interactable ? 1f : DisabledAlpha;
        }

        /// <summary>
        /// Dims a subtree that has no authored CanvasGroup, adding one on first use. Safe to call
        /// every frame -- the component is only created once.
        /// </summary>
        public static void Apply(GameObject target, bool interactable)
        {
            if (target == null) return;

            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            Apply(group, interactable);
        }
    }
}
