using System;
using System.Collections.Generic;

using Railgun;

namespace Example
{
  public class Arena
  {
    private RailHost host;
    private RailWorld environment;

    public Arena(RailHost host)
    {
      this.host = host;
      this.environment = host.World;

      host.ClientAdded += this.OnPeerAdded;
    }

    private void OnPeerAdded(RailPeerClient peer)
    {
      DemoEntity entity = this.environment.CreateEntity<DemoEntity>(DemoTypes.TYPE_DEMO);
      entity.InitializeHost(0);
      entity.AssignOwner(peer);
      this.environment.AddEntity(entity);
    }
  }
}
