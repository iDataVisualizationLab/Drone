using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DJI.WindowsSDK;
using DJI;
namespace Brain_uwp.ParkingLotMissions
{
    /*  COPY THIS TEMPLATE AND PASTE INTO CLASS TO CREATE NEW MISSIOn
    
         public static WaypointMission Get_missionNameHere_Mission()
        {
            WaypointMission mission = new WaypointMission();
            mission.waypointCount = 0;
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.NO_ACTION;
            mission.headingMode = WaypointMissionHeadingMode.USING_WAYPOINT_HEADING;
            mission.flightPathMode = WaypointMissionFlightPathMode.NORMAL;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 0;
            mission.waypoints = new List<Waypoint>()
            {
                InitWaypoint(_lat , _lon , _alt , _heading , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType. }
                }),
                InitWaypoint(_lat , _lon , _alt , _heading , new List<WaypointAction>(){
                   
                })
                /// ADD MORE IF NEEDED
            };
            return mission;
        }
 
         */


    //TODO: Storing mission data like this is not good, best approach would be store them in a file, get data from there when its needed.
    /// <summary>
    /// This class is just temporary data space for the missions
    /// </summary>
    public class ParkingLotMissions
    {
        /// <summary>
        /// This creates a test mission, 
        /// It will create a square path around given origin point
        /// </summary>
        /// <param name="loc"> the origin of the square path that will be followed</param>
        /// <returns> <see cref="WaypointMission"/>  </returns>
        public static WaypointMission GetTestMission(LocationCoordinate2D loc)
        {
            WaypointMission mission = new WaypointMission();
            mission.waypointCount = 0;
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.NO_ACTION;
            mission.headingMode = WaypointMissionHeadingMode.AUTO;
            mission.flightPathMode = WaypointMissionFlightPathMode.NORMAL;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 0;
            mission.waypoints = new List<Waypoint>()
            {
                InitWaypoint(loc.latitude+0.00001, loc.longitude+0.000015, 3, 0, new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                InitWaypoint(loc.latitude+0.00001, loc.longitude-0.000015, 3, 0, new List<WaypointAction>(){
                    
                }),
                InitWaypoint(loc.latitude-0.00001, loc.longitude-0.000015, 3, 0, new List<WaypointAction>(){
                    
                }),
                InitWaypoint(loc.latitude-0.00001, loc.longitude+0.000015, 3, 0, new List<WaypointAction>(){
                    
                })
            };
            return mission;
        }


        /// <summary>
        /// Mission path of parking lot R16 of the Texas Tech University
        /// </summary>
        /// <returns> <see cref="WaypointMission"/> </returns>
        public static WaypointMission GetR16Mission()
        {
            WaypointMission mission = new WaypointMission();
            mission.waypointCount = 0;
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.AUTO_LAND;
            mission.headingMode = WaypointMissionHeadingMode.USING_WAYPOINT_HEADING;
            mission.flightPathMode = WaypointMissionFlightPathMode.NORMAL;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 0;
            mission.waypoints = new List<Waypoint>()
            {
                InitWaypoint(33.588462, -101.875579, 3, 90 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                InitWaypoint(33.588483, -101.875543, 3, 90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588639, -101.875553, 3, 90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588637, -101.875607, 3, -90 , new List<WaypointAction>(){
  
                }),
                InitWaypoint(33.588510, -101.875615, 3, -90 , new List<WaypointAction>(){
  
                }),
                InitWaypoint(33.588462, -101.875579, 3, -180 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.STOP_RECORD }
                }),

            };
            return mission;
        }

