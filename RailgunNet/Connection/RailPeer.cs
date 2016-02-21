using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public delegate void RailPeerEvent(RailPeer peer);

  public class RailPeer
  {
    internal event RailPeerEvent MessagesReady;

    internal INetPeer NetPeer { get { return this.NetPeer; } }
    internal int LastAckedTick { get; set; }

    private readonly INetPeer netPeer;

    internal RailPeer(INetPeer netPeer)
    {
      this.netPeer = netPeer;
      this.netPeer.MessagesReady += this.OnMessagesReady;
      this.LastAckedTick = RailClock.INVALID_TICK;
    }

    internal IEnumerable<int> ReadReceived(byte[] buffer)
    {
      return this.netPeer.ReadReceived(buffer);
    }

    internal void EnqueueSend(byte[] buffer, int length)
    {
      this.netPeer.EnqueueSend(buffer, length);
    }

    private void OnMessagesReady(INetPeer peer)
    {
      if (this.MessagesReady != null)
        this.MessagesReady.Invoke(this);
    }
  }
}
