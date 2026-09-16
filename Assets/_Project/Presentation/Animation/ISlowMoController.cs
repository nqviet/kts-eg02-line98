namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Contract for presentation-local slow motion scaling.
    /// Never touches Time.timeScale per Animation §1 rule 4.
    /// </summary>
    public interface ISlowMoController
    {
        void RequestSlowMo(float scale, float durationSeconds);
    }
}