        /// <summary>
        /// Mission path of parking lot R25 of the Texas Tech University
        /// </summary>
        /// <returns> <see cref="WaypointMission"/> </returns>
        public static WaypointMission GetR25Mission()
        {
            WaypointMission mission = new WaypointMission();
            mission.waypointCount = 0;
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.NO_ACTION;
            mission.headingMode = WaypointMissionHeadingMode.USING_WAYPOINT_HEADING;
            mission.flightPathMode = WaypointMissionFlightPathMode.NORMAL;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 0;
            mission.waypoints = new List<Waypoint>()
            {
                InitWaypoint(33.586253, -101.881052 , 3, 90 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                InitWaypoint(33.585668,-101.881044, 3, 90 , new List<WaypointAction>(){
                    
                }),
                InitWaypoint(33.585668,-101.881149, 3, -180 , new List<WaypointAction>(){
                    
                }),
                InitWaypoint(33.585666,-101.881288, 3, -180 , new List<WaypointAction>(){
                    
                }),
                InitWaypoint(33.585690,-101.881288, 3, -90 , new List<WaypointAction>(){
                    

                }),
                InitWaypoint(33.586256,-101.881284, 3, -90 , new List<WaypointAction>(){
                    new WaypointAction(){actionType = WaypointActionType.STOP_RECORD }

                })
            };
            return mission;
        }

        /// <summary>
        /// Mission path of parking lot R25 of the Texas Tech University
        /// </summary>
        /// <returns> <see cref="WaypointMission"/></returns>
        public static WaypointMission GetR17TestMission()
        {
            WaypointMission mission = new WaypointMission();
            mission.waypointCount = 0;
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.NO_ACTION;
            mission.headingMode = WaypointMissionHeadingMode.USING_WAYPOINT_HEADING;
            mission.flightPathMode = WaypointMissionFlightPathMode.NORMAL;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 0;
            mission.waypoints = new List<Waypoint>()
            {
                InitWaypoint(33.578834, -101.863277, 3, -180 , new List<WaypointAction>(){
               
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                InitWaypoint(33.578842, -101.863142, 3, -180 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.STOP_RECORD }
                })
            };
            return mission;
        }

        /// <summary>
        /// Test mission path of parking lot R25 of the Texas Tech University
        /// </summary>
        /// <returns> <see cref="WaypointMission"/> </returns>
        public static WaypointMission GetR17Mission()
        {
            WaypointMission mission = new WaypointMission();
            mission.waypointCount = 0;
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.NO_ACTION;
            mission.headingMode = WaypointMissionHeadingMode.USING_WAYPOINT_HEADING;
            mission.flightPathMode = WaypointMissionFlightPathMode.NORMAL;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 0;
            mission.waypoints = new List<Waypoint>()
            {
                /*left*/
                InitWaypoint(33.588915, -101.875378, 3, -90 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                InitWaypoint(33.588911, -101.875216 , 3, -90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588983, -101.875514 , 3, -90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589006, -101.875515 , 3, -90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589032, -101.875515 , 3, -90 , new List<WaypointAction>(){
                 
                }),
                InitWaypoint(33.589057, -101.875513 , 3, -90 , new List<WaypointAction>(){
                  
                }),
                InitWaypoint(33.589105, -101.875503 , 3, -90 , new List<WaypointAction>(){
                   
                }),
                InitWaypoint(33.589132, -101.875506 , 3, -90 , new List<WaypointAction>(){
            
                }),
                InitWaypoint(33.589159, -101.875509 , 3, -90 , new List<WaypointAction>(){
               
                }),
                InitWaypoint(33.589179, -101.875509 , 3, -90 , new List<WaypointAction>(){
                 
                }),
                InitWaypoint(33.589207, -101.875514 , 3, -90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589231, -101.875515 , 3, -90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589254, -101.875505 , 3, -90 , new List<WaypointAction>(){
             
                }),
                InitWaypoint(33.589283, -101.875513 , 3, -90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589305, -101.875512 , 3, -90 , new List<WaypointAction>(){
      
                }),
                InitWaypoint(33.589329, -101.875511 , 3, -90 , new List<WaypointAction>(){

                }),
                /*up*/
                InitWaypoint(33.589354, -101.875502 , 3, 0 , new List<WaypointAction>(){
      
                }),
                InitWaypoint(33.589357, -101.875468 , 3, 0 , new List<WaypointAction>(){
                 
                }),
                InitWaypoint(33.589359, -101.875440 , 3, 0 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589362, -101.875410 , 3, 0 , new List<WaypointAction>(){
          
                }),
                InitWaypoint(33.589365, -101.875377 , 3, 0 , new List<WaypointAction>(){
                  
                }),
                InitWaypoint(33.589367, -101.875349 , 3, 0 , new List<WaypointAction>(){
                
                }),
                InitWaypoint(33.589367, -101.875320 , 3, 0 , new List<WaypointAction>(){
  
                }),
                InitWaypoint(33.589363, -101.875290 , 3, 0 , new List<WaypointAction>(){
        
                }),
                InitWaypoint(33.589365, -101.875261, 3, 0 , new List<WaypointAction>(){
           
                }),
                InitWaypoint(33.589365, -101.875230, 3, 0 , new List<WaypointAction>(){
            
                }),
                InitWaypoint(33.589362, -101.875197, 3, 0 , new List<WaypointAction>(){
              
                }),
                InitWaypoint(33.589364, -101.875143, 3, 0 , new List<WaypointAction>(){
                
                }),
                InitWaypoint(33.589362, -101.875110, 3, 0 , new List<WaypointAction>(){
                   
                }),
                /*right*/
                InitWaypoint(33.589330, -101.875030, 3, 90 , new List<WaypointAction>(){
 
                }),
                InitWaypoint(33.589311, -101.875027, 3, 90 , new List<WaypointAction>(){
    
                }),
                InitWaypoint(33.589284, -101.875027, 3, 90 , new List<WaypointAction>(){
                    
                }),
                InitWaypoint(33.589259, -101.875025, 3, 90 , new List<WaypointAction>(){
            
                }),
                InitWaypoint(33.589234, -101.875025, 3, 90 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.589209, -101.875028, 3, 90 , new List<WaypointAction>(){
                 
                }),
                /*moving*/
                InitWaypoint(33.589205, -101.875068, 3, 180 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.STOP_RECORD }
                }),
                InitWaypoint(33.589088, -101.875072, 3, 180 , new List<WaypointAction>(){
      
                }),
                InitWaypoint(33.588968, -101.875048, 3, 180 , new List<WaypointAction>(){

                }),
                /*down*/
                InitWaypoint(33.588951, -101.875047, 3, 180 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                InitWaypoint(33.588942, -101.875084, 3, 180 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588943, -101.875120, 3, 180 , new List<WaypointAction>(){
                   
                }),
                InitWaypoint(33.588933, -101.875217, 3, 180 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588933, -101.875259, 3, 180 , new List<WaypointAction>(){
                  
                }),
                InitWaypoint(33.588933, -101.875250, 3, 180 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588932, -101.875281, 3, 180 , new List<WaypointAction>(){
                    
                }),
                InitWaypoint(33.588930, -101.875313, 3, 180 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588927, -101.875345, 3, 180 , new List<WaypointAction>(){
               
                }),
                InitWaypoint(33.588928, -101.875376, 3, 180 , new List<WaypointAction>(){

                }),
                InitWaypoint(33.588925, -101.875408, 3, 180 , new List<WaypointAction>(){
                   
                }),
                /*moving 2*/
                InitWaypoint(33.588960, -101.875452, 3, 0 , new List<WaypointAction>(){
                
                }),
                /*inner 1 left*/
                InitWaypoint(33.589032, -101.875451, 3, 90 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.START_TAKE_PHOTO }
                }),
                /*temp move away*/
                InitWaypoint(33.589029, -101.875483, 3, 0 , new List<WaypointAction>(){
                    new WaypointAction() { actionType = WaypointActionType.STOP_RECORD }
                })
            };
            return mission;
        }

