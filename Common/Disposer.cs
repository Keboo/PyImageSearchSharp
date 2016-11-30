using System;
using System.Collections.Generic;

namespace PyImageSearchSharp
{
        public class Disposer : IDisposable
    {
        private bool _disposed;

        private readonly HashSet<IDisposable> _disposableItems = new HashSet<IDisposable>();

        public bool Add(IDisposable item)
        {
            return _disposableItems.Add(item);
        }

        public void Add(IEnumerable<IDisposable> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (IDisposable item in items)
            {
                Add(item);
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                foreach (IDisposable item in _disposableItems)
                {
                    item?.Dispose();
                }
                _disposableItems.Clear();
            }
            _disposed = true;
        }

        ~Disposer()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}