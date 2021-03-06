﻿using System.Collections.Concurrent;

namespace HSR.PresWriter.PenTracking
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        /// <summary>
        /// Size of the queue
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Creating the queue
        /// </summary>
        /// <param name="size">fixed size</param>
        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        /// <summary>
        /// Enqueue an item, eventually deleting an old one
        /// </summary>
        /// <param name="obj"></param>
        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (this)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }
}
