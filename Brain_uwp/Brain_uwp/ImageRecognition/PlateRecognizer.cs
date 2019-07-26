    using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.IO;
using Windows.Data.Json;
using System.Collections.Specialized;
using Brain_uwp.Data;
using Brain_uwp.Utils;

namespace Brain_uwp.ImageRecognition
{
	/// <summary>
    /// This class is responsible for sending images to a NodeJS server, and reading the outputs
    /// </summary>
    class PlateRecognizer
	{
        /// <summary>
        /// Default Callback for detected plates
        /// </summary>
        /// <param name="openAlprData"> Result as OpenALPR data <see cref="OpenAlprData"/> </param>
        /// <param name="bitmap"> The image that is used for the detection </param>
        /// <param name="err"> <c> if(err == 0) { /*good*/ } else {/*bad*/} </c> </param>
		public delegate void DetectedPlatesHandler(OpenAlprData openAlprData, SoftwareBitmap bitmap, int err);

        /// <summary>
        /// Callback for detected plate in a form of <see cref="DetectedLpData"/>
        /// </summary>
        /// <param name="detectedLpData"> Result of license plate detection from server </param>
        /// <param name="err"><c> if(err == 0) { /*good*/ } else {/*bad*/} </c> </param>
        public delegate void DetectedPlateHandlerGeoCoord(DetectedLpData detectedLpData, int err);
	
        /// <summary>
        /// Count of total inputs that are proceed.
        /// </summary>
		private int totalProcessed = 0;

