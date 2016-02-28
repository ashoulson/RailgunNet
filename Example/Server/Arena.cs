using System;
using System.Collections.Generic;

using Railgun;

namespace Example
{
  public class Arena
  {
    private RailHost host;

    public Arena(RailHost host)
    {
      this.host = host;

      host.ClientAdded += this.OnPeerAdded;
    }

    private void OnPeerAdded(RailPeerClient peer)
    {
      DemoState state = this.host.CreateState<DemoState>();
      state.ArchetypeId = 0;
      DemoEntity entity = this.host.CreateEntity<DemoEntity>(state);
      entity.AssignOwner(peer);
      this.host.AddEntity(entity);
    }
  }
}
