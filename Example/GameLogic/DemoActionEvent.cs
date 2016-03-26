using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Railgun;

[RegisterEvent]
public class DemoActionEvent : RailEvent<DemoActionEvent>
{
  public int Key;

  protected override void SetDataFrom(DemoActionEvent other)
  {
    this.Key = other.Key;
  }

  protected override void EncodeData(BitBuffer buffer)
  {
    buffer.WriteInt(this.Key);
  }

  protected override void DecodeData(BitBuffer buffer)
  {
    this.Key = buffer.ReadInt();
  }

  protected override void ResetData()
  {
    this.Key = 0;
  }

  protected override void Invoke(RailEntity entity)
  {
    DemoEvents.OnDemoActionEvent(this);
  }
}