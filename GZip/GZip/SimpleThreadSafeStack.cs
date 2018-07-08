using System.Collections.Generic;

namespace GZip
{
    public class SimpleThreadSafeStack<T>
    {
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly object _lock = new object();

        public void Push(T obj)
        {
            lock (_lock)
            {
                _stack.Push(obj);
            }
        }

        public T Pop()
        {
            lock (_lock)
            {
                return _stack.Pop();
            }
        }

        public bool IsEmpty { get { return _stack.Count == 0; } }
    }
}