namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Contract for systems and animators updated by the single PresentationRoot.Tick loop.
    /// Ban on scattered MonoBehaviour.Update loops per Architecture §2 and Animation §2.
    /// </summary>
    public interface ITickable
    {
        void Tick(float dt);
    }
}
