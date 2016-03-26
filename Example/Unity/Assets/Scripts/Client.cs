using System;
using System.Collections.Generic;

using UnityEngine;

using MiniUDP;
using Railgun;

public class Client : MonoBehaviour
{
  private const int BANDWIDTH_WINDOW_SIZE = 60;

  public static Client Instance { get; private set; }

  public static bool DoSmoothing = true;

  public string address;
  private NetSocket netSocket;
  private RailClient client;

  public float KBps = 0.0f;

  private int receivedThisFrame = 0;
  private int framesActive = 0;
  private int[] bandwidthWindow = new int[BANDWIDTH_WINDOW_SIZE];

  void Awake()
  {
    Client.Instance = this;

    this.netSocket = new NetSocket();
    this.netSocket.Connected += this.OnConnected;
    this.netSocket.Disconnected += this.OnDisconnected;
    this.netSocket.TimedOut += this.OnTimedOut;

    DemoEvents.DemoActionEvent += OnDemoActionEvent;

    this.client = new RailClient();
  }

  void OnDemoActionEvent(DemoActionEvent evnt)
  {
    Debug.Log(evnt.Key);
  }

  void Start()
  {
    this.netSocket.Connect(this.address);
  }

  void OnDisable()
  {
    this.netSocket.Shutdown();
    this.netSocket.Transmit();
  }

  void FixedUpdate()
  {
    this.netSocket.Poll();
    this.client.Update();
    this.netSocket.Transmit();

    this.UpdateBandwidth();

    if (Input.GetKey(KeyCode.Alpha1))
      Client.DoSmoothing = true;
    if (Input.GetKey(KeyCode.Alpha2))
      Client.DoSmoothing = false;
  }

  private void UpdateBandwidth()
  {
    this.bandwidthWindow[this.framesActive % BANDWIDTH_WINDOW_SIZE] =
      this.receivedThisFrame;
    this.framesActive++;
    this.receivedThisFrame = 0;

    int sum = 0;
    foreach (int bytes in this.bandwidthWindow)
      sum += bytes;
    float average = (float)sum / (float)BANDWIDTH_WINDOW_SIZE;
    this.KBps = (average / Time.fixedDeltaTime) / 1024.0f;
  }

  private void OnConnected(NetPeer peer)
  {
    Debug.Log("Connected: " + peer.ToString() + " (" + this.netSocket.PeerCount + ")");
    peer.MessagesReady += this.OnPeerMessagesReady;

    this.client.SetPeer(new NetPeerWrapper(peer));
  }

  private void OnDisconnected(NetPeer peer)
  {
    Debug.Log("Disconnected: " + peer.ToString() + " (" + this.netSocket.PeerCount + ")");
  }

  void OnTimedOut(NetPeer peer)
  {
    Debug.Log("Timed Out: " + peer.ToString() + " (" + this.netSocket.PeerCount + ")");
  }

  private void OnPeerMessagesReady(NetPeer source)
  {
    byte[] buffer = new byte[2048];
    foreach (int length in source.ReadReceived(buffer))
    {
      this.receivedThisFrame += length;
      Debug.Log("Received: " + length);
    }
  }
}
