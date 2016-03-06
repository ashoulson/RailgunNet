using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RailControlEvent : RailEvent<RailControlEvent>
  {
    protected internal override int EventType
    {
      get { return RailEventTypes.TYPE_CONTROL; }
    }

    internal int EntityId { get; set; }
    internal bool Granted { get; set; }

    internal void SetData(
      int entityId,
      bool granted)
    {
      this.EntityId = entityId;
      this.Granted = granted;
    }

    protected override void ResetData()
    {
      this.EntityId = RailEntity.INVALID_ID;
      this.Granted = false;
    }

    protected internal override void SetDataFrom(RailControlEvent other)
    {
      this.EntityId = other.EntityId;
      this.Granted = other.Granted;
    }

    protected override void EncodeData(BitBuffer buffer)
    {
      // Write: [EntityId]
      buffer.Push(StandardEncoders.EntityId, this.EntityId);

      // Write: [Granted]
      buffer.Push(StandardEncoders.Bool, this.Granted);
    }

    protected override void DecodeData(BitBuffer buffer)
    {
      // Write: [Granted]
      this.Granted = buffer.Pop(StandardEncoders.Bool);

      // Write: [EntityId]
      this.EntityId = buffer.Pop(StandardEncoders.EntityId);
    }
  }
}
