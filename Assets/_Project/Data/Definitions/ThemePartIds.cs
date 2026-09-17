namespace Line98.Data
{
    /// <summary>Flattened part ids of a single theme pack.</summary>
    public readonly struct ThemePartIds
    {
        public readonly string BallId;
        public readonly string BoardId;
        public readonly string UiId;
        public readonly string ClearEffectId;

        public ThemePartIds(string ballId, string boardId, string uiId, string clearEffectId)
        {
            BallId = ballId;
            BoardId = boardId;
            UiId = uiId;
            ClearEffectId = clearEffectId;
        }

        public string Get(ThemeCategory category)
        {
            return category switch
            {
                ThemeCategory.Ball => BallId,
                ThemeCategory.Board => BoardId,
                ThemeCategory.Ui => UiId,
                ThemeCategory.ClearEffect => ClearEffectId,
                _ => null
            };
        }
    }
}
