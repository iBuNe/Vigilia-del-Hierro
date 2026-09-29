namespace Purga
{
    /// <summary>
    /// Deterministic, injectable RNG for ALL gameplay randomness (design §1, §5:
    /// "semilla determinista por campaña — recrear/compartir partidas; base del RNG").
    /// The seed lives on the Campaign, so a run can be reproduced or shared by re-seeding.
    ///
    /// Semantics mirror UnityEngine.Random exactly so routing existing calls is behaviour-safe:
    ///   Value           → [0,1] float, like Random.value
    ///   Range(int,int)  → max-EXCLUSIVE, like Random.Range(int,int)
    ///   Range(float,f)  → max-INCLUSIVE, like Random.Range(float,float)
    ///
    /// Backed by System.Random (an isolated instance, NOT UnityEngine's global state),
    /// so editor tooling or the engine touching Random elsewhere can't perturb a run.
    /// </summary>
    public static class Rng
    {
        static System.Random rng = new System.Random();

        /// <summary>The seed currently driving the stream (shown in UI, shareable).</summary>
        public static int Seed { get; private set; }

        /// <summary>Reset the stream to a known seed. Called by Campaign.New so every run is reproducible.</summary>
        public static void Init(int seed)
        {
            Seed = seed;
            rng = new System.Random(seed);
        }

        /// <summary>A fresh, hard-to-guess, non-negative seed for runs the player didn't pin.</summary>
        public static int NewSeed() => System.Guid.NewGuid().GetHashCode() & 0x7fffffff;

        public static float Value => (float)rng.NextDouble();

        // Max EXCLUSIVE, matching UnityEngine.Random.Range(int, int).
        public static int Range(int minInclusive, int maxExclusive)
            => maxExclusive <= minInclusive ? minInclusive : rng.Next(minInclusive, maxExclusive);

        // Max INCLUSIVE, matching UnityEngine.Random.Range(float, float).
        public static float Range(float minInclusive, float maxInclusive)
            => minInclusive + (float)rng.NextDouble() * (maxInclusive - minInclusive);
    }
}
