# GhettoVR

GhettoVR was made as a test to evaluate the idea of room-scale VR, similar to what the HTC Vive has to offer, but at a more reasonable cost. It can track the head and both hands. The hands can be used to interact with the basic VR scene.
It was built using Unity 3D along with Google Cardboard and Microsoft Kinect V2 over the course of 24 hours.

### *Components*
1. An Android app running the VR scene and a UDP server
2. A Windows app which uses Microsoft Kinect V2 to track the user and relay the position data to the phone via UDP

### *Usage*
In order to use this, the mobile part has to be compiled and ran on an Android phone (iOS might work, too, but was not tested).
After running the Android app, the Kinect app has to be started and connected to the phone app using its IP address.

Remember to start up the Android app facing the Kinect, so the orientation of the VR scene is tracked properly.


### *Note*
The Android app is not optimized, so it requires a fairly powerful phone. It was tested on a Nexus 6p (which fared fairly well, but overheated pretty fast and throttled down) and on a Google Pixel (which fared much better, but still had issues over time).
