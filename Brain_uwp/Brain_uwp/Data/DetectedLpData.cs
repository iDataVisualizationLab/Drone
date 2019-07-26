using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Brain_uwp.Data
{
    /// <summary>
    /// This structure holds the data about detected license plate data.
    /// </summary>
	public struct DetectedLpData
	{
        /// <summary>
        /// Possible license plate
        /// </summary>
		public string lp;
        /// <summary>
        /// Latitude of the drone at the time that picture is taken
        /// </summary>
        public double lat;
        /// <summary>
        /// Longitude of the drone at the time that picture is taken
        /// </summary>
        public double lon;
        /// <summary>
        /// The Image
        /// </summary>
        public SoftwareBitmap lpImage;

        /// <summary>
        /// Con struts DetectedLpData
        /// </summary>
        /// <param name="lp">Possible license plate</param>
        /// <param name="lat">Latitude of the drone at the time that picture is taken</param>
        /// <param name="lon">Longitude of the drone at the time that picture is taken</param>
        /// <param name="bitmap">The Image</param>
		public DetectedLpData(string lp , double lat , double lon , SoftwareBitmap bitmap)
		{
			this.lpImage = bitmap;
			this.lat = lat;
			this.lon = lon;
			this.lp = lp;
		}
	}
}
