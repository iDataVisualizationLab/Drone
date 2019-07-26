using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Brain_uwp.Data
{
    /// <summary>
    /// This class holds information about picture that is taken by the drone
    /// </summary>
	public struct LPImageData
	{
        /// <summary>
        /// The picture
        /// </summary>
		public SoftwareBitmap lpImage;
        /// <summary>
        /// Latitude of the drone at the time that picture is taken
        /// </summary>
        public double lat;
        /// <summary>
        /// Longitude of the drone at the time that picture is taken
        /// </summary>
        public double lon;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="softwareBitmap"> The picture </param>
        /// <param name="lat"> Latitude of the drone at the time that picture is taken </param>
        /// <param name="lon">  Longitude of the drone at the time that picture is taken </param>
		public LPImageData(SoftwareBitmap softwareBitmap , double lat , double lon)
		{
			this.lpImage = softwareBitmap;
			this.lat = lat;
			this.lon = lon;
		}
	}
}
