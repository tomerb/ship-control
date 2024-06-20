using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipControlHQ
{
    internal class ThreadedQueue<T>
    {
        public interface IItemsHandler
        {
            public bool ShouldProcessItems();
            public void HandleItem(T item);
        }

        private readonly Queue<T> _queue = new();
        private readonly Thread _workerThread;
        private bool _shouldStop;
        private readonly object _lockObject = new();
        private readonly IItemsHandler _handler;

        public ThreadedQueue(IItemsHandler handler)
        {
            _workerThread = new Thread(ProcessQueueItems);
            _workerThread.Start();
            _handler = handler;
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                Monitor.PulseAll(_queue);
            }
        }

        public void Pulse()
        {
            lock (_queue)
            {
                Monitor.PulseAll(_queue);
            }
        }

        private void ProcessQueueItems()
        {
            while (true)
            {
                T item;
                lock (_queue)
                {
                    if (_queue.Count == 0 || !_handler.ShouldProcessItems())
                    {
                        lock (_lockObject)
                        {
                            if (_shouldStop)
                                return;
                        }
                        Monitor.Wait(_queue);
                        continue;
                    }
                    item = _queue.Dequeue();
                }

                // Process the item
                Console.WriteLine($"Processing item: {item}");
                _handler.HandleItem(item);

                lock (_lockObject)
                {
                    if (_shouldStop)
                        return;
                }
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _shouldStop = true;
            }
            _workerThread.Join();
        }
    }
}
