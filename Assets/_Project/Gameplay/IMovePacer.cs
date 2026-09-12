using System;
using Line98.Core;

namespace Line98.Gameplay
{
    /// <summary>
    /// Narrow inversion seam between Gameplay and Presentation.
    /// In production, MoveAnimator/FeedbackDirector paces animations then calls commit.
    /// In tests or headless runs, ImmediatePacer invokes commit immediately.
    /// </summary>
    public interface IMovePacer
    {
        void Play(MovePlan plan, Action commitCallback);
    }

    public sealed class ImmediatePacer : IMovePacer
    {
        public static ImmediatePacer Instance { get; } = new ImmediatePacer();

        public void Play(MovePlan plan, Action commitCallback)
        {
            commitCallback?.Invoke();
        }
    }
}
