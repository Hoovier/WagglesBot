using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWaggles
{
    static class levenshtein
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        /// <summary>
        /// Checks if two strings are within maxDistance of each other.
        /// Returns -1 if maxDistance is bypassed.
        ///   (distance == maxDistance) is a valid result.
        /// </summary> 
        public static int ComputeClamp(string s, string t, int maxDistance)
        {
            // If strings are equal, edit distance is zero.
            if (s.Equals(t)) {
                return 0;
            }

            // Swap so `t` is the smaller string. Saves on memory buffer size later.
            if (t.Length > s.Length) {
                string tmp = s;
                s = t;
                t = tmp;
            }

            int sL = s.Length, tL = t.Length;
            if (sL == 0) {
                return tL;
            }
            if (tL == 0) {
                return sL;
            }

            // Still Step 1: Additional case to check if they violate the maxDistance already.
            if (Math.Abs(sL - tL) > maxDistance) {
                return -1;
            }

            // Initialize maxtrix only when necessary, for computations.
            int[,] d = new int[sL + 1, tL + 1];

            // Step 2
            for (int i = 0; i <= sL; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= tL; d[0, j] = j++)
            {
            }

            // Step 3 & 4: Iterate over all p
            for (int i = 1; i <= sL; i++)
            {
                for (int j = 1; j <= tL; j++)
                {
                    // If the current cost is already too large, skip cost analysis.
                    if (d[i - 1, j - 1] > maxDistance) {
                        // Any further cells that build off this one carry the clamped maximum cost.
                        d[i,j] = d[i - 1, j - 1];
                        continue;
                    }

                    // Step 5: Calculate the new cost, if there is an additional edit.
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6: Get the least cost between the neighbors adjacent or diagonal.
                    d[i, j] = Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1);
                    d[i, j] = Math.Min(d[i, j], d[i - 1, j - 1] + cost);
                }
            }
            // Step 7: Return the final cost tabulated between words.
            return d[sL, tL] > maxDistance ? -1 : d[sL, tL];
        }
    }
}
