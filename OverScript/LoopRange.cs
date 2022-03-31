using System.Collections;

namespace OverScript
{
    struct LoopRange : IEnumerable
    {
        public int Start, Stop, Step;
        public LoopRange(int start, int stop, int step)
        {
            Start = start;
            Stop = stop;
            Step = step;
        }
        public IEnumerator GetEnumerator()
        {
            return new RangeEnumerator(Start, Stop, Step);
        }
        public class RangeEnumerator : IEnumerator
        {
            public int Start, Stop, Step, CurrentValue;
            bool Reseted = false;
            public RangeEnumerator(int start, int stop, int step)
            {
                Start = start;
                Stop = stop;
                Step = step;
                Reset();
            }

            public bool MoveNext()
            {
                if (Reseted)
                {
                    CurrentValue = Start;
                    Reseted = false;
                }
                else
                    CurrentValue += Step;

                return Step > 0 ? (CurrentValue < Stop) : (CurrentValue > Stop);
            }

            public void Reset()
            {
                CurrentValue = 0;
                Reseted = true;
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public int Current
            {
                get
                {
                    return CurrentValue;

                }
            }
        }
    }
}
