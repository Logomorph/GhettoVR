using UnityEngine;
using System.Collections;
using System.Collections.Generic;	
using Kinect = Windows.Kinect;
using Windows.Kinect;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;
using UnityEngine.UI;

public class BodySourceManager : MonoBehaviour 
{
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
	private Body[] _Data = null;

	public int port = 1337;
	IPEndPoint remoteEndPoint;
	UdpClient client;
	public InputField ipInputField;
	public Text outText;
	private float lastSend;
	private bool connected = false;
    
    public Body[] GetData()
    {
        return _Data;
    }

	public void startConnection() {
		string newIP = ipInputField.text;
		if (newIP.Length != 0) {
			remoteEndPoint = new IPEndPoint (IPAddress.Parse (newIP), port);
			Debug.Log (newIP);
			client = new UdpClient ();
			connected = true;
			_Reader.FrameArrived += this.Reader_FrameArrived;
		}
	}

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
			_Reader = _Sensor.BodyFrameSource.OpenReader();
            
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }   
    }
    
    void Update () 
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }
                
                frame.GetAndRefreshBodyData(_Data);
                
                frame.Dispose();
                frame = null;
            }
        }    
    }
    
    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }

	private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
	{

		using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
		{
			if (bodyFrame != null)
			{
				Body[] bodies = new Body[bodyFrame.BodyCount];
				bodyFrame.GetAndRefreshBodyData (bodies);
				foreach ( Body body in bodies) 
				{
					if (body.IsTracked) {

						if (connected) {
							SendBodyData (body);
								break;
						}
					}
				}
			}
		}
	}

	private void SendBodyData (Body body)
	{
		//if (Time.time *1000 - lastSend > sendInterval) {
			Kinect.Joint neck = body.Joints [Kinect.JointType.Head];
			Kinect.Joint rHand = body.Joints [Kinect.JointType.HandRight];
			Kinect.Joint lHand = body.Joints [Kinect.JointType.HandLeft]; 
			try {
				List<float> position = new List<float> ();
				position.Add (neck.Position.X);
				position.Add (neck.Position.Y);
				position.Add (neck.Position.Z);
				position.Add (rHand.Position.X);	
				position.Add (rHand.Position.Y);
				position.Add (rHand.Position.Z);
				position.Add (lHand.Position.X);
				position.Add (lHand.Position.Y);
				position.Add (lHand.Position.Z);
				position.Add (lastSend);
				Debug.Log(position.ToString());
				var data = new byte[position.Count * 4];
				Buffer.BlockCopy (position.ToArray (), 0, data, 0, data.Length);
				client.Send (data, data.Length, remoteEndPoint);
			} catch (Exception err) {
			Debug.Log (err.StackTrace);
			}
			lastSend = Time.time*1000;
		//}
	}
}
