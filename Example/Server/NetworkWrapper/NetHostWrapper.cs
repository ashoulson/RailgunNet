using System;
using System.Collections.Generic;

using Railgun;
using MiniUDP;

namespace Example
{
  /// <summary>
  /// Responsible for interpreting events from the socket and communicating
  /// them to the Railgun host.
  /// </summary>
  internal class NetHostWrapper
  {
    private NetSocket socket;
    private RailHost host;

    public NetHostWrapper(NetSocket socket, RailHost host)
    {
      this.host = host;

      this.socket = socket;
      this.socket.Connected += this.OnConnected;
      this.socket.Disconnected += this.OnDisconnected;
      this.socket.TimedOut += this.OnTimedOut;
    }

    private void OnConnected(NetPeer peer)
    {
      NetPeerWrapper wrapper = new NetPeerWrapper(peer);
      peer.UserData = wrapper;
      this.host.AddPeer(wrapper);
    }

    private void OnDisconnected(NetPeer peer)
    {
      NetPeerWrapper wrapper = (NetPeerWrapper)peer.UserData;
      this.host.RemovePeer(wrapper);
    }

    private void OnTimedOut(NetPeer peer)
    {
      NetPeerWrapper wrapper = (NetPeerWrapper)peer.UserData;
      this.host.RemovePeer(wrapper);
    }
  }
}
