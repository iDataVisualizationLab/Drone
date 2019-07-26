using System;
using Windows.UI.Xaml.Controls;
using DJI.WindowsSDK;
using Brain_uwp.ImageRecognition;
using Windows.Graphics.Imaging;
using System.Diagnostics;
using Brain_uwp.Data;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Threading;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.IO;
using Windows.System;
using System.Net.Sockets;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Popups;
using DJIVideoParser;
using DJIWindowsSDKSample.Playback;
using Windows.UI.Core;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Brain_uwp
{
    public sealed partial class MainPage : Page
    {
        #region MEDIADOWNLOAD
        /// <summary>
        /// Count of total images that received by the drone
        /// </summary>
        private static int receivedImageCount = 0;

        /// <summary>
        /// Used for downloading images and videos from Mavic2
        /// </summary>
        private MediaTaskManager mediaTaskManager;

        /// <summary>
        /// Keeps the data about the file is being downloaded 
        /// </summary>
        private TaskModel taskModel = new TaskModel(); /*TODO: This is not needed, remove it maybe ?*/

        #endregion

        #region LIVECAMERAFEEDTCP

        /// <summary>
        /// This keeps tracks of the time so, live feed of the video could send in x intervals, currently is 0.3
        /// </summary>
        private Stopwatch stopwatch;

        /// <summary>
        /// Keeps the last time that it send the live video feed
        /// </summary>
        private double lastLiveFeed = 0;

        #endregion

        #region DETECTION

        /// <summary>
        /// This gets the data in images_from_drone folder and sends the data to PlateDetector via callbacks
        /// </summary>
        private DataSenderToPlateDetector dataSender = new DataSenderToPlateDetector();

        /// <summary>
        /// This takes the data coming from dataSender via callbacks and sends them to the NodeJS server for detection and gets the result of the detection from server 
        /// </summary>
        private PlateRecognizer plateRecognizer = new PlateRecognizer(); 

        #endregion

        #region MAP

        private MapIcon aircraftMapIcon = null;
        private MapElementsLayer routeLayer = new MapElementsLayer();
        private MapElementsLayer waypointLayer = new MapElementsLayer();
        private MapElementsLayer locationLayer = new MapElementsLayer();

        #endregion

        #region DJI

        /// <summary>
        /// This parses the live video feed from camera
        /// </summary>
        private DJIVideoParser.Parser videoParser;

        #endregion


        #region AIRCRAFT

        /// <summary>
        /// Keeps track of whether the aircraft is currently connected or not
        /// </summary>
        private static bool aircraftConnected = false;

        /// <summary>
        /// Keep tracks of current location of aircraft
        /// </summary>
        private LocationCoordinate2D aircraftPosition = new LocationCoordinate2D();

        /// <summary>
        /// Keep tracks of current altitude of aircraft
        /// </summary>
        private double aircraftAltidue = 0;

        #endregion

        #region LOCATIONTRANSMISSION
       
        /// <summary>
        /// This is the socket that transmit live loction of the drone to the NodeJS server via port 8081
        /// </summary>
        private MessageWebSocket locationTransmitSocket;

        /// <summary>
        /// Address that will be connected to, to transmit local position 
        /// </summary>
        private Uri uri = new Uri("ws://localhost:8081/socket.io/?EIO=2&transport=websocket");
        /// <summary>
        /// Use to write message to the socket stream.
        /// </summary>
        private DataWriter locationTransmitMesseageWriter;

        #endregion

        /// <summary>
        /// Main page of the application
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationStateChangedAsync;

            ToggleSimButt.Click += ToggleSimButt_Click;
            CenterMapButt.Click += CenterMapButt_Click;
            LoadMissButt.Click += LoadMissButt_Click;
            StartMissButt.Click += StartMissButt_Click;
            TakeoffButt.Click += TakeoffButt_Click;
            LandButt.Click += LandButt_Click;
            GroundStationButt.Click += GroundStationButt_Click;

            var y = MemoryManager.AppMemoryUsageLimit; /*TODO: Maybe remove this?*/
            bool result = MemoryManager.TrySetAppMemoryUsageLimit(y + 10000); /*TODO: Maybe remove this?*/

            Task socketTransmitter = new Task(SocketTransmitPosition); /*This continuously transmits the live location of the drone to the NodeJS server*/
            socketTransmitter.Start();

            Task showAircraftOnTheMap = new Task(ShowAircraftOnTheMap); /*This continuously updates the Aircraft icon on the map*/
            showAircraftOnTheMap.Start();

            dataSender.SubscribeDataProduced(OnDataProduced); /*When there is data on the images_from_drone folder this callback sends those images to the OnDataProduced and they get Enqueued in the InputPool*/

            Task takeDataFromInputPoolAndRunDetect = new Task(TakeDataFromInputPoolAndRunDetect); /* This continuously dequeues data from the pool and run detect on them*/
            takeDataFromInputPoolAndRunDetect.Start();

            Task downloadDataUsingMetaDataFromDroneGenDataPool = new Task(DownloadDataUsingMetaDataFromDroneGenDataPool); /*This gets the data Enqueued by MainPage_NewlyGeneratedMediaFileChanged and download them*/
            downloadDataUsingMetaDataFromDroneGenDataPool.Start();

            //Task getDetectedPlatesFromPoolAndShowOnUI = new Task(GetDetectedPlatesFromPoolAndPutItIntoListView);
            //getDetectedPlatesFromPoolAndShowOnUI.Start();

            WaypointMap.Layers.Add(routeLayer);
            WaypointMap.Layers.Add(waypointLayer);
            WaypointMap.Layers.Add(locationLayer);
            WaypointMap.MapElementClick += WaypointMap_MapElementClick;

            DJISDKManager.Instance.RegisterApp("3f5a2dc7bcd532812ad05a33"); /*This key wont work if the package name is changed check out the link bellow*/
            /* https://developer.dji.com/windows-sdk/documentation/application-development-workflow/workflow-register.html */
        }

        #region BUTTON_CLICKS
        /// <summary>
        /// Works when GroundStationButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GroundStationButt_Click(object sender, RoutedEventArgs e)
        {
            SDKError err = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).SetGroundStationModeEnabledAsync(new BoolMsg() { value = true });
            var messageDialog = new MessageDialog(String.Format("Set GroundStationMode Enabled: {0}", err.ToString()));
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// Works when LandButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LandButt_Click(object sender, RoutedEventArgs e)
        {
            await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartAutoLandingAsync();
        }

        /// <summary>
        /// Works when TakeoffButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TakeoffButt_Click(object sender, RoutedEventArgs e)
        {
            await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartTakeoffAsync();
        }

        /// <summary>
        /// Works when StartMissButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartMissButt_Click(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetCurrentState() == WaypointMissionState.READY_TO_EXECUTE)
            {
                DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).StartMission();
            }
        }

        /// <summary>
        /// Works when LoadMissButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadMissButt_Click(object sender, RoutedEventArgs e)
        {
            WaypointMission mission = await GetMission(ParkingLotMissionCB.SelectedItem.ToString()); /*Mission is a basically list of way point*/

            SDKError errLoad = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).LoadMission(mission); /*Loading means load them into the DJISDK*/
            var messageDialog = new MessageDialog(String.Format("Load Mission: {0}", errLoad.ToString()));
            await messageDialog.ShowAsync();

            SDKError errUpload = await DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).UploadMission(); /*This is the part where way point instruction gets loaded into Mavic2*/
            var messageDialog2 = new MessageDialog(String.Format("Upload mission: {0}", errUpload.ToString()));
            await messageDialog2.ShowAsync();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
            {
                WpMissResTB.Text = "Result: " + errUpload.ToString();
            });
            RedrawWaypoint();
        }

        /// <summary>
        /// Works when  CenterMapButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CenterMapButt_Click(object sender, RoutedEventArgs e)
        {
            var aircraftLocaton = (await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync()).value.Value;
            WaypointMap.Center = new Geopoint((new BasicGeoposition() { Latitude = aircraftLocaton.latitude, Longitude = aircraftLocaton.longitude }));
        }

        /// <summary>
        /// Works when WaypointMap_MapElement is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaypointMap_MapElementClick(MapControl sender, MapElementClickEventArgs args)
        {
            Debug.WriteLine(args.Location.Position.Latitude + " , " + args.Location.Position.Longitude);
        }

        /// <summary>
        /// Works when ToggleSimButt is click on the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToggleSimButt_Click(object sender, RoutedEventArgs e)
        {
            var productType = (await DJISDKManager.Instance.ComponentManager.GetProductHandler(0).GetProductTypeAsync()).value;
            if (productType != null && productType?.value != ProductType.UNRECOGNIZED)
            {
                /*To start simulator, it needs SimulatorInitializationSettings which has staring location and satellite count*/
                SimulatorInitializationSettings settings = new SimulatorInitializationSettings();
                if (ToggleSimButt.IsChecked == true)
                {
                    var gpssingal = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetGPSSignalLevelAsync();
                    var loc = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync();

                    if (gpssingal.value.HasValue && loc.value.HasValue && gpssingal.value.Value.value >= FCGPSSignalLevel.LEVEL_3)
                    {
                        /*If Mavic and RC has good signal and If location has a value, start the simulator with the drones current location*/
                        settings.latitude = loc.value.Value.latitude;
                        settings.longitude = loc.value.Value.longitude;
                        settings.satelliteCount = 8; //TODO: CHANGE THIS ACCORDING TO SIGNAL LEVEL

                    }
                    else
                    {
                        /*If Mavic and RC does not have a good signal or cant get its real location start the simulator at predefined location*/
                        settings.latitude = 33.588733; /*Location of TTU*/
                        settings.longitude = -101.875494;
                        settings.satelliteCount = 8; //TODO: CHANGE THIS ACCORDING TO SIGNAL LEVEL
                    }

                    var startRes = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartSimulatorAsync(settings);
                    if (startRes == SDKError.NO_ERROR)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
                        {
                            SimParamTB.Text = "SimParam: " + settings.latitude + "," + settings.longitude + "," + settings.satelliteCount;
                        });
                    }
                    else
                    {
                        Debug.Fail("TODO fIX HERE");
                    }
                }
                else
                {
                    var stopRes = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StopSimulatorAsync();
                    if (stopRes == SDKError.NO_ERROR)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
                        {
                            SimParamTB.Text = "SimParam: ";
                        });
                    }
                    else
                    {
                        Debug.Fail("TODO fIX HERE");
                    }
                }
            }
            else
            {
                Debug.Fail("TODO fIX HERE");
            }
        }

        /// <summary>
        /// Works when ListViewItem is click on the main page
        /// NOTE: This element is not current used on the main page, but it used to show images of the detected plates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ListViewItem_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                ListViewItem item = ((ListViewItem)sender);
                var detectedLpData = (DetectedLpData)item.DataContext;
                SoftwareBitmap bitmap = detectedLpData.lpImage;
                string result = detectedLpData.lp;

                if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || bitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(bitmap);

                //image.Source = source;
                //textblock.Text = result;
            });
        }

        #endregion

        #region ROUTINES
        /// <summary>
        /// This routine downloads the data which is created by the drone, which is then saved into images_from_drone folder
        /// </summary>
        private async void DownloadDataUsingMetaDataFromDroneGenDataPool()
        {
            /*TODO: THIS LOGIC SEEMS OFF TRY TO TEST AGAIN*/
            while (true)
            {
                if (DroneGenDataPool.Instance.HasNext())
                {
                    Debug.Write("DATA DOWNLOADING aircraft is " + aircraftConnected );
                    /*This gets the meta data about the picture that is taken by the drone and tries to download that picture */
                    /*picture is save to a temp folder while its begin download and then moved to the images_from_drone, so thats why there is mutex*/
                    while (Interlocked.Exchange(ref DataSenderToPlateDetector.MUTEX, DataSenderToPlateDetector.MUTEX) != 0 /*Make sure that you don't try to access images_from_drone folder while its begin used by DataSenderToPlateDetector*/
                        || !aircraftConnected)
                    {
                        /*This part sleeps until one the conditions above is not satisfied*/
                        Debug.WriteLine("WAITING FOR SUTFF aircraft is " + aircraftConnected);
                        Thread.Sleep(500);
                    }
                    await DonwloadImageDataAndSaveCoordinatesAsWell(DroneGenDataPool.Instance.GetNext()/*Meta data about the image*/);
                }
            }
        }

        /// <summary>
        /// This routine paints the location of aircraft on the map
        /// </summary>
        private async void ShowAircraftOnTheMap()
        {
            while (true)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DroneGenPoolTB.Text = "Drone Gen Pool Count: " + DroneGenDataPool.Instance.GetCount();
                    InputPoolTB.Text = "Input Pool Count: " + InputPool.Instance.GetCount();
                    ResultPoolTB.Text = "Result Pool Count: " + ResultPool.Instance.GetCount();

                    if (aircraftMapIcon == null)
                    {
                        aircraftMapIcon = new MapIcon()
                        {
                            NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 0.5),
                            ZIndex = 1,
                            Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/phantom.svg")),
                        };
                        locationLayer.MapElements.Add(aircraftMapIcon);
                    }
                    aircraftMapIcon.Location = new Geopoint(new BasicGeoposition() { Latitude = aircraftPosition.latitude, Longitude = aircraftPosition.longitude, Altitude = aircraftAltidue });
                });
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// This routine transmits the position of the aircraft to the server
        /// </summary>
        private async void SocketTransmitPosition()
        {
            locationTransmitSocket = new MessageWebSocket();
            locationTransmitSocket.Control.MessageType = SocketMessageType.Utf8;
            await locationTransmitSocket.ConnectAsync(uri);
            locationTransmitMesseageWriter = new DataWriter(locationTransmitSocket.OutputStream);

            while (true)
            {
                /*There is two example ways to send data to the server this is one of them */
                /*In the server side you can get this data with _socket_.on('drone_loc', (location)=>{....}) event by using socket.io*/
                string message = "42[\"drone_loc\"," +
                                 "{\"lat\": \"" + aircraftPosition.latitude + "\", " +
                                 "\"lon\": \"" + aircraftPosition.longitude + "\"}]";
                locationTransmitMesseageWriter.WriteString(message);
                await locationTransmitMesseageWriter.StoreAsync();

                Thread.Sleep(200); /*send data each 200 milliseconds*/
            }
        }

        /// <summary>
        /// This routine takes the images from input pool run detects by sending the images to the main server and returns the result as callback
        /// </summary>
        private async void TakeDataFromInputPoolAndRunDetect()
        {
            while (true)
            {
                if (InputPool.Instance.HasNext())
                {
                    await plateRecognizer.DetectAndProcess(InputPool.Instance.GetNext(), (DetectedLpData detectedLpData, int err) =>
                    {
                        if (err == 0)
                        {
                            /*In here you can get the data about detected plate, for now its not used and image data is disposed immediately*/
                            detectedLpData.lpImage.Dispose();
                            /*if you want to something with the data its better store in a result pool and get the data from there*/
                            //ResultPool.Instance.Enqueue(detectedLpData); 
                        }
                    });
                }
            }
        }

        /// <summary>
        /// This old routine used to show results from server by showing in the list view it is not used now.
        /// </summary>
        private async void GetDetectedPlatesFromPoolAndPutItIntoListView()
        {
            while (true)
            {
                /*This part is used to dequeue data from the result pool and show it on the main UI it is not used now*/

                //if (ResultPool.Instance.HasNext())
                //{
                //    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //       {
                //           queCount.Text = InputPool.Instance.GetCount() + " inputs are in the input pool";
                //           resultPoolCount.Text = ResultPool.Instance.GetCount() + " result are in the result pool";
                //           totalProcessed.Text = +plateRecognizer.GetTotalProcessed() + " inputs are processed";
                //           totalNotFound.Text = "Not Found: " + total_not_found;
                //           if (!pauseOutput.IsOn)
                //           {
                //               if (ResultPool.Instance.HasNext())
                //               {
                //                   var result = ResultPool.Instance.GetNext();
                //                   var listViewItem = new ListViewItem();
                //                   listViewItem.DoubleTapped += ListViewItem_DoubleTapped; /*show the image when double tapped*/
                //                   listViewItem.DataContext = result;
                //                   listViewItem.FontSize = 26;
                //                   if (result.lp.Equals(""))
                //                   {
                //                       Interlocked.Increment(ref total_not_found);
                //                       listViewItem.Content = "--fail/not-found--";
                //                   }
                //                   else
                //                   {
                //                       listViewItem.Content = result.lp;
                //                   }
                //    
                //                   if (resultsListView.Items.Count > 8)
                //                   {
                //                       var lv = (ListViewItem)resultsListView.Items[0];
                //                       var lvpair = (DetectedLpData)lv.DataContext;
                //                       lvpair.lpImage.Dispose();
                //                       lvpair.lp = "";
                //                       if (resultsListView.SelectedIndex == 0)
                //                       {
                //                           ((SoftwareBitmapSource)image.Source).Dispose();
                //                           image.Source = null;
                //                       }
                //                       resultsListView.Items.RemoveAt(0);
                //                   }
                //                   resultsListView.Items.Add(listViewItem);
                //               }
                //           }
                //       });
                //    Thread.Sleep(500);
                //}

            }
        }

        #endregion

        #region METHODS

        /// <summary>
        /// A Wrapper method to get mission by ID
        /// </summary>
        /// <param name="missionID"> mission id </param>
        /// <returns><see cref="WaypointMission"/></returns>
        private async Task<WaypointMission> GetMission(string missionID)
        {
            /*A utilty method the get mission by id, each id respresent a diffrent parking lot mission, some missions may need current location of the drone*/
            var loc = (await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync()).value.Value;
            WaypointMission mission = new WaypointMission();
            if (missionID.Equals("Test"))
            {
                mission = ParkingLotMissions.ParkingLotMissions.GetTestMission(loc);
            }
            else if (missionID.Equals("R25"))
            {
                mission = ParkingLotMissions.ParkingLotMissions.GetR25Mission();
            }
            /* To get new mission
            else if (missionID.Equals("_missionID"))
            {
                mission = ParkingLotMissions.ParkingLotMissions.Get_missionIDMission();
            } 
             */
            else if (missionID.Equals("R16"))
            {
                mission = ParkingLotMissions.ParkingLotMissions.GetR16Mission();
            }
            else if (missionID.Equals("R17"))
            {
                mission = ParkingLotMissions.ParkingLotMissions.GetR17Mission();
            }
            else if (missionID.Equals("R17Test"))
            {
                mission = ParkingLotMissions.ParkingLotMissions.GetR17TestMission();
            }
            else
            {
                Debug.Fail("TODO FIX HERE");
            }

            return mission;
        }

        /// <summary>
        /// This method downloads the image data given by, in the form of <see cref="DroneGenMetaData"/> and starts to download, downloading file will be saved
        /// in temporary folder, after download is finish they will be moved to the images_from_drone folder
        /// </summary>
        /// <param name="droneGen"></param>
        /// <returns></returns>
        private async Task DonwloadImageDataAndSaveCoordinatesAsWell(DroneGenMetaData droneGen)
        {
            if (!aircraftConnected) return;
            Debug.WriteLine("DOWNLAOD STARTED " + droneGen.droneFileIndex);
           
            int count = Interlocked.Increment(ref receivedImageCount); /*This ID will be used to identify the picture and the location information after its downloaded*/
        
            StorageFolder storageFolderTemp = ApplicationData.Current.TemporaryFolder; /*Picture will be stored on the temp folder while its being downloaded*/
            for (int i = 1; i < 10; i++)
            {
                /*Try to create file and write to it, this part could throw exception, so it tries to create and if it fails, sleeps 1 + i seconds and tries again*/
                try
                {
                    /*The location of the drone is considered location of the car, this needed in order to know where the car is, location information is saved in to txt file right after picture is taken*/
                    /*the txt file about location is created immediately but it takes time to download the related picture*/
                    /*so 1.jpg is the image that is detect by the drone*/
                    /*and 1.txt is the location of the drone at that time that it took the picture which gives approximate location of the car*/
                    Debug.WriteLine("HERE FOR LOOOP");
                    StorageFile droneLocationFile = await storageFolderTemp.CreateFileAsync(count + ".txt", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(droneLocationFile, droneGen.lat + "," + droneGen.lon);
                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("HEHE EXEPTION");
                    Thread.Sleep(1000 * i);
                }
            }

            /*I took this piece of code bellow from DJISDKSamples.Playback  so look there for unchanged version*/

            var request = new MediaFileDownloadRequest
            {
                index = droneGen.droneFileIndex, /*This is the actual file index of the picture that is taken by drone*/
                count = 1,
                dataSize = -1,
                offSet = 0,
                segSubIndex = 0,
                subIndex = 0,
                type = MediaRequestType.SCREEN /*This is the smallest version of the picture but its enough to detect plates*/
            };


            var task = MediaTask.FromRequest(request);
            var tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(count + ".jpg", CreationCollisionOption.ReplaceExisting); /*Picture is saved onto temp folder while its being downladed*/
            var stream = await tempFolder.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            var outputStream = stream.GetOutputStreamAt(0);
            var fileWriter = new DataWriter(outputStream);
            /*TODO: Logic seems off here as well so test here again, but it works*/
            task.OnDataReqResponse += async (sender, req, data, speed) => /*This part called after downloading started*/
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    taskModel.cachedByte += data.Length;
                    taskModel.MBSpeed = (speed / 8388608 /*I don't know that this number means*/);
                    fileWriter.WriteBytes(data);
                    await fileWriter.StoreAsync();
                    await outputStream.FlushAsync();

                    /*After download is finished*/
                    var folder = ApplicationData.Current.TemporaryFolder;

                    var lpPicture = await folder.GetFileAsync(count + ".jpg"); /*Get picture from tmp folder*/
                    var lpLoc = await folder.GetFileAsync(count + ".txt"); /*Get the drone location data from tmp folder*/

                    if (lpPicture == null || lpLoc == null) Debug.Fail("AAAHHHHH");

                    var storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(@"images_from_drone\");
                    await lpPicture.MoveAsync(storageFolder); /*move picture to the images_from_drone folder */
                    await lpLoc.MoveAsync(storageFolder); /*move drone location data to the images_from_drone folder */

                    Debug.WriteLine("DOWNLAOD FINISHED " + droneGen.droneFileIndex);
                });
            };

            mediaTaskManager.PushBack(task); /*This part enqueues the task created above*/
        }

        /// <summary>
        /// Redraws the way point onto map by getting the way point data from <see cref="DJISDKManager.WaypointMissionManager"/>
        /// </summary>
        private async void RedrawWaypoint()
        {
            /*Check DJISDKSample WaypointHandling*/
            List<BasicGeoposition> waypointPositions = new List<BasicGeoposition>();
            var downloadResult = await DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).DownloadMission();

            var result = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetLoadedMission();
            var mission = result.Value;
            waypointLayer.MapElements.Clear();

            for (int i = 0; i < mission.waypoints.Count; ++i)
            {
                if (waypointLayer.MapElements.Count == i)
                {
                    MapIcon waypointIcon = new MapIcon()
                    {
                        Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/waypoint.png")),
                        NormalizedAnchorPoint = new Point(0.5, 0.5),
                        ZIndex = 0,
                    };
                    waypointLayer.MapElements.Add(waypointIcon);
                }

                var geolocation = new BasicGeoposition() { Latitude = mission.waypoints[i].location.latitude, Longitude = mission.waypoints[i].location.longitude };
                (waypointLayer.MapElements[i] as MapIcon).Location = new Geopoint(geolocation);
                waypointPositions.Add(geolocation);
            }
            if (routeLayer.MapElements.Count == 0 && waypointPositions.Count >= 2)
            {
                var polyline = new MapPolyline
                {
                    StrokeColor = Color.FromArgb(255, 0, 255, 0),
                    Path = new Geopath(waypointPositions),
                    StrokeThickness = 2
                };
                routeLayer.MapElements.Add(polyline);
            }
            else
            {
                var waypointPolyline = routeLayer.MapElements[0] as MapPolyline;
                waypointPolyline.Path = new Geopath(waypointPositions);
            }

        }

        /// <summary>
        /// Initializes video parser
        /// </summary>
        /// <returns></returns>
        private async Task InitVideoParser()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                if (videoParser == null)
                {
                    videoParser = new DJIVideoParser.Parser();
                    videoParser.Initialize(delegate (byte[] data)
                    {
                        //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
                        return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
                    });
                    //Set the swapChainPanel to display and set the decoded data callback.
                    videoParser.SetSurfaceAndVideoCallback(0, 0, swapChainPanel, ReceiveDecodedData);
                    DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
                }

                //get the camera type and observe the CameraTypeChanged event.
                DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += OnCameraTypeChanged;
                var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
                OnCameraTypeChanged(this, type.value);
            });
        }

        #endregion

        #region EVENTS

        /// <summary>
        /// Called when there are images in the images_from_drone folder
        /// </summary>
        /// <param name="bitmap">Actual image</param>
        /// <param name="lat">Latitude of the images that is taken</param>
        /// <param name="lon">Longitude of the images that is taken</param>
        /// <param name="err">if err == 0: good,  else: bad</param>
        private void OnDataProduced(SoftwareBitmap bitmap, double lat, double lon, int err)
        {
            if (err == 0)
            {
                InputPool.Instance.Enqueue(new LPImageData(bitmap /*Actual Image that is taken by the drone*/, lat /*latitude of drone when the picture is taken*/, lon));
                //Debug.WriteLine(InputPool.Instance.GetCount());
            }
            else
            {
                Debug.WriteLine("Error with the bitmap");
            }
        }

        /// <summary>
        /// Called when video feeder has new raw data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="bytes"></param>
        private void OnVideoPush(VideoFeed sender, byte[] bytes /*This is the raw bytes from live video feed from drone*/)
        {
            videoParser.PushVideoData(0, 0, bytes, bytes.Length);
        }

        /// <summary>
        /// Called when the new video feeder raw data is decoded
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void ReceiveDecodedData(byte[] data /*Decoded data from live video feed from drone in RGBA format*/, int width, int height)
        {
            //TODO: CREATE BETTER APROACH FOR RESIZING THE DATA AND SENDING
            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            /*Send the live video feed to the nodejs server each 0.4 seconds*/
            if (stopwatch.Elapsed.TotalSeconds < lastLiveFeed + 0.2) return;
            lastLiveFeed = stopwatch.Elapsed.TotalSeconds;

            /*Before sending the data it needs to be resized because original size is too big for raw transmission*/
            byte[] resizedBytes;
            using (var ras = new MemoryStream().AsRandomAccessStream() /*this creates an empty random access stream*/)
            {
                var task1 = BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ras); /*create an encoder, BitmapEncoder can resize the image so thats why its used here*/
                /*All of the code here are mostly async functions, but since ReceiveDecodedData is not async function I cant use await here so I have to manually wait for tasks to complete in these while loops*/
                /*Note: you can declare ReceiveDecodedData as async and use await in each of the functions, but that makes it really slow*/
                /*TODO: Create a pool maybe for the data ???? so I can proceed asyncly out side of this function*/
                while (task1.Status != AsyncStatus.Completed) { }

                var encoder = task1.GetResults();
                encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, (uint)width, (uint)height, 2, 2, data); /*Set the encoder with the acutal pixel data so it can encode the data*/
                encoder.BitmapTransform.ScaledWidth = 300; /*new witdh of the live feed*/
                encoder.BitmapTransform.ScaledHeight = 200; /*new hight of the live feed*/
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;


                var task2 = encoder.FlushAsync(); /*This the encoding part so image will be resized and the data will be in ras*/
                while (task2.Status != AsyncStatus.Completed) { }

                resizedBytes = new byte[ras.Size]; // Initialize the buffer to store image data
                ras.Seek(0); // seek beginning of the stream
                var taskTest = ras.AsStream().ReadAsync(resizedBytes ,0, (int)ras.Size); /*read the stream and copy into resizedBytes*/
                while (taskTest.Status != TaskStatus.RanToCompletion) { }
            }

            TcpClient tcpClient = new TcpClient("127.0.0.1", 1337); /*connect to the local nodejs on port 1337*/

            if (tcpClient.Connected)
            {
                var size = resizedBytes.Length; /*first send the size so nodejs knows when to stop*/
                string header = "size=" + size;
                byte[] readBuff;
                using (var stream = tcpClient.GetStream())
                {
                    var headerBytes = Encoding.ASCII.GetBytes(header);

                    stream.Write(headerBytes, 0, headerBytes.Length); /*Send the header first*/
                    stream.ReadTimeout = 2000;

                    while (tcpClient.Available < 5) { } /*wait until there is enough data to read in the read buffer*/

                    readBuff = new byte[tcpClient.Available];
                    stream.Read(readBuff, 0, tcpClient.Available); /*read the data coming from the server*/

                    string recevied = Encoding.ASCII.GetString(readBuff);
                    var bytesSend = 0;

                    if (recevied.Equals("ready")) /*server is ready to recive*/
                    {
                        do { bytesSend += tcpClient.Client.Send(resizedBytes); } while (bytesSend < size); /*send chunks of pixel byte*/
                    }
                }
            }
        }

        /// <summary>
        /// Called when simulator state is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_IsSimulatorStartedChanged(object sender, BoolMsg? value)
        {
            if (value.HasValue)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
                {
                    SimStatusTB.Text = value.Value.value.ToString();
                });
            }
        }

        /// <summary>
        /// Called when SDK Registration state is changed
        /// </summary>
        /// <param name="state"></param>
        /// <param name="errorCode"></param>
        private async void Instance_SDKRegistrationStateChangedAsync(SDKRegistrationState state, SDKError errorCode)
        {
            if (errorCode == SDKError.NO_ERROR)
            {
                System.Diagnostics.Debug.WriteLine("Register app successfully.");

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AppStateTB.Text = "Register app successfully.";
                });

                /*The product connection state will be updated when it changes here.*/
                DJISDKManager.Instance.ComponentManager.GetProductHandler(0).ProductTypeChanged += MainPage_ProductTypeChanged;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Register SDK failed, the error is: ");
                System.Diagnostics.Debug.WriteLine(errorCode.ToString());
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AppStateTB.Text = "Register SDK failed, the error is: " + errorCode.ToString();
                });

            }
        }

        /// <summary>
        /// Called when product type is changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_ProductTypeChanged(object sender, ProductTypeMsg? value)
        {
            /*Init video parser so that it can show live camera feed from the drone*/
            ///await InitVideoParser();
            if (value != null && value?.value != ProductType.UNRECOGNIZED)
            {
                aircraftConnected = true;
                await InitVideoParser();
                mediaTaskManager = new MediaTaskManager(0, 0);

                /*Register all the events*/
                DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).HomeLocationChanged += MainPage_HomeLocationChanged;
                DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).VelocityChanged += MainPage_VelocityChanged;
                DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AircraftLocationChanged += MainPage_AircraftLocationChanged;
                DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GPSSignalLevelChanged += MainPage_GPSSignalLevelChanged;
                DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).NewlyGeneratedMediaFileChanged += MainPage_NewlyGeneratedMediaFileChanged;
                DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).IsSimulatorStartedChanged += MainPage_IsSimulatorStartedChanged;
                DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AltitudeChanged += MainPage_AltitudeChanged;
                DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).StateChanged += MainPage_WaypointMissionStateChanged;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AircraftStateTB.Text = "The Aircraft is connected now.";
                });

                Debug.WriteLine("The Aircraft is connected now.");
            }
            else
            {
                aircraftConnected = false;
                videoParser = null;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    GpsTB.Text = "GPSSignal: NA";
                    HomLocTB.Text = "HomeLocation: NA";
                    LocTB.Text = "Location: NA";
                    AircraftStateTB.Text = "The Aircraft is disconnected now.";
                });

                Debug.WriteLine("The Aircraft is disconnected now.");
            }
        }

        /// <summary>
        /// Called when drone takes a picture or records a video
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void MainPage_NewlyGeneratedMediaFileChanged(object sender, GeneratedMediaFileInfo? value)
        {
            if (value.HasValue && value.Value.type == MediaFileType.JPEG)
            {
                Debug.WriteLine("DATA GENERATED");
                DroneGenDataPool.Instance.Enqueue(new DroneGenMetaData(value.Value.index, aircraftPosition.latitude, aircraftPosition.longitude));
            }
        }

        /// <summary>
        /// Called when drones velocity is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void MainPage_VelocityChanged(object sender, Velocity3D? value)
        {
            //If you need to do something with the velocity
        }

        /// <summary>
        /// Called when altitude is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_AltitudeChanged(object sender, DoubleMsg? value)
        {
            if (value.HasValue)
            {
                aircraftAltidue = value.Value.value;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
                {
                    AltTB.Text = "Altidue: " + value.Value.value;
                });
            }
        }

        /// <summary>
        /// Called when way point mission state is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_WaypointMissionStateChanged(DJI.WindowsSDK.Mission.Waypoint.WaypointMissionHandler sender, WaypointMissionStateTransition? value)
        {
            if (value.HasValue)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
                {
                    WpMissResTB.Text = "Waypoint Mission Result: " + value.Value.current;
                });
            }
        }

        /// <summary>
        /// Called when GPSSignalLevel is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_GPSSignalLevelChanged(object sender, FCGPSSignalLevelMsg? value)
        {
            if (value.HasValue)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    GpsTB.Text = "GPSSignal: " + value.Value.value;
                });
            }
        }

        /// <summary>
        /// Called when homelocation is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_HomeLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (value.HasValue)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    HomLocTB.Text = "HomeLocation: " + value.Value.latitude + " , " + value.Value.longitude;
                });
            }
        }

        /// <summary>
        /// Called when aircraft location is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void MainPage_AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (value.HasValue)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    aircraftPosition = new LocationCoordinate2D() { latitude = value.Value.latitude, longitude = value.Value.longitude };
                    LocTB.Text = "Location: " + value.Value.latitude + " , " + value.Value.longitude;
                });
            }
        }

        /// <summary>
        /// NOT USED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void App_ProductTypeChangedCB(object sender, ProductTypeMsg? value)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, /*async*/ () =>
            {
                if (value != null && value?.value != ProductType.UNRECOGNIZED)
                {
                    Debug.WriteLine("The Aircraft is connected now.");
                    //You can load/display your pages according to the aircraft connection state here
                }
                else
                {
                    Debug.WriteLine("The Aircraft is disconnected now.");
                    //You can hide your pages according to the aircraft connection state here, or show the connection tips to the users.
                }
            });
        }

        //We need to set the camera type of the aircraft to the DJIVideoParser. After setting camera type, DJIVideoParser would correct the distortion of the video automatically.
        /// <summary>
        /// Called when drones camera type is changed,  DJIVideoParser would correct the distortion of the video automatically
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void OnCameraTypeChanged(object sender, CameraTypeMsg? value)
        {
            if (value != null)
            {
                switch (value.Value.value)
                {
                    case CameraType.MAVIC_2_ZOOM:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Zoom);
                        break;
                    case CameraType.MAVIC_2_PRO:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Pro);
                        break;
                    default:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Others);
                        break;
                }
            }
        }

        #endregion
    }
}
