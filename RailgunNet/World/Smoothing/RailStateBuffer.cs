using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RailStateBuffer
  {
    private RailRingBuffer<RailState> buffer;

    public RailStateBuffer()
    {
      this.buffer =
        new RailRingBuffer<RailState>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);
    }

    public void Store(RailState state)
    {
      this.buffer.Store(state);
    }

    internal void PopulateDelta(RailRingDelta<RailState> delta, int currentTick)
    {
      this.buffer.PopulateDelta(delta, currentTick);
    }
  }
}
