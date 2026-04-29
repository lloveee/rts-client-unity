namespace RTS.Sim
{
    public static class SimVictory
    {
        public static PlayerResult[] CheckVictory(World w)
        {
            if (w.Players.Count == 0) return null;
            var aliveBld = new bool[w.Players.Count];
            for (int i = 0; i < w.Buildings.Count; i++)
            {
                var b = w.Buildings[i];
                if (b.State != BuildingState.Dead && b.Owner < aliveBld.Length)
                    aliveBld[b.Owner] = true;
            }
            var losers = new bool[w.Players.Count];
            int living = 0;
            for (int i = 0; i < w.Players.Count; i++)
            {
                if (!aliveBld[i] || w.Players[i].Surrendered) losers[i] = true;
                else living++;
            }
            if (living == w.Players.Count) return null;
            var results = new PlayerResult[w.Players.Count];
            for (int i = 0; i < w.Players.Count; i++)
            {
                byte r;
                if (living == 1) r = losers[i] ? (byte)2 : (byte)1;
                else r = 3;
                results[i] = new PlayerResult { PlayerID = (byte)i, Result = r };
            }
            return results;
        }
    }
}