        /// <summary>
        /// Constructor automatically starts the automatic file remover so that programs does not uses too much space
        /// </summary>
		public PlateRecognizer()
		{
			Task fileRemover = new Task(RemoveCycle);
			fileRemover.Start();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns> Returns count of total inputs that are proceed by now </returns>
		public int GetTotalProcessed()
		{
			return totalProcessed;
		}

        /// <summary>
        /// Automatically removes all the pictures that is in the test_pics_out folder and older then 2 minutes, because this space is use to save images, 
        /// so that FormUpload could actually read these images from test_pics_out folder and send them to the NodeJS server
        /// </summary>
		private async void RemoveCycle()
		{
			while (true)
			{
				StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(@"test_pics_out\");
				var files = await storageFolder.GetFilesAsync();
				foreach (var file in files)
				{
					if(file.DateCreated < DateTimeOffset.Now.AddSeconds(-120))
					{
						Debug.WriteLine("Cleaning " + file.Name);
						await file.DeleteAsync();
					}
				}
				Thread.Sleep(60000);
			}
		}

        /// <summary>
        /// Saves the given SoftwareBitmap to a file
        /// </summary>
        /// <param name="softwareBitmap"> The image data that going to be saved </param>
        /// <param name="outputFile"> Place to store </param>
		private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
		{
			using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
			{
				// Create an encoder with the desired format
				BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

				// Set the software bitmap
				encoder.SetSoftwareBitmap(softwareBitmap);
				encoder.IsThumbnailGenerated = false;

				try
				{
					await encoder.FlushAsync();
				}
				catch (Exception err)
				{
					const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
					switch (err.HResult)
					{
						case WINCODEC_ERR_UNSUPPORTEDOPERATION:
							// If the encoder does not support writing a thumbnail, then try again
							// but disable thumbnail generation.
							encoder.IsThumbnailGenerated = false;
							break;
						default:
							throw;
					}
				}
			}
		}

        /// <summary>
        /// Sends the given storage file with the LPImageData combined to the NodeJS server
        /// </summary>
        /// <param name="storageFile"> Image file that possibly contains the license plate of a car  </param>
        /// <param name="pImageData"> This contains the location the image, lat and lon, lpImage is not used  </param>
        /// <returns>OpenAlprData that contains the detected plate from an image <see cref="OpenAlprData"/></returns>
        private OpenAlprData UseOpenAlpr(StorageFile storageFile, LPImageData pImageData)
        {
  
            string CT = "file";
            string fullPath = storageFile.Path;
            FormUpload.FileParameter f = null;
            for (int i = 1; i < 10; i++)
            {
                try
                {   /*Try to read from a file until you succeed*/
                    f = new FormUpload.FileParameter(File.ReadAllBytes(fullPath), storageFile.Name, "multipart/form-data");
                    break;
                }
                catch (IOException e)
                {
                    Thread.Sleep(1000 * i);
                    Debug.WriteLine("File Load Exception OPENALPR " + i + " : " + e.ToString());
                }
            }

            /*Build the post request*/
            Dictionary<string, object> d = new Dictionary<string, object>();
            d.Add(CT, f);
            d.Add("detected_lat", pImageData.lat);
            d.Add("detected_lon", pImageData.lon);

            string ua = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"; /* I Don't know what this does*/
            var wr = FormUpload.MultipartFormDataPost("http://localhost:8081/file_upload_alpr", ua, d);


            WebResponse wresp = null;
            string result = "";
            try
            {
                Stream stream2 = wr.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                result = reader2.ReadToEnd();
                Debug.WriteLine(string.Format("File uploaded {0}, server response is: {1}", "http://localhost:8081/file_upload_alpr", result));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error uploading file", ex);
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }
            /*Build the response as a OpenAlprData*/
            JsonObject jsonObject;
            OpenAlprData openAlprData = new OpenAlprData();
            if (JsonObject.TryParse(result, out jsonObject))
            {
                openAlprData.Parse(jsonObject);
            }
            return openAlprData;
        }

        /// <summary>
        /// Send the LPImageData to the NodeJS and then processes it. Which is just taking the most confident plate that is been detected by the OpenALPR
        /// </summary>
        /// <param name="lpImageData"> Location and the image data </param>
        /// <param name="detectedPlateHandlerGeoCoord"> Callback of the most confident plate with the location and the image itself <see cref="DetectedPlateHandlerGeoCoord"/> </param>
        /// <returns><see cref="Task"/></returns>
        public async Task DetectAndProcess(LPImageData lpImageData, DetectedPlateHandlerGeoCoord detectedPlateHandlerGeoCoord)
		{	
			await Detect(lpImageData, (OpenAlprData openAlprData, SoftwareBitmap bitmap, int err) =>
			{
				string final_plate = openAlprData.GetMostConfidentPlate().First;
				detectedPlateHandlerGeoCoord(new DetectedLpData(final_plate , lpImageData.lat , lpImageData.lon , bitmap), err);
			});
		}

        /// <summary>
        /// Send the LPImageData to the NodeJS and the return the resulting OpenALPR data with callback
        /// </summary>
        /// <param name="pImageData">Location and the image data</param>
        /// <param name="detectedPlatesCB"> <see cref="DetectedPlatesHandler"/> </param>
        /// <returns><see cref="Task"/></returns>
		public async Task Detect(LPImageData pImageData, DetectedPlatesHandler detectedPlatesCB)
		{
			StorageFolder outputFolder = ApplicationData.Current.LocalFolder;
			int r = Interlocked.Increment(ref totalProcessed);
            DateTime dt = DateTime.UtcNow;
            string id = dt.ToString("MM/dd/yyyy hh:mm:ss.fff tt"); /*create unique file name based on the current date and the count*/
            id = id.Replace(" ", "");
            id = id.Replace("/", "");
            id = id.Replace(":", "");
            id = id.Replace(".", "");
            id += r;

            StorageFile outputFile = await outputFolder.CreateFileAsync(@"test_pics_out\" + id + ".jpg", CreationCollisionOption.ReplaceExisting);

			SaveSoftwareBitmapToFile(pImageData.lpImage, outputFile);

			var openALPR = new Task<OpenAlprData>(() => UseOpenAlpr(outputFile, pImageData));
			openALPR.Start();
			openALPR.Wait();

			var openALPR_result = openALPR.Result;
			
			detectedPlatesCB(openALPR_result, pImageData.lpImage, (openALPR_result != null) ? 0 : 1);
		}
	}
}
