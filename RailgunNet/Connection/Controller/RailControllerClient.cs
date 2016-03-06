using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTools;

namespace Railgun
{
  public class RailControllerClient : RailController
  {
    /// <summary>
    /// A history of sent commands to the server. Used on the client.
    /// </summary>
    private readonly Queue<RailCommand> outgoingBuffer;

    internal override IEnumerable<RailCommand> OutgoingCommands
    {
      get { return this.outgoingBuffer; }
    }

    internal RailControllerClient() : base()
    {
      this.outgoingBuffer = new Queue<RailCommand>();
    }


    internal virtual void AddEntity(RailEntity entity)
    {
      if (entity.Controller == this)
        return;

      CommonDebug.Assert(entity.Controller == null);
      this.controlledEntities.Add(entity);
      entity.Controller = this;
    }

    internal virtual void RemoveEntity(RailEntity entity)
    {
      CommonDebug.Assert(entity.Controller == this);
      this.controlledEntities.Remove(entity);
      entity.Controller = null;
    }

    internal void QueueOutgoing(RailCommand command)
    {
      if (this.outgoingBuffer.Count < RailConfig.COMMAND_BUFFER_COUNT)
        this.outgoingBuffer.Enqueue(command);
    }

    internal void CleanCommands(int lastReceivedTick)
    {
      while (true)
      {
        if (this.outgoingBuffer.Count == 0)
          break;
        if (this.outgoingBuffer.Peek().Tick > lastReceivedTick)
          break;
        RailPool.Free(this.outgoingBuffer.Dequeue());
      }
    }
  }
}
