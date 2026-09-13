namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Target interface for zero-allocation tween callbacks.
    /// Eliminates closure and delegate allocations when tweening pooled entities.
    /// </summary>
    public interface ITweenTarget
    {
        void OnTweenUpdate(int actionId, float value);
        void OnTweenComplete(int actionId);
    }
}
