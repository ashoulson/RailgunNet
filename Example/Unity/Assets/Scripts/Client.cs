using System;
using System.Collections.Generic;

using UnityEngine;

using MiniUDP;
using Railgun;

public class Client : MonoBehaviour
{
  public static Client Instance { get; private set; }

  public string address;
  private NetSocket netSocket;
  private RailClient client;

  void Awake()
  {
    Client.Instance = this;

    this.netSocket = new NetSocket();
    this.netSocket.Connected += this.OnConnected;
    this.netSocket.Disconnected += this.OnDisconnected;
    this.netSocket.TimedOut += this.OnTimedOut;

    this.client = new RailClient();
    Demo.RegisterTypes();
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
    //byte[] buffer = new byte[2048];
    //foreach (int length in source.ReadReceived(buffer))
    //  Debug.Log("Received " + length + " bytes");
  }
}
