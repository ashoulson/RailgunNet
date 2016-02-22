using System;
using System.Collections.Generic;

using Railgun;
using MiniUDP;

namespace Example
{
  internal class NetPeerWrapper : IRailNetPeer
  {
    public event NetPeerEvent MessagesReady;

    // N.B.: This does not read/write the NetPeer's UserData; it's separate
    public object UserData { get; set; }

    private NetPeer peer;

    public NetPeerWrapper(NetPeer peer)
    {
      this.peer = peer;
      this.peer.MessagesReady += this.OnMessagesReady;
    }

    private void OnMessagesReady(NetPeer peer)
    {
      if (this.MessagesReady != null)
        this.MessagesReady.Invoke(this);
    }

    public IEnumerable<int> ReadReceived(byte[] buffer)
    {
      return this.peer.ReadReceived(buffer);
    }

    public void EnqueueSend(byte[] buffer, int length)
    {
      this.peer.EnqueueSend(buffer, length);
    }
  }
}
