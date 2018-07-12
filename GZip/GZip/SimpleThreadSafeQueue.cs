using System.Collections.Generic;
using System.Threading;

namespace GZip
{
    public class SimpleThreadSafeQueue<T>
    {
        private readonly Queue<T> _stack = new Queue<T>();

        public void Push(T obj)
        {
            lock (_stack)
            {
                _stack.Enqueue(obj);
                Monitor.PulseAll(_stack);
            }
        }

        public T Pop()
        {
            lock (_stack)
            {
                while (_stack.Count == 0)
                    Monitor.Wait(_stack);
                return _stack.Dequeue();
            }
        }
    }
}