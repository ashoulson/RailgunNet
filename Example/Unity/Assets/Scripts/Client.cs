using System;
using System.Collections.Generic;

using UnityEngine;

using MiniUDP;
using Railgun;

public class Client : MonoBehaviour
{
  public static Client Instance { get; private set; }

  public int RemoteTick { get { return this.client.RemoteTick; } }

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

    this.client = new RailClient(new DemoCommand(), new DemoState());
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

    DemoCommand command = this.client.CreateCommand<DemoCommand>();
    this.PopulateCommand(command);
    this.client.RegisterCommand(command);

    this.client.Update();
    this.netSocket.Transmit();
  }

  private void PopulateCommand(DemoCommand command)
  {
    command.SetData(
      Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
      Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S),
      Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
      Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D));
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
