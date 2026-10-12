using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project_OP_Final.Class
{
    public enum RequestResult
    {
        Granted,
        ExceedsNeed,      // Process asked for more than its declared maximum
        MustWait,         // Not enough available resources right now
        UnsafeState       // Granting would leave the system in an unsafe state
    }

    public class BankerAlgorithm
    {
        public int ProcessCount { get; }
        public int ResourceCount { get; }

        public int[,] MaxDemand { get; }
        public int[,] Allocation { get; }
        public int[] Available { get; }

        public BankerAlgorithm(int processCount, int resourceCount)
        {
            ProcessCount = processCount;
            ResourceCount = resourceCount;
            MaxDemand = new int[processCount, resourceCount];
            Allocation = new int[processCount, resourceCount];
            Available = new int[resourceCount];
        }

        /// <summary>Need = Max - Allocation</summary>
        public int[,] Need
        {
            get
            {
                int[,] need = new int[ProcessCount, ResourceCount];
                for (int i = 0; i < ProcessCount; i++)
                    for (int j = 0; j < ResourceCount; j++)
                        need[i, j] = MaxDemand[i, j] - Allocation[i, j];
                return need;
            }
        }

        /// <summary>
        /// Safety algorithm. <paramref name="safeSequence"/> holds the processes that can finish (in order).
        /// <paramref name="isDeadlock"/> is true when no process at all can proceed.
        /// </summary>
        public bool IsSafe(out List<int> safeSequence, out bool isDeadlock)
        {
            bool[] finish = RunSafetyCheck(out safeSequence);
            isDeadlock = safeSequence.Count == 0 && finish.Any(f => !f);
            return finish.All(f => f);
        }

        /// <summary>Resource-request algorithm. Resources are only allocated when the result is Granted.</summary>
        public RequestResult RequestResource(int processIndex, int[] request)
        {
            int[,] need = Need;
            for (int j = 0; j < ResourceCount; j++)
            {
                if (request[j] > need[processIndex, j])
                    return RequestResult.ExceedsNeed;
            }

            for (int j = 0; j < ResourceCount; j++)
            {
                if (request[j] > Available[j])
                    return RequestResult.MustWait;
            }

            // Pretend to allocate, then check whether the system is still safe
            ApplyRequest(processIndex, request, 1);

            if (IsSafe(out _, out _))
                return RequestResult.Granted;

            ApplyRequest(processIndex, request, -1); // Roll back
            return RequestResult.UnsafeState;
        }

        public string SuggestDeadlockSolution()
        {
            bool[] finish = RunSafetyCheck(out _);
            List<int> blocked = Enumerable.Range(0, ProcessCount).Where(i => !finish[i]).ToList();

            if (blocked.Count == 0)
                return "No deadlock to solve.";

            int[,] need = Need;
            var solution = new StringBuilder();
            solution.AppendLine($"Deadlock involves process(es): {string.Join(", ", blocked.Select(p => $"P{p}"))}.");

            foreach (int p in blocked)
            {
                string allocated = string.Join(", ", Enumerable.Range(0, ResourceCount).Select(r => Allocation[p, r]));
                string needed = string.Join(", ", Enumerable.Range(0, ResourceCount).Select(r => need[p, r]));
                solution.AppendLine($"- P{p} is waiting. Allocated: ({allocated}). Needed: ({needed}). " +
                                    $"Consider terminating P{p} or freeing its resources.");
            }

            return solution.ToString();
        }

        private bool[] RunSafetyCheck(out List<int> safeSequence)
        {
            safeSequence = new List<int>();
            int[,] need = Need;
            int[] work = (int[])Available.Clone();
            bool[] finish = new bool[ProcessCount];

            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < ProcessCount; i++)
                {
                    if (finish[i] || !CanSatisfy(need, i, work))
                        continue;

                    // Process i can finish and release everything it holds
                    for (int j = 0; j < ResourceCount; j++)
                        work[j] += Allocation[i, j];

                    finish[i] = true;
                    safeSequence.Add(i);
                    changed = true;
                }
            } while (changed);

            return finish;
        }

        private bool CanSatisfy(int[,] need, int process, int[] work)
        {
            for (int j = 0; j < ResourceCount; j++)
            {
                if (need[process, j] > work[j])
                    return false;
            }
            return true;
        }

        private void ApplyRequest(int processIndex, int[] request, int sign)
        {
            for (int j = 0; j < ResourceCount; j++)
            {
                Available[j] -= sign * request[j];
                Allocation[processIndex, j] += sign * request[j];
            }
        }
    }
}
