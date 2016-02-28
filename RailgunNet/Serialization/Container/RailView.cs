using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTools;

namespace Railgun
{
  internal class RailView : IRailPoolable
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }

    private Dictionary<int, int> idToFrame;

    public RailView()
    {
      this.idToFrame = new Dictionary<int, int>();
    }

    public RailView(int capacity)
    {
      this.idToFrame = new Dictionary<int, int>(capacity);
    }

    public void Update(RailView received)
    {
      foreach (KeyValuePair<int, int> pair in received.idToFrame)
      {
        if (this.idToFrame.ContainsKey(pair.Key))
        {
          CommonDebug.Assert(this.idToFrame[pair.Key] <= pair.Value);
          this.idToFrame[pair.Key] = pair.Value;
        }
        else
        {
          this.idToFrame.Add(pair.Key, pair.Value);
        }
      }
    }

    public void Update(RailSnapshot snapshot)
    {
      foreach (RailState state in snapshot.Values)
      {
        if (this.idToFrame.ContainsKey(state.Id))
        {
          CommonDebug.Assert(this.idToFrame[state.Id] <= snapshot.Tick);
          this.idToFrame[state.Id] = snapshot.Tick;
        }
        else
        {
          this.idToFrame.Add(state.Id, snapshot.Tick);
        }
      }
    }

    public void Add(int id, int frame)
    {
      this.idToFrame.Add(id, frame);
    }

    public bool TryGetValue(int id, out int frame)
    {
      return this.idToFrame.TryGetValue(id, out frame);
    }

    public void Encode(BitBuffer buffer)
    {
      foreach (KeyValuePair<int, int> pair in this.idToFrame)
      {
        buffer.Push(StandardEncoders.Tick, pair.Value);
        buffer.Push(StandardEncoders.EntityId, pair.Key);
      }
      buffer.Push(StandardEncoders.EntityCount, this.idToFrame.Count);
    }

    public static RailView Decode(BitBuffer buffer)
    {
      int count = buffer.Pop(StandardEncoders.EntityCount);
      RailView result = RailResource.Instance.AllocateView();

      for (int i = 0; i < count; i++)
        result.Add(
          buffer.Pop(StandardEncoders.EntityId),
          buffer.Pop(StandardEncoders.Tick));

      return result;
    }

    protected void Reset()
    {
      this.idToFrame.Clear();
    }
  }
}
