using System;
using System.Collections.Generic;
using System.Linq;

namespace Project_OP_Final.Class
{
    public enum Direction { Left, Right }

    /// <summary>One movement of the disk head.</summary>
    public struct SeekStep
    {
        public int From { get; }
        public int To { get; }
        public int Distance => Math.Abs(To - From);

        public SeekStep(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// Disk scheduling algorithms. Each one returns the list of head movements;
    /// the total head movement is <see cref="TotalMovement"/>.
    /// </summary>
    public static class DiskScheduling
    {
        public static int TotalMovement(IEnumerable<SeekStep> steps) => steps.Sum(s => s.Distance);

        public static List<SeekStep> FCFS(IEnumerable<int> requests, int head)
        {
            var path = new HeadPath(head);
            foreach (int r in requests)
                path.Serve(r);
            return path.Steps;
        }

        public static List<SeekStep> SSTF(IEnumerable<int> requests, int head)
        {
            var path = new HeadPath(head);
            var pending = requests.ToList();

            while (pending.Count > 0)
            {
                int closest = pending.OrderBy(r => Math.Abs(r - path.Position)).First();
                path.Serve(closest);
                pending.Remove(closest);
            }
            return path.Steps;
        }

        /// <summary>Elevator: serve requests in the direction, go to the end of the disk, then reverse.</summary>
        public static List<SeekStep> SCAN(IEnumerable<int> requests, int head, int diskSize, Direction direction)
        {
            Split(requests, head, direction, out List<int> ahead, out List<int> behind);
            int edge = direction == Direction.Right ? diskSize - 1 : 0;

            var path = new HeadPath(head);
            path.Serve(ahead);
            path.MoveToEdge(edge);
            path.Serve(behind);
            return path.Steps;
        }

        /// <summary>
        /// Circular SCAN: serve requests in the direction, go to the end of the disk,
        /// jump to the opposite end and keep moving in the same direction.
        /// </summary>
        public static List<SeekStep> CSCAN(IEnumerable<int> requests, int head, int diskSize, Direction direction)
        {
            Split(requests, head, direction, out List<int> ahead, out List<int> behind);
            int edge = direction == Direction.Right ? diskSize - 1 : 0;
            int oppositeEdge = direction == Direction.Right ? 0 : diskSize - 1;

            var path = new HeadPath(head);
            path.Serve(ahead);
            path.MoveToEdge(edge);
            if (behind.Count > 0)
            {
                path.MoveToEdge(oppositeEdge); // The return seek is counted as head movement
                behind.Reverse();          // Continue in the original direction
                path.Serve(behind);
            }
            return path.Steps;
        }

        /// <summary>Like SCAN, but the head only goes as far as the last request in each direction.</summary>
        public static List<SeekStep> LOOK(IEnumerable<int> requests, int head, Direction direction)
        {
            Split(requests, head, direction, out List<int> ahead, out List<int> behind);

            var path = new HeadPath(head);
            path.Serve(ahead);
            path.Serve(behind);
            return path.Steps;
        }

        /// <summary>Like C-SCAN, but the head jumps from the last request to the farthest request on the other side.</summary>
        public static List<SeekStep> CLOOK(IEnumerable<int> requests, int head, Direction direction)
        {
            Split(requests, head, direction, out List<int> ahead, out List<int> behind);
            behind.Reverse(); // Continue in the original direction after the jump

            var path = new HeadPath(head);
            path.Serve(ahead);
            path.Serve(behind);
            return path.Steps;
        }

        /// <summary>
        /// Splits the requests into those ahead of the head in the given direction (in service order)
        /// and those behind it (in order of distance from the head).
        /// </summary>
        private static void Split(IEnumerable<int> requests, int head, Direction direction,
                                  out List<int> ahead, out List<int> behind)
        {
            var sorted = requests.OrderBy(r => r).ToList();

            if (direction == Direction.Right)
            {
                ahead = sorted.Where(r => r >= head).ToList();
                behind = sorted.Where(r => r < head).Reverse().ToList();
            }
            else
            {
                ahead = sorted.Where(r => r <= head).Reverse().ToList();
                behind = sorted.Where(r => r > head).ToList();
            }
        }

        /// <summary>Records the head movements while the head serves requests.</summary>
        private class HeadPath
        {
            public List<SeekStep> Steps { get; } = new List<SeekStep>();
            public int Position { get; private set; }

            public HeadPath(int head)
            {
                Position = head;
            }

            public void Serve(int request)
            {
                Steps.Add(new SeekStep(Position, request));
                Position = request;
            }

            public void Serve(IEnumerable<int> requests)
            {
                foreach (int r in requests)
                    Serve(r);
            }

            /// <summary>Moves to a disk edge (skipped when the head is already there).</summary>
            public void MoveToEdge(int edge)
            {
                if (Position != edge)
                    Serve(edge);
            }
        }
    }
}
