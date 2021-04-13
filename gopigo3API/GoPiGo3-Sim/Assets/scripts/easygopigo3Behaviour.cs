using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class easygopigo3Behaviour : MonoBehaviour
{
    static Socket listener;
    private CancellationTokenSource source;
    public ManualResetEvent allDone;
    private Camera mainCamera; 
    private Vector3 cameraPos;

    public static readonly int PORT = 1755;
    public static readonly int WAITTIME = 1;
    private int speed = 0;
    private int lastspeed = -400;
    private float lastx = 0;
    private float lasty = 0;
    private float distancex = 0;
    private float distancey = 0;
    private int degree = 0;
    private int lastdegree = -400;
    private int rotation = 0;
    private bool blinker1;
    private bool blinker2;
    private Color led1Color = Color.white;
    private Color led2Color = Color.white;

    public easygopigo3Behaviour()
    {
    }

    // Start is called before the first frame update
    async void Start()
    {
        source = new CancellationTokenSource();
        allDone = new ManualResetEvent(false);
        mainCamera = Camera.main;
        cameraPos = mainCamera.transform.position;

        await Task.Run(() => ListenEvents(source.Token));
    }

    // Update is called once per frame
    void Update()
    {
        //transform.Translate(new Vector3(motor1+motor2, 0, 1) * speed * Time.deltaTime);
        GameObject[] gopigos = GameObject.FindGameObjectsWithTag("GoPiGo3");
        GameObject led1 = GameObject.FindGameObjectsWithTag("led1")[0];
        GameObject led2 = GameObject.FindGameObjectsWithTag("led1")[0];
        GameObject blinker1 = GameObject.FindGameObjectsWithTag("blinker1")[0];
        GameObject blinker2 = GameObject.FindGameObjectsWithTag("blinker2")[0];
        GameObject gopigo = gopigos[0];
        if (degree != lastdegree) {
            lastdegree = degree;
            rotation = rotation + lastdegree;
            gopigo.transform.eulerAngles = new Vector3((float)0, (float)rotation, (float)0);

        }
        if(lastspeed != speed)
        {
            lastspeed = speed;
            distancex = (speed * Time.deltaTime * Mathf.Cos(rotation * Mathf.Deg2Rad)) / 100f;
            distancey = (speed * Time.deltaTime * Mathf.Sin(rotation * Mathf.Deg2Rad)) / 100f;
        }
        if (speed != 0)
        {
            lastx = lastx + distancex;
            lasty = lasty + distancey;
            gopigo.transform.position = (new Vector3((float)lasty, (float)1, (float)lastx));
            mainCamera.transform.position = (new Vector3(cameraPos.x + lasty, cameraPos.y, cameraPos.z + (float)lastx));
        }
        if (this.blinker1)
        {
            var blinkerImage = blinker1.GetComponents<RawImage>();
            blinkerImage[0].color = Color.cyan;
        }
        else
        {
            var blinkerImage = blinker1.GetComponents<RawImage>();
            blinkerImage[0].color = Color.white;
        }
        if (this.blinker2)
        {
            var blinkerImage = blinker2.GetComponents<RawImage>();
            blinkerImage[0].color = Color.cyan;
        }
        else
        {
            var blinkerImage = blinker2.GetComponents<RawImage>();
            blinkerImage[0].color = Color.white;
        }
        var led1Image = led1.GetComponents<RawImage>()[0];
        led1Image.color = led1Color;
        var led2Image = led2.GetComponents<RawImage>()[0];
        led2Image.color = led2Color;
    }
    private void ListenEvents(CancellationToken token)
    {
        IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
        IPAddress ipAddress = ipHostInfo.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);
        listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);  
            while (!token.IsCancellationRequested)
            {
                allDone.Reset();
                print("Waiting for a connection... host :" + ipAddress.MapToIPv4().ToString() + " port : " + PORT);
                listener.BeginAccept(new AsyncCallback(AcceptCallback),listener);
                while(!token.IsCancellationRequested)
                {
                    if (allDone.WaitOne(WAITTIME))
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            print(e.ToString());
        }
    }
    void AcceptCallback(IAsyncResult ar)
    {  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);
 
        allDone.Set();
  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }

    void ReadCallback(IAsyncResult ar)
    {
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        int read = handler.EndReceive(ar);
  
        if (read > 0)
        {
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }
        else
        {
            if (state.sb.Length > 1)
            { 
                string content = state.sb.ToString();
                print($"Read {content.Length} bytes from socket.\n Data : {content}");
                string[] data = content.Split(';');
                foreach(var item in data)
                {
                    string[] value = item.Split(':');
                    if (value[0] == "speed")
                        Int32.TryParse(value[1], out speed);
                    else if (value[0] == "degree")
                    {
                        Int32.TryParse(value[1], out degree);
                    }
                    else if (value[0] == "blinker1")
                        blinker1 = value[1] == "1";
                    else if (value[0] == "blinker2")
                        blinker2 = value[1] == "1";
                    else if (value[0] == "Led4")
                    {
                        int r, g, b;
                        string[] rgb = value[1].Split(',');
                        Int32.TryParse(rgb[0], out r);
                        Int32.TryParse(rgb[1], out g);
                        Int32.TryParse(rgb[2], out b);
                        led1Color = new Color(255-r, 255 - g, 255 - b);
                    }
                    else if (value[0] == "Led8")
                    {
                        int r, g, b;
                        string[] rgb = value[1].Split(',');
                        Int32.TryParse(rgb[0], out r);
                        Int32.TryParse(rgb[1], out g);
                        Int32.TryParse(rgb[2], out b);
                        led2Color = new Color(255 - r, 255 - g, 255 - b);
                    }
                    //else if (value[0] == "motor2")
                    //    Int32.TryParse(value[1], out motor2);

                }
            }
            handler.Close();
        }
    }

    public class StateObject
    {  
        public Socket workSocket = null;  
        public const int BufferSize = 1024;  
        public byte[] buffer = new byte[BufferSize];  
        public StringBuilder sb = new StringBuilder();  
    }  
}
