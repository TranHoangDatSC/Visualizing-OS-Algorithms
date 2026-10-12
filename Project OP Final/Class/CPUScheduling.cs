using System;
using System.Collections.Generic;
using System.Linq;

namespace Project_OP_Final.Class
{
    public class Process
    {
        public string ID { get; set; }
        public int ArrivalTime { get; set; }
        public int BurstTime { get; set; }
        public int Priority { get; set; } // Lower number = higher priority

        public int StartTime { get; set; }      // -1 until the process first gets the CPU
        public int RemainingTime { get; set; }  // Used by the preemptive algorithms
        public int CompletionTime { get; set; }

        public int TurnaroundTime { get; set; }
        public int WaitingTime { get; set; }

        public Process(string id, int arrivalTime, int burstTime, int priority = 0)
        {
            ID = id;
            ArrivalTime = arrivalTime;
            BurstTime = burstTime;
            Priority = priority;
            StartTime = -1;
            RemainingTime = burstTime;
        }

        internal void Complete(int completionTime)
        {
            RemainingTime = 0;
            CompletionTime = completionTime;
            TurnaroundTime = CompletionTime - ArrivalTime;
            WaitingTime = TurnaroundTime - BurstTime;
        }
    }

    /// <summary>One block of the Gantt chart: process <see cref="Id"/> runs from StartTime to EndTime.</summary>
    public class ProcessSnapshot
    {
        public string Id { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }

    /// <summary>
    /// CPU scheduling algorithms. Every algorithm fills in the timing fields of the given processes
    /// (the list order is kept) and returns the Gantt chart timeline.
    /// </summary>
    public static class CPUScheduling
    {
        public const string IdleId = "Idle";

        // ---------------- Non-preemptive ----------------

        public static List<ProcessSnapshot> FCFS(List<Process> processes)
        {
            return RunNonPreemptive(processes, ready => ready
                .OrderBy(p => p.ArrivalTime)
                .First());
        }

        public static List<ProcessSnapshot> SJF(List<Process> processes)
        {
            return RunNonPreemptive(processes, ready => ready
                .OrderBy(p => p.BurstTime)
                .ThenBy(p => p.ArrivalTime)
                .First());
        }

        public static List<ProcessSnapshot> Priority(List<Process> processes)
        {
            return RunNonPreemptive(processes, ready => ready
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.ArrivalTime)
                .First());
        }

        /// <summary>SJF that picks by priority first, then by burst time.</summary>
        public static List<ProcessSnapshot> PrioritySJF(List<Process> processes)
        {
            return RunNonPreemptive(processes, ready => ready
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.BurstTime)
                .ThenBy(p => p.ArrivalTime)
                .First());
        }

        // ---------------- Preemptive ----------------

        public static List<ProcessSnapshot> SRTF(List<Process> processes)
        {
            return RunPreemptive(processes, ready => ready
                .OrderBy(p => p.RemainingTime)
                .ThenBy(p => p.ArrivalTime)
                .First());
        }

