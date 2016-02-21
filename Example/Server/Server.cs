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
    private RailHost host;
    private NetHostWrapper wrapper;

    private Arena arena;

    public Server(int port, float updateRate)
    {
      this.port = port;

      this.socket = new NetSocket();

      // Logging
      this.socket.Connected += this.OnConnected;
      this.socket.Disconnected += this.OnDisconnected;
      this.socket.TimedOut += this.OnTimedOut;

      this.host = new RailHost(new RailFactory<DemoState>());
      this.wrapper = new NetHostWrapper(socket, host);
      this.arena = new Arena(this.host);

      this.clock = new Clock(updateRate);
      this.clock.OnFixedUpdate += this.FixedUpdate;
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
      this.host.Update();
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
