using System;
using System.Collections.Generic;
using Windows.Graphics.Imaging;
using Brain_uwp.Data;

namespace Brain_uwp.ImageRecognition
{
    /// <summary>
    /// Images that are taken by the drone is stored in this singleton class
    /// </summary>
    public class InputPool
	{
		private static InputPool _instance;

		private static readonly object _lock = new object();

        /// <summary>
        /// Gets the instance of this singleton
        /// </summary>
		public static InputPool Instance {
			get {
				if (_instance == null)
				{
					_instance = new InputPool();
				}
				return _instance;
			}
		}

		private Queue<LPImageData> pool;
		private uint limit = 4096;
		private InputPool()
		{
			pool = new Queue<LPImageData>();
		}

        /// <summary>
        /// Check if the queue has next
        /// </summary>
        /// <returns><c>true: if has next,false: else</c></returns>
        public bool HasNext()
		{
			lock (_lock)
			{
				return pool.Count != 0;
			}
		}

        /// <summary>
        /// Get the how many elements in the queue
        /// </summary>
        /// <returns>total number of elements in the queue</returns>
        public int GetCount()
		{
			lock (_lock)
			{
				return pool.Count;
			}
		}

        /// <summary>
        /// Get the next LPImageData
        /// </summary>
        /// <returns><see cref="LPImageData"/></returns>
        public LPImageData GetNext()
		{
			lock (_lock)
			{
				return pool.Dequeue();
			}
		}

        /// <summary>
        /// Enqueue new LPImageData
        /// </summary>
        /// <param name="lPImageData"></param>
        public void Enqueue(LPImageData lPImageData)
		{
			lock (_lock)
			{
				if (GetCount() <= limit)
				{
					pool.Enqueue(lPImageData);
				}
				else
				{
					throw new OutOfMemoryException();
				}
			}
		}
	}
}

