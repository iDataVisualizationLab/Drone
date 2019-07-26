# ParkinglotEnforcementServer

NOTE: THIS APPLICATION IS NOT FINISHED IT MAY CONTAIN BUGS.

This NodeJS server is for the detecting images that are sent by the drone and showing them on the map, application requires OpenALPR https://www.openalpr.com/ to be installed on has to be on the PATH envirment variable and MongoDB to be installed on the computer.


----------------------------------------------------------------------------------------------------------------------------------------
This is a system that acts as a basis for parking lot enforcement by using a DJI Mavic 2 drone. This system collects data about license plates, their location and the detection time and stores them in a database for future extraction, however it does not do an actual enforcement. 

Drone controlling part of the system is built for drones that support DJI Windows SDK, however the rest of the system could still work, if they were able to mimic the output of the main program, which is developed by using DJI Windows SDK. 

![System](https://github.com/iDataVisualizationLab/Drone/blob/master/basicsys.png)
