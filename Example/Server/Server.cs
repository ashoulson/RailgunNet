using System;
using System.Collections.Generic;

using Railgun;
using MiniUDP;

namespace Example
{
  internal class Server
  {
    private int port;
    private Clock clock;

    private NetSocket socket;
    private RailServer server;
    private NetServerWrapper wrapper;

    private Arena arena;

    public Server(int port, float updateRate)
    {
      this.port = port;

      this.socket = new NetSocket();

      this.server = new RailServer(new DemoCommand(), new DemoState());
      this.wrapper = new NetServerWrapper(socket, server);
      this.arena = new Arena(this.server);

      this.clock = new Clock(updateRate);
      this.clock.OnFixedUpdate += this.FixedUpdate;

      // Logging
      this.socket.Connected += this.OnConnected;
      this.socket.Disconnected += this.OnDisconnected;
      this.socket.TimedOut += this.OnTimedOut;
    }

    public void Start()
    {
      this.socket.Bind(this.port);
      this.clock.Start();
    }

    public void Update()
    {
      this.clock.Tick();
    }

    public void Stop()
    {
      this.socket.Shutdown();
      this.socket.Transmit();
    }

    private void FixedUpdate()
    {
      this.socket.Poll();
      this.server.Update();
      this.socket.Transmit();
    }

    private void OnConnected(NetPeer peer)
    {
      Console.WriteLine("Connected: " + peer);
    }

    private void OnDisconnected(NetPeer peer)
    {
      Console.WriteLine("Disconnected: " + peer);
    }

    private void OnTimedOut(NetPeer peer)
    {
      Console.WriteLine("Timed Out: " + peer);
    }
  }
}
