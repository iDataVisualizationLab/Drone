using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brain_uwp.Data
{
    /// <summary>
    /// This structure holds the data about newly generated data that is coming from the drone, so it can be used to download
    /// </summary>
    public struct DroneGenMetaData
    {
        /// <summary>
        /// The newly generated file index
        /// </summary>
        public readonly int droneFileIndex;
        /// <summary>
        /// Latitude of the drone at the time that picture is taken
        /// </summary>
        public readonly double lat;
        /// <summary>
        /// Longitude of the drone at the time that picture is taken
        /// </summary>
        public readonly double lon;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="droneFileIndex">File index of newly generated file by drone</param>
        /// <param name="lat">Latitude of the drone at the time that picture is taken</param>
        /// <param name="lon">Longitude of the drone at the time that picture is taken</param>
        public DroneGenMetaData(int droneFileIndex ,double lat, double lon)
        {
            this.droneFileIndex = droneFileIndex;
            this.lat = lat;
            this.lon = lon;
        }
    }
}
