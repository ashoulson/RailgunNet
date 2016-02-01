using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class ClientPeer : Peer
  {
    /// <summary>
    /// The last acknowledged frame update received by the peer.
    /// </summary>
    internal int LastAcked { get; set; }

    public ClientPeer(IConnection connection) : base(connection)
    {
      this.LastAcked = Clock.INVALID_FRAME;
    }
  }
}
