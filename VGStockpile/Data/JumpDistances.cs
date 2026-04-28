using System.Collections.Generic;
using Source.Galaxy;

namespace VGStockpile.Data;

internal static class JumpDistances
{
    /// <summary>
    /// Returns systemGuid → jump count from the player's current system.
    /// Computed once via BFS over <see cref="SystemMapData.GetAdjacentSystems"/>.
    /// Current system reports 0; unreachable systems are absent from the
    /// returned dictionary.
    /// </summary>
    public static IReadOnlyDictionary<string, int> ComputeFromCurrent()
    {
        var distances = new Dictionary<string, int>();
        var start = SystemMapData.current;
        if (start is null) return distances;

        distances[start.guid] = 0;
        var queue = new Queue<SystemMapData>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var d = distances[node.guid];
            foreach (var nb in node.GetAdjacentSystems())
            {
                if (nb is null) continue;
                var id = nb.guid;
                if (string.IsNullOrEmpty(id)) continue;
                if (distances.ContainsKey(id)) continue;
                distances[id] = d + 1;
                queue.Enqueue(nb);
            }
        }
        return distances;
    }
}
