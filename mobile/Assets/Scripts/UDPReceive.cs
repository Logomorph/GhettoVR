using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Generic;

public class UDPReceive : MonoBehaviour
{
    // receiving Thread
    Thread receiveThread;

    // udpclient object
    UdpClient client;

    // public
    // public string IP = "127.0.0.1"; default local
    public int port = 1337;
    public float zOffset = -43;
    public int packetCount = 0;

    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = ""; // clean up this from time to time!;
    public Text countLabel;

    Queue<float[]> positions;
    private object _asyncLock = new object();
    float lastRecvTimeStamp;
    public int maxPositions;

    public GameObject kinectRefPosition;
    public float scale;

    // Use this for initialization
    void Start()
    {
        positions = new Queue<float[]>();
        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }// receive thread

    private void ReceiveData()
    {

        client = new UdpClient(port);
        while (true)
        {

            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);

                float[] position = new float[10];
                Buffer.BlockCopy(data, 0, position, 0, data.Length);
                //Debug.Log("Position: " + position[0] + " " + position[1] + " " + position[2]);

                lock (_asyncLock)
                {
                    positions.Enqueue(position);
                    packetCount++;
                }

            }
            catch (System.Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        countLabel.text = "Packets " + packetCount + " " + positions.Count + " " + lastRecvTimeStamp;
        if (positions.Count != 0)
        {
            float[] position;
            lock (_asyncLock)
            {
                while (positions.Count > maxPositions)
                {
                    positions.Dequeue();
                }
                do
                {
                    position = positions.Dequeue();
                } while (position[9] < lastRecvTimeStamp && positions.Count != 0);
            }
            lastRecvTimeStamp = position[9];
            Vector3 centerPosition = kinectRefPosition.transform.position;
            this.transform.position = new Vector3(centerPosition.x + (position[0] * scale), position[1] * scale, centerPosition.z - (position[2] * scale));

            //Vector3 eulers = GameObject.Find("Head").transform.rotation.eulerAngles;
            //countLabel.text = "Pos " + this.transform.position.x + " " + this.transform.position.y + " " + this.transform.position.z + " " + scale;
            //countLabel.text = "Rot " + eulers.x + " " + eulers.y + " " + eulers.z;

            GameObject rHand = GameObject.Find("rHand");
            GameObject lHand = GameObject.Find("lHand");
            rHand.transform.position = new Vector3(centerPosition.x + (position[3] * scale), position[4] * scale, centerPosition.z - (position[5] * scale));
            lHand.transform.position = new Vector3(centerPosition.x + (position[6] * scale), position[7] * scale, centerPosition.z - (position[8] * scale));
        }
            
    }
}
