using System;
using System.Collections.Generic;

using Railgun;

namespace Example
{
  public class Arena
  {
    private RailServer server;

    public Arena(RailServer server)
    {
      this.server = server;

      server.ClientAdded += this.OnPeerAdded;
    }

    private void OnPeerAdded(RailPeerClient peer)
    {
      DemoState state = this.server.CreateState<DemoState>();
      state.ArchetypeId = 0;
      DemoEntity entity = this.server.CreateEntity<DemoEntity>(state);
      entity.AssignOwner(peer);
      this.server.AddEntity(entity);
    }
  }
}
