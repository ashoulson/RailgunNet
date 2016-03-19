/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections;
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
      this.server = new RailServer();

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
