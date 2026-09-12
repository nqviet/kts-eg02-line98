namespace Line98.Core
{
    /// <summary>
    /// Color type for balls. None (0) indicates an empty cell.
    /// Exactly 7 distinct colors per GDD §2.
    /// </summary>
    public enum BallColor : byte
    {
        None = 0,
        Red = 1,
        Orange = 2,
        Yellow = 3,
        Green = 4,
        Cyan = 5,
        Purple = 6,
        Pink = 7
    }
}