        /// <summary>Preemptive priority scheduling, ties broken by the shortest remaining time.</summary>
        public static List<ProcessSnapshot> PrioritySRTF(List<Process> processes)
        {
            return RunPreemptive(processes, ready => ready
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.RemainingTime)
                .ThenBy(p => p.ArrivalTime)
                .First());
        }

        public static List<ProcessSnapshot> RoundRobin(List<Process> processes, int quantumTime)
        {
            var timeline = new List<ProcessSnapshot>();
            var readyQueue = new Queue<Process>();
            var arrivals = processes.OrderBy(p => p.ArrivalTime).ToList();
            int arrivalIndex = 0;
            int currentTime = 0;
            int completedCount = 0;

            void EnqueueArrivals()
            {
                while (arrivalIndex < arrivals.Count && arrivals[arrivalIndex].ArrivalTime <= currentTime)
                    readyQueue.Enqueue(arrivals[arrivalIndex++]);
            }

            while (completedCount < processes.Count)
            {
                EnqueueArrivals();

                if (readyQueue.Count == 0)
                {
                    // CPU stays idle until the next arrival
                    int nextArrival = arrivals[arrivalIndex].ArrivalTime;
                    AddSegment(timeline, IdleId, currentTime, nextArrival);
                    currentTime = nextArrival;
                    continue;
                }

                Process process = readyQueue.Dequeue();
                if (process.StartTime < 0)
                    process.StartTime = currentTime;

                int executionTime = Math.Min(quantumTime, process.RemainingTime);
                AddSegment(timeline, process.ID, currentTime, currentTime + executionTime);
                currentTime += executionTime;
                process.RemainingTime -= executionTime;

                // Processes arriving during this slice enter the queue before the preempted one
                EnqueueArrivals();

                if (process.RemainingTime == 0)
                {
                    process.Complete(currentTime);
                    completedCount++;
                }
                else
                {
                    readyQueue.Enqueue(process);
                }
            }

            return timeline;
        }

        /// <summary>
        /// Round Robin among the processes with the highest priority. A process is preempted when its
        /// quantum expires or when a process with a strictly higher priority arrives.
        /// </summary>
        public static List<ProcessSnapshot> PriorityRoundRobin(List<Process> processes, int quantumTime)
        {
            var timeline = new List<ProcessSnapshot>();
            var readyQueue = new List<Process>(); // Kept in FIFO order
            var arrivals = processes.OrderBy(p => p.ArrivalTime).ToList();
            int arrivalIndex = 0;
            int currentTime = 0;
            int completedCount = 0;
            int quantumUsed = 0;
            Process current = null;

            void EnqueueArrivals()
            {
                while (arrivalIndex < arrivals.Count && arrivals[arrivalIndex].ArrivalTime <= currentTime)
                    readyQueue.Add(arrivals[arrivalIndex++]);
            }

            while (completedCount < processes.Count)
            {
                EnqueueArrivals();

                if (current != null && readyQueue.Any(p => p.Priority < current.Priority))
                {
                    readyQueue.Add(current);
                    current = null;
                }

                if (current == null)
                {
                    if (readyQueue.Count == 0)
                    {
                        AddSegment(timeline, IdleId, currentTime, currentTime + 1);
                        currentTime++;
                        continue;
                    }

                    // First process in FIFO order among those with the highest priority
                    int bestPriority = readyQueue.Min(p => p.Priority);
                    current = readyQueue.First(p => p.Priority == bestPriority);
                    readyQueue.Remove(current);
                    quantumUsed = 0;

                    if (current.StartTime < 0)
                        current.StartTime = currentTime;
                }

                AddSegment(timeline, current.ID, currentTime, currentTime + 1);
                current.RemainingTime--;
                currentTime++;
                quantumUsed++;

                if (current.RemainingTime == 0)
                {
                    current.Complete(currentTime);
                    completedCount++;
                    current = null;
                }
                else if (quantumUsed == quantumTime)
                {
                    EnqueueArrivals();
                    readyQueue.Add(current);
                    current = null;
                }
            }

            return timeline;
        }

        // ---------------- Helpers ----------------

        private static List<ProcessSnapshot> RunNonPreemptive(List<Process> processes, Func<List<Process>, Process> pickNext)
        {
            var timeline = new List<ProcessSnapshot>();
            var pending = new List<Process>(processes);
            int currentTime = 0;

            while (pending.Count > 0)
            {
                var ready = pending.Where(p => p.ArrivalTime <= currentTime).ToList();
                if (ready.Count == 0)
                {
                    int nextArrival = pending.Min(p => p.ArrivalTime);
                    AddSegment(timeline, IdleId, currentTime, nextArrival);
                    currentTime = nextArrival;
                    continue;
                }

                Process next = pickNext(ready);
                pending.Remove(next);

                next.StartTime = currentTime;
                currentTime += next.BurstTime;
                next.Complete(currentTime);
                AddSegment(timeline, next.ID, next.StartTime, currentTime);
            }

            return timeline;
        }

        /// <summary>Simulates one time unit at a time, re-selecting the process on every tick.</summary>
        private static List<ProcessSnapshot> RunPreemptive(List<Process> processes, Func<List<Process>, Process> pickNext)
        {
            var timeline = new List<ProcessSnapshot>();
            int currentTime = 0;
            int completedCount = 0;

            while (completedCount < processes.Count)
            {
                var ready = processes.Where(p => p.ArrivalTime <= currentTime && p.RemainingTime > 0).ToList();
                if (ready.Count == 0)
                {
                    AddSegment(timeline, IdleId, currentTime, currentTime + 1);
                    currentTime++;
                    continue;
                }

                Process next = pickNext(ready);
                if (next.StartTime < 0)
                    next.StartTime = currentTime;

                AddSegment(timeline, next.ID, currentTime, currentTime + 1);
                next.RemainingTime--;
                currentTime++;

                if (next.RemainingTime == 0)
                {
                    next.Complete(currentTime);
                    completedCount++;
                }
            }

            return timeline;
        }

        /// <summary>Appends a block to the timeline, merging it with the previous one when the same process keeps running.</summary>
        private static void AddSegment(List<ProcessSnapshot> timeline, string id, int start, int end)
        {
            if (end <= start) return;

            ProcessSnapshot last = timeline.Count > 0 ? timeline[timeline.Count - 1] : null;
            if (last != null && last.Id == id && last.EndTime == start)
            {
                last.EndTime = end;
                return;
            }

            timeline.Add(new ProcessSnapshot { Id = id, StartTime = start, EndTime = end });
        }
    }
}
