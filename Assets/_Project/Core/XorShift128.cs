using System;

namespace Line98.Core
{
    /// <summary>
    /// Deterministic, serializable PRNG (XorShift128+).
    /// Used across Core and Gameplay to guarantee identical behavior across platforms,
    /// exact reproducibility for Daily Challenge seeds, and true Undo capability.
    /// </summary>
    [Serializable]
    public struct XorShift128 : IRandomSource
    {
        public ulong S0;
        public ulong S1;
        public ulong S2;
        public ulong S3;

        public (ulong s0, ulong s1, ulong s2, ulong s3) GetState() => (S0, S1, S2, S3);

        public void RestoreState(ulong s0, ulong s1, ulong s2, ulong s3)
        {
            S0 = s0;
            S1 = s1;
            S2 = s2;
            S3 = s3;
        }

        public XorShift128(uint seed)
        {
            S0 = 0;
            S1 = 0;
            S2 = 0;
            S3 = 0;
            SeedFrom(seed);
        }

        public void SeedFrom(uint seed)
        {
            // SplitMix64 initialization
            ulong state = seed != 0 ? seed : 0xDEADBEEF;
            S0 = NextSplitMix64(ref state);
            S1 = NextSplitMix64(ref state);
            S2 = NextSplitMix64(ref state);
            S3 = NextSplitMix64(ref state);
        }

        private static ulong NextSplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public uint NextUInt()
        {
            return (uint)(NextULong() >> 32);
        }

        public ulong NextULong()
        {
            ulong result = S0 + S3;
            ulong t = S1 << 17;

            S2 ^= S0;
            S3 ^= S1;
            S1 ^= S2;
            S0 ^= S3;

            S2 ^= t;
            S3 = (S3 << 45) | (S3 >> 19);

            return result;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                return minInclusive;
            }

            uint range = (uint)(maxExclusive - minInclusive);
            uint sample = NextUInt();
            return (int)(minInclusive + (sample % range));
        }
    }
}
