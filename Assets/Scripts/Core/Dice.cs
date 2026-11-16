using UnityEngine;

// A different course every launch. Unity seeds its generator when the game starts, but a standalone
// build can come up with the same state as the last run, which made the built game deal the same
// faults over and over. Seeding from the clock here makes sure it never repeats.
public static class Dice
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Seed()
    {
        unchecked
        {
            int seed = (int)(System.DateTime.UtcNow.Ticks ^ System.Environment.TickCount);
            Random.InitState(seed == 0 ? 1 : seed);
        }
    }
}
