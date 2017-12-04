using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;
using UnityEngine.UI;

public class BodySourceView : MonoBehaviour 
{
    public Material BoneMaterial;
    public GameObject BodySourceManager;
	public InputField ipInputField;
	public Text outText;
    
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
	private BodySourceManager _BodyManager;

	public int port = 1337;
	IPEndPoint remoteEndPoint;
	UdpClient client;
	private float lastSend;
	public float sendInterval = 50;
	private double FaceRotationIncrementInDegrees = 0.5f;
    
    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

	public void startConnection() {
		string newIP = ipInputField.text;
		if (newIP.Length != 0) {
			remoteEndPoint = new IPEndPoint (IPAddress.Parse (newIP), port);
			client = new UdpClient ();
		}
	}
	void Start() 
	{
	}
    
    void Update () 
    {
        if (BodySourceManager == null)
        {
            return;
        }
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }
        
        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
              }
                
            if(body.IsTracked)
            {
                trackedIds.Add (body.TrackingId);
            }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }
            
            if(body.IsTracked)
            {
                if(!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }
                
                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }
    }
    
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);
        
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            lr.SetVertexCount(2);
            lr.material = BoneMaterial;
            lr.SetWidth(0.05f, 0.05f);
            
            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
        }
        
        return body;
    }
    
    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
		Kinect.JointOrientation orientation;
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;
            
            if(_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }
            
            Transform jointObj = bodyObject.transform.FindChild(jt.ToString());
            jointObj.localPosition = GetVector3FromJoint(sourceJoint);
            
            LineRenderer lr = jointObj.GetComponent<LineRenderer>();
            if(targetJoint.HasValue)
            {
                lr.SetPosition(0, jointObj.localPosition);
                lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
                lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
            }
            else
            {
                lr.enabled = false;
            }
        }
		/*Kinect.JointOrientation headOrientation = body.JointOrientations[Kinect.JointType.Neck];
		Kinect.JointOrientation chestOrientation = body.JointOrientations[Kinect.JointType.SpineBase];
		Kinect.Vector4 orientationVec = headOrientation.Orientation;
		Vector3 neckEuler = EulerAnglesFromKinect (orientationVec);
		Vector3 chestEuler = EulerAnglesFromKinect (chestOrientation.Orientation);
		if (headOrientation != null) {
			int pitch = 0, yaw = 0, roll = 0;
			ExtractFaceRottionInDegrees (headOrientation.Orientation,out pitch,out yaw,out roll);
			outText.text = "Orientation:\nX " + neckEuler.x + "\nY " + neckEuler.y + "\nZ " + neckEuler.z;
			outText.text += "\nOrientation:\nX " + chestEuler.x + "\nY " + chestEuler.y + "\nZ " + chestEuler.z;
			// Y is the way to go.
		}*/
		/*if (Time.time *1000 - lastSend > sendInterval) {
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
				var data = new byte[position.Count * 4];
				Buffer.BlockCopy (position.ToArray (), 0, data, 0, data.Length);
				client.Send (data, data.Length, remoteEndPoint);
			} catch (Exception err) {
				//Debug.Log (err.ToString ());
			}
			lastSend = Time.time*1000;
		}*/
    }
    
    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }
    
    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }

	private void ExtractFaceRottionInDegrees(Kinect.Vector4 quaternation, out int pitch, out int yaw, out int roll) 
	{
		double x = quaternation.X;
		double y = quaternation.Y;
		double z = quaternation.Z;
		double w = quaternation.W;

		// convert face rotation quat to Euler angles in degrees
		double yawD, pitchD, rollD;
		pitchD = Math.Atan2 (2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
		yawD = Math.Asin (2 * ((w * y) - (x * z))) / Math.PI * 180.0f;
		rollD = Math.Atan2 (2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

		//clamp the values to multiple of the specified increment to control refresh rate
		double increment = FaceRotationIncrementInDegrees;
		pitch = (int)(Math.Floor ((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
		yaw = (int)(Math.Floor ((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
		roll = (int)(Math.Floor ((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
	}
	private Vector3 EulerAnglesFromKinect(Kinect.Vector4 quaternation) {
		Quaternion quat = new Quaternion (quaternation.X, quaternation.Y, quaternation.Z, quaternation.W);
		return quat.eulerAngles;
	}
}
