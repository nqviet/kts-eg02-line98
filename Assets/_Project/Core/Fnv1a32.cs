namespace Line98.Core
{
    /// <summary>
    /// Pure deterministic 32-bit FNV-1a hashing utility per Architecture §12 and Concept Vocabularies §8.
    /// </summary>
    public static class Fnv1a32
    {
        public static uint Compute(string text)
        {
            uint hash = 2166136261U;
            if (string.IsNullOrEmpty(text))
            {
                return hash;
            }

            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619U;
            }

            return hash;
        }
    }
}
