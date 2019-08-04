# Drone

NOTE: THIS APPLICATION IS NOT FINISHED IT MAY CONTAIN BUGS.

[![Watch the video](https://img.youtube.com/vi/AUeFbDf5x9A/0.jpg)](https://www.youtube.com/watch?v=AUeFbDf5x9A)


### Demo video: https://www.youtube.com/watch?v=AUeFbDf5x9A
----------------------------------------------------------------------------------------------------------------------------------------
# ParkinglotEnforcementServer

This NodeJS server is for the detecting images that are sent by the drone and showing them on the map, application requires OpenALPR https://www.openalpr.com/ to be installed on has to be on the PATH envirment variable and MongoDB to be installed on the computer.


----------------------------------------------------------------------------------------------------------------------------------------
# ParkinglotEnforcementDJIDrone


NOTE: THIS APPLICATION IS NOT FINISHED IT MAY CONTAIN BUGS.

This application is responsible for controlling the DJI Mavic 2. To enable this applice one must have a DJI App key
Follow this instruction on how to get access to key https://developer.dji.com/windows-sdk/documentation/quick-start/index.html

After you can do the following:
  1-	To Register the app first go to https://developer.dji.com/ and register a user
  2-	Then login to your account and clink on create app button
  3-	Choose SDK as Windows SDK app name does not matter, Package Name should be same as Applications package name, 
    - To get the applications package name, open the project solution in visual studio
    - On the solution explorer window (usually on the right) find Package.appxmanifest file and click it twice
    - Navigate to Packaging
    - Now you should see the Package name on the page.
  4-	Category and Description does not matter put some random stuff or fill it correctly
  5-	Click on create app
  6-	After registration you should receive a email.
  7-	You can access your app key from apps page just click on the app and copy the key.
  8-	After you copy the key open the project and find the file MainPage.xaml.cs and open
  9-	Press ctrl+f and search for DJISDKManager.Instance.RegisterApp there should be only one instance
  10-	Replace your app key
  11-	Its done you can run the application and see if the app is registered correctly, you can see the results on the top left, where it should say App Registered Correctly. You need internet connection to register the app

To start to application
  1-	Make sure remote controller and the drone are off,
  2-	Make sure the ParkinglotEnforcementServer(Brain_node) is on
  3-	Connect the remote controller to the pc
  4-	Open the remote controller
  5-	Open the drone
  6-	Wait for remote controller to connect to drone
  7-	Open the application
  8-	Wait for it registers and detects the drone
  9-	If the live camera video feed disappears you can close and reopen the application again
  10-	Press Set Ground Station Enabled button (you should see NO ERROR code
  11-	If you want to see where the drone is on the map press Center to Aircraft button
  12-	Select a parking lot from the selection box
  13-	Click Load Waypoint Mission button (two pop is going to appear; they all should say NO ERROR)
  14-	You can start the mission by clicking the Start Mission button

To add new waypoint mission
1-	Open project solution
2-	Open file ParkingLotMission.cs
3-	Copy the commented TEMPLATE and paste into class and change the values accordingly (you can get location of any place from google maps)
4-	Open MainPage.xaml (needs to be in code view)
5-	Locate <ComboBox x:Name="ParkingLotMissionCB" Header="Parking Lot" PlaceholderText="Pick a parking Lot" Width="200">
6-	 Add your new parking lot mission <x:String>_changeThisToTheMissionName_</x:String> 
7-	Open MainPage.xaml.cs
8-	Locate (Press ctrl+f) GetMission(string missionID)
9-	Add one more “else if” , 
    else if (missionID.Equals("_changeThisToTheMissionName_"))
    {
    mission = ParkingLotMissions.ParkingLotMissions.Get_changeThisToTheMissionName_Mission();
    }

----------------------------------------------------------------------------------------------------------------------------------------
This is a system that acts as a basis for parking lot enforcement by using a DJI Mavic 2 drone. This system collects data about license plates, their location and the detection time and stores them in a database for future extraction, however it does not do an actual enforcement. 

Drone controlling part of the system is built for drones that support DJI Windows SDK, however the rest of the system could still work, if they were able to mimic the output of the main program, which is developed by using DJI Windows SDK. 

![System](https://github.com/iDataVisualizationLab/Drone/blob/master/basicsys.png)
