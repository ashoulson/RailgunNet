using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class Host
  {
    private Dictionary<Connection, Peer> connectionToPeer;

    public Host()
    {
      this.connectionToPeer = new Dictionary<Connection, Peer>();
    }

    public void AddConnection(Connection connection)
    {
      this.connectionToPeer.Add(connection, this.CreatePeer(connection));
    }

    public void ReceivePayload(Connection source, byte[] payload)
    {
      // TODO
    }

    public abstract Peer CreatePeer(Connection connection);

    public virtual void OnPeerConnected(Peer peer) { }
  }
}
