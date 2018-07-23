using System.Collections.Generic;
using System.Threading;

namespace GZip
{
    public class SimpleThreadSafeQueue<T>
    {
        private readonly Queue<T> _stack = new Queue<T>();
        private int _count = 0;
        public void Enqueue(T obj)
        {
            lock (_stack)
            {
                _stack.Enqueue(obj);
                _count++;
                Monitor.PulseAll(_stack);
            }
        }

        public T Dequeue()
        {
            lock (_stack)
            {
                while (_stack.Count == 0)
                    Monitor.Wait(_stack);
                _count--;
                return _stack.Dequeue();
            }
        }

        public int Count
        {
            get { return _count; }
        }
    }
}