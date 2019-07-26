using System;
using System.Collections.Generic;
using Brain_uwp.Data;
using Windows.Graphics.Imaging;

namespace Brain_uwp.ImageRecognition
{
    /// <summary>
    /// All the resulting detected plates are collected in this pool
    /// </summary>
	public class ResultPool
	{
		private static ResultPool _instance;

		private static readonly object _lock = new object();

        /// <summary>
        /// Gets the instance
        /// </summary>
		public static ResultPool Instance {
			get {
				if(_instance == null)
				{
					_instance = new ResultPool();
				}
				return _instance;
			}
		}

		private Queue<DetectedLpData> pool;
		private uint limit = 1026;

		private ResultPool()
		{
			pool = new Queue<DetectedLpData>();
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
        /// Get the next DetectedLpData
        /// </summary>
        /// <returns><see cref="DetectedLpData"/></returns>
        public DetectedLpData GetNext()
		{
			lock (_lock)
			{
				return pool.Dequeue();
			}
		}

        /// <summary>
        /// Enqueue new DetectedLpData
        /// </summary>
        /// <param name="detectedLpData"></param>
        public void Enqueue(DetectedLpData detectedLpData)
		{
			lock (_lock)
			{
				if(GetCount() <= limit)
				{
					pool.Enqueue(detectedLpData);
				}
				else
				{
					throw new OutOfMemoryException();
				}
			}
		}
	}
}
