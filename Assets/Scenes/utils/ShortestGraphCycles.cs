using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortestGraphCycles
{
    private IEnumerable<Crossroad> vertices;

    private Dictionary<Crossroad, short> color;
    private Dictionary<Crossroad, List<int>> mark;
    private Dictionary<Crossroad, Road> parent;

    private List<List<Road>> cycles;
    private List<List<Road>> shortestCycles;

    private int cycleNumber = 0;

    public ShortestGraphCycles(IEnumerable<Crossroad> vertices)
    {
        this.vertices = vertices;

        color = new Dictionary<Crossroad, short>();
        mark = new Dictionary<Crossroad, List<int>>();
        parent = new Dictionary<Crossroad, Road>();
        cycles = new List<List<Road>>();
        shortestCycles = new List<List<Road>>();
    }

    public List<List<Road>> Compute()
    {
        if (vertices == null)
            return null;

        IEnumerator<Crossroad> e = this.vertices.GetEnumerator();

        if (e.MoveNext())
        {
            Crossroad v = e.Current;
            Debug.LogFormat("valore del primo crossroad: {0}", v);

            DfsCycle(v, null, null);
            return shortestCycles;
        }
        return null;
    }

    private void DfsCycle(Crossroad u, Crossroad p, Road road)
    {
        if (!color.ContainsKey(u))
        {
            parent[u] = road;
            color.Add(u, 1);

            foreach (Road r in u.GetRoadList())
            {
                Crossroad v = r.start == u ? r.end : r.start;

                if (v == p) continue;

                DfsCycle(v, u, r);
            }
            color[u] = 2;
            return;
        }

        if (color[u] == 2)
            return;

        if (color[u] == 1)
        {
            int[] firstCycles = null;
            List<Road> cycle = new List<Road>();

            cycle.Add(road);

            Crossroad curr = p;

            if (mark.ContainsKey(curr))
                firstCycles = mark[curr].ToArray();
            else
                mark.Add(curr, new List<int>());
            mark[curr].Add(cycleNumber);

            while (curr != u)
            {
                Road r = parent[curr];
                curr = r.start == curr ? r.end : r.start;
                if (mark.ContainsKey(curr))
                {
                    if (firstCycles == null)
                        firstCycles = mark[curr].ToArray();
                }
                else
                    mark.Add(curr, new List<int>());
                mark[curr].Add(cycleNumber);

                cycle.Add(r);
            }

            cycles.Add(cycle);
            this.cycleNumber++;

            if (firstCycles == null)
                shortestCycles.Add(cycle);
            else
            {
                List<Road> shortestCycle = null;

                foreach(int i in firstCycles)
                {
                    List<Road> candidate = UnionMinusIntersection(cycle, cycles[i]);

                    if (candidate == null) continue;

                    if (shortestCycle == null || (shortestCycle != null && shortestCycle.Count > candidate.Count))
                        shortestCycle = candidate;

                }

                if (shortestCycle == null) return;

                shortestCycles.Add(shortestCycle);

                foreach(Road r in shortestCycle)
                {
                    if (mark.ContainsKey(r.start))
                    {
                        if (!mark[r.start].Contains(cycleNumber))
                            mark[r.start].Add(cycleNumber);
                    }

                    if (mark.ContainsKey(r.end))
                    {
                        if (!mark[r.end].Contains(cycleNumber))
                            mark[r.start].Add(cycleNumber);
                    }
                }
                cycleNumber++;

                cycles.Add(shortestCycle);
            }
            return;
        }
    }

    private List<Road> UnionMinusIntersection(List<Road> c1, List<Road> c2)
    {
        List<Road> result = new List<Road>();

        foreach(Road r in c1)
        {
            if (!c2.Contains(r))
                result.Add(r);
        }
        return result.Count > 0 ? result : null;
    }
}