        /// <summary>
        /// An utility method for creating way point.
        /// </summary>
        /// <param name="lat"> Latitude of the way point </param>
        /// <param name="lon"> Longitude of the way point </param>
        /// <param name="alt"> Altitude of the drone in meters</param>
        /// <param name="heading"> Heading of the drone 0 is look NORTH, -90 WEST, 90 EAST , -180 or 180 SOUTH drone will gradually change its position until it arrives to the point</param>
        /// <param name="actions"> A list of actions to be done when the drone arrives its position</param>
        /// <returns> <see cref="Waypoint"/> </returns>
        private static Waypoint InitWaypoint(double lat, double lon, double alt, int heading, List<WaypointAction> actions)
        {
            Waypoint waypoint = new Waypoint()
            {
                location = new LocationCoordinate2D() { latitude = lat, longitude = lon },
                altitude = alt,
                gimbalPitch = -45,
                turnMode = WaypointTurnMode.CLOCKWISE,
                heading = heading,
                actionRepeatTimes = 1,
                actionTimeoutInSeconds = 10,
                cornerRadiusInMeters = 0.2,
                speed = 0.5,
                shootPhotoTimeInterval = 2,
                shootPhotoDistanceInterval = -1,
                waypointActions = actions
            };
            return waypoint;
        }
    }
}
