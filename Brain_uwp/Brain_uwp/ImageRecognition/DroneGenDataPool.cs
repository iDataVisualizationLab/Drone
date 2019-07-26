using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brain_uwp.Data;

namespace Brain_uwp.ImageRecognition
{
    /*TODO: LIMIT ACCESS TO THIS CLASS*/
    /// <summary>
    /// Meta data about images that are taken by the drone is stored in this singleton class
    /// </summary>
    public class DroneGenDataPool
    {
        private static DroneGenDataPool _instance;

        /// <summary>
        /// Locks the access so that multiple threads could accesses it without data loss
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the instance
        /// </summary>
        public static DroneGenDataPool Instance {
            get {
                if (_instance == null)
                {
                    _instance = new DroneGenDataPool();
                }
                return _instance;
            }
        }

        private Queue<DroneGenMetaData> pool;
        private uint limit = 4096;
        private DroneGenDataPool()
        {
            pool = new Queue<DroneGenMetaData>();
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
        /// Get the next DroneGenMetaData
        /// </summary>
        /// <returns><see cref="DroneGenMetaData"/></returns>
        public DroneGenMetaData GetNext()
        {
            lock (_lock)
            {
                return pool.Dequeue();
            }
        }

        /// <summary>
        /// Enqueue new DroneGenMetaData
        /// </summary>
        /// <param name="genMetaData">MetaData that is newly generated</param>
        public void Enqueue(DroneGenMetaData genMetaData)
        {
            lock (_lock)
            {
                if (GetCount() <= limit)
                {
                    pool.Enqueue(genMetaData);
                }
                else
                {
                    throw new OutOfMemoryException();
                }
            }
        }
    }
}
