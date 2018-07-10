using System.Runtime.InteropServices;
using System.Threading;

namespace GZip
{
    public class ThreadsManager
    {
        private readonly Thread[] _workThreads;
        private SimpleThreadSafeStack<Frame> _stack;

        public ThreadsManager(int threadCount)
        {
            _stack = new SimpleThreadSafeStack<Frame>();

            _workThreads = new Thread[threadCount];
            for (int i=0; i<_workThreads.Length; i++)
            {
                _workThreads[i] = new Thread(Process);
            }

        }

        public bool IsWork { get; }

        private void Process()
        {
            while (true)
            {
                var frame = _stack.Pop();

            }
        }

        public void Start()
        {
            foreach (var thread in _workThreads)
            {
                thread.Start();
            }
        }

        public void Stop()
        {
            foreach (var thread in _workThreads)
            {
                th
            }
        }
    }
}