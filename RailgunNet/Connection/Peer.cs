using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class Peer
  {
    public Connection Connection { get; private set; }
    private SnapshotBuffer buffer;

    private Peer()
    {
      throw new NotSupportedException();
    }

    public Peer(Connection connection)
    {
      this.Connection = connection;
    }
  }
}
