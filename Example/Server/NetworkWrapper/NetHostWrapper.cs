using System;
using System.Collections.Generic;

using Railgun;
using MiniUDP;

namespace Example
{
  /// <summary>
  /// Responsible for interpreting events from the socket and communicating
  /// them to the Railgun server.
  /// </summary>
  internal class NetServerWrapper
  {
    private NetSocket socket;
    private RailServer server;

    public NetServerWrapper(NetSocket socket, RailServer server)
    {
      this.server = server;

      this.socket = socket;
      this.socket.Connected += this.OnConnected;
      this.socket.Disconnected += this.OnDisconnected;
      this.socket.TimedOut += this.OnTimedOut;
    }

    private void OnConnected(NetPeer peer)
    {
      NetPeerWrapper wrapper = new NetPeerWrapper(peer);
      peer.UserData = wrapper;
      this.server.AddPeer(wrapper);
    }

    private void OnDisconnected(NetPeer peer)
    {
      NetPeerWrapper wrapper = (NetPeerWrapper)peer.UserData;
      this.server.RemovePeer(wrapper);
    }

    private void OnTimedOut(NetPeer peer)
    {
      NetPeerWrapper wrapper = (NetPeerWrapper)peer.UserData;
      this.server.RemovePeer(wrapper);
    }
  }
}
