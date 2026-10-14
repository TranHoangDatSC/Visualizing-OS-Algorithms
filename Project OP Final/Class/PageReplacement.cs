using System;
using System.Collections.Generic;
using System.Linq;

namespace Project_OP_Final.Class
{
    /// <summary>
    /// Page replacement algorithms. After a Run method, <see cref="FrameStates"/>[i] holds the frame contents
    /// after reference i and <see cref="PageFaults"/>[i] tells whether reference i caused a page fault.
    /// </summary>
    internal class PageReplacement
    {
        public List<int> ReferenceString { get; }
        public int FrameCount { get; }

        public List<List<int>> FrameStates { get; private set; }
        public List<bool> PageFaults { get; private set; }
        public List<List<bool>> BitStates { get; private set; } // Reference bits, Clock algorithm only

        public int Faults => PageFaults.Count(f => f);

        public PageReplacement(List<int> referenceString, int frameCount)
        {
            ReferenceString = referenceString;
            FrameCount = frameCount;
        }

        /// <summary>Optimal: replace the page that will not be used for the longest time.</summary>
        public void RunOPT()
        {
            Run((frame, i) => IndexOfMax(frame, page => NextUse(page, i)));
        }

        /// <summary>Least Recently Used: replace the page that has not been used for the longest time.</summary>
        public void RunLRU()
        {
            Run((frame, i) => IndexOfMax(frame, page => i - LastUse(page, i)));
        }

        /// <summary>First-In First-Out: replace the oldest loaded page.</summary>
        public void RunFIFO()
        {
            int oldest = 0;
            Run((frame, i) =>
            {
                int victim = oldest;
                oldest = (oldest + 1) % FrameCount;
                return victim;
            });
        }

        /// <summary>Clock (second chance): skip pages whose reference bit is set, clearing the bit.</summary>
        public void RunClock()
        {
            var bits = new List<bool>();
            int hand = 0;
            BitStates = new List<List<bool>>();

            Run(
                selectVictim: (frame, i) =>
                {
                    while (bits[hand])
                    {
                        bits[hand] = false;
                        hand = (hand + 1) % FrameCount;
                    }
                    int victim = hand;
                    hand = (hand + 1) % FrameCount;
                    return victim;
                },
                onAccess: slot =>
                {
                    if (slot == bits.Count) bits.Add(true);
                    else bits[slot] = true;
                },
                afterStep: () => BitStates.Add(new List<bool>(bits)));
        }

        /// <summary>
        /// Common simulation loop. <paramref name="selectVictim"/> returns the frame slot to replace
        /// when all frames are full; <paramref name="onAccess"/> is called with the slot that holds the page.
        /// </summary>
        private void Run(Func<List<int>, int, int> selectVictim,
                         Action<int> onAccess = null,
                         Action afterStep = null)
        {
            var frame = new List<int>();
            FrameStates = new List<List<int>>();
            PageFaults = new List<bool>();

            for (int i = 0; i < ReferenceString.Count; i++)
            {
                int page = ReferenceString[i];
                int slot = frame.IndexOf(page);
                bool fault = slot < 0;

                if (fault)
                {
                    if (frame.Count < FrameCount)
                    {
                        // Free frame available: compulsory miss, still a page fault
                        frame.Add(page);
                        slot = frame.Count - 1;
                    }
                    else
                    {
                        slot = selectVictim(frame, i);
                        frame[slot] = page;
                    }
                }

                onAccess?.Invoke(slot);
                PageFaults.Add(fault);
                FrameStates.Add(new List<int>(frame));
                afterStep?.Invoke();
            }
        }

        /// <summary>Index of the next reference to <paramref name="page"/> after position <paramref name="current"/> (int.MaxValue if never).</summary>
        private int NextUse(int page, int current)
        {
            int next = ReferenceString.IndexOf(page, current + 1);
            return next < 0 ? int.MaxValue : next;
        }

        /// <summary>Index of the last reference to <paramref name="page"/> before position <paramref name="current"/>.</summary>
        private int LastUse(int page, int current)
        {
            return ReferenceString.LastIndexOf(page, current - 1);
        }

        /// <summary>Returns the first slot whose page has the largest key.</summary>
        private static int IndexOfMax(List<int> frame, Func<int, int> key)
        {
            int best = 0;
            for (int slot = 1; slot < frame.Count; slot++)
            {
                if (key(frame[slot]) > key(frame[best]))
                    best = slot;
            }
            return best;
        }
    }
}
