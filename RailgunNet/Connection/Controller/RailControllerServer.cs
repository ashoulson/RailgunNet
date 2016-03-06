using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTools;

namespace Railgun
{
  public class RailControllerServer : RailController
  {
    /// <summary>
    /// A history of received packets from the client. Used on the server.
    /// </summary>
    private readonly RailRingBuffer<RailCommand> incomingBuffer;

    /// <summary>
    /// The latest usable command in the dejitter buffer.
    /// </summary>
    internal override RailCommand LatestCommand
    {
      get { return this.latestCommand; }
    }

    private RailCommand latestCommand;

    internal RailControllerServer() : base()
    {
      // We use no divisor for storing commands because commands are sent in
      // batches that we can use to fill in the holes between send frames
      this.incomingBuffer =
        new RailRingBuffer<RailCommand>(RailConfig.DEJITTER_BUFFER_LENGTH);
      this.latestCommand = null;
    }

    internal void Update(int tick)
    {
      this.latestCommand = this.incomingBuffer.GetLatest(tick);
    }

    /// <summary>
    /// Stores incoming commands to be buffered in the dejitter buffer.
    /// </summary>
    internal void StoreIncoming(IEnumerable<RailCommand> commands)
    {
      foreach (RailCommand command in commands)
        this.incomingBuffer.Store(command);
    }

    /// <summary>
    /// Adds an entity to be controlled by this controller.
    /// </summary>
    internal void AddEntity(RailEntity entity, int tick)
    {
      if (entity.Controller == this)
        return;

      CommonDebug.Assert(entity.Controller == null);
      this.controlledEntities.Add(entity);
      entity.Controller = this;

      this.QueueControlEvent(entity.Id, true, tick);
    }

    /// <summary>
    /// Removed an entity from being controlled by this controller.
    /// </summary>
    internal void RemoveEntity(RailEntity entity, int tick)
    {
      CommonDebug.Assert(entity.Controller == this);
      this.controlledEntities.Remove(entity);
      entity.Controller = null;

      this.QueueControlEvent(entity.Id, false, tick);
    }

    private void QueueControlEvent(int entityId, bool granted, int tick)
    {
      RailControlEvent controlEvent =
        RailResource.Instance.AllocateControlEvent();
      controlEvent.EntityId = entityId;
      controlEvent.Granted = granted;
      this.QueueReliable(controlEvent, tick);
      RailPool.Free(controlEvent);
    }
  }
}
