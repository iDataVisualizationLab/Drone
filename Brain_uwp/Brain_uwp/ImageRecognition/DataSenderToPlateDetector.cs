using System;
using System.Collections.Generic;
using Windows.Storage;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Windows.Graphics.Imaging;

using Windows.Storage.Streams;

namespace Brain_uwp.ImageRecognition
{
    /// <summary>
    /// Takes the data from images_from_drone folder, and sends them to listeners,  main purpose of this class is just to collect files, invoke listeners, and removing the data after begin distributed by invoking
    /// </summary>
    class DataSenderToPlateDetector
    {
        /// <summary>
        /// MUTEX is = 1 when This class is doing stuff in the images_from_drone folder,
        /// and MUTEX is = 0 when This class does not doing stuff in the images_from_drone folder
        /// DataSenderToPlateDetector is continuously doing stuff in the images_from_drone folder, so when you want to accesses it there is high possible change of getting access denied exceptions
        /// so it you want to do stuff in images_from_drone folder, check the mutex with atomic operations to see if its 0
        /// DO NOT CHANGE THIS OUT SIDE OF THIS CLASS
        /// </summary>
        public static int MUTEX = 0; /*TODO: THIS HERE IS BEING PUBLIC IS NOT GOOD FIND SOME OTHER WAY TO ACCOMPLISH SAME BEHAVIOUR*/

        /// <summary>
        /// Callback that each listener will get after subscribing
        /// </summary>
        /// <param name="softwareBitmap"> The Image, possibly contains the license plate </param>
        /// <param name="drone_lat"> Latitude of the drone at the time of picture taken </param>
        /// <param name="drone_long"> Longitude of the drone at the time of picture taken</param>
        /// <param name="err"><c> if(err == 0) { /*good*/ } else {/*bad*/} </c></param>
        public delegate void DroneProducedDataHandler(SoftwareBitmap softwareBitmap, double drone_lat, double drone_long, int err);

        /// <summary>
        /// Listeners
        /// </summary>
        private List<DroneProducedDataHandler> droneProducedDataHandlers;

        /// <summary>
        /// Constructing the DataSenderToPlateDetector will automatically starts the Data sending processes, however no call will be made until, there is data to send and at least one listener
        /// </summary>
        public DataSenderToPlateDetector()
        {
            droneProducedDataHandlers = new List<DroneProducedDataHandler>();
            Task t = new Task(TaskForwardData);
            t.Start();
        }

        /// <summary>
        /// Subscribe to this to get notified when there is data in images_from_drone to be processed
        /// </summary>
        /// <param name="producedDataHandler"></param>
        public void SubscribeDataProduced(DroneProducedDataHandler producedDataHandler) => droneProducedDataHandlers.Add(producedDataHandler);

        /// <summary>
        /// Check if there is file in images_from_drone folder
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <returns> true if file exist </returns>
        private async Task<bool> isFilePresent(string fileName)
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(@"images_from_drone\" + fileName);
            return item != null;
        }

        /// <summary>
        /// Forwards data in the images_from_drone to the listeners, deletes the files that are forwarded
        /// </summary>
        private async void TaskForwardData()
        {
            StorageFolder inputFolder;
            IReadOnlyList<StorageFile> inputFiles;
            while (true)
            {
                do
                {
                    Interlocked.Increment(ref MUTEX); /*Lock mutex so, program knows no to do stuff in this folder while its being processed*/
                    inputFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(@"images_from_drone\"); 
                    inputFiles = await inputFolder.GetFilesAsync(); /*Get all the files that are currently in this folder*/
                    Interlocked.Decrement(ref MUTEX); /*Unlock */
                    Thread.Sleep(2000); /*Do this 1 time at each 2 seconds, until condition bellow are satisfied*/
                } while (inputFiles.Count == 0 || droneProducedDataHandlers.Count == 0);

                //Forward each file to the handlers
                foreach (var file in inputFiles)
                {
                    if (file.Path.EndsWith(".jpg")) /*Remember there are also .txt files with same name e.g 1.jpg which is the image, 1.txt which is the loctation*/
                    {
                        SoftwareBitmap softwareBitmap = null;
                        /*The code bellow will throw exceptions, so waiting a second and trying again seems to solve the problem*/
                        for (int k = 1; k <= 5; k++) 
                        {

                            try
                            {
                                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read, StorageOpenOptions.AllowReadersAndWriters))
                                {
                                    // Create the decoder from the stream
                                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                                
                                    // Get the SoftwareBitmap representation of the file
                                    for (int j = 1; j <= 5; j++)
                                    {
                                        try
                                        {
                            
                                            softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                                            break;
                                        }
                                        catch (OutOfMemoryException e)
                                        {
                                            Debug.WriteLine("HAHA " + e.ToString());
                                            Thread.Sleep(1000 * j);
                                        }
                                    }
                                    break;
                                }
                            
                            }
                            catch (UnauthorizedAccessException e)
                            {
                                Debug.WriteLine("HEHE " +e.ToString());
                                Thread.Sleep(1000 * k);
                            }
                        }

                        string txtfile = file.Name.Substring(0, file.Name.LastIndexOf('.')) + ".txt"; /*extract the name of the file and put .txt at the end*/
                        /*Get the related text file if it does not exist to nothing since location information is important*/
                        if (await isFilePresent(txtfile))
                        {
                            StorageFile location = await inputFolder.GetFileAsync(txtfile);

                            string text = await FileIO.ReadTextAsync(location);
                            var locations = text.ToLower().Trim().Split(",");
                            double lat = double.Parse(locations[0]);
                            double lon = double.Parse(locations[1]);

                            foreach (var handle in droneProducedDataHandlers)
                            {
                                //if everything goes well invoke the listeners with the data
                                handle(softwareBitmap, lat, lon, (softwareBitmap != null) ? 0 : 1);
                            }
                        }
                    }
                }

                //remove all the processed data
                foreach (var file in inputFiles)
                {
                    for(var i = 1; i < 10; i++)
                    {
                        try
                        {
                            await file.DeleteAsync();
                            break;

                        }catch(Exception e)
                        {
                            Debug.WriteLine("KEKE " + e.ToString());
                            Thread.Sleep(1000 * i);
                        }
                    }
                }

                Thread.Sleep(250); /*wait a little while before staring over*/
            }
        }
    }
}
