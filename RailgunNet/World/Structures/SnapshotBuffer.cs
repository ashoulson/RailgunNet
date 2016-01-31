
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class SnapshotBuffer
  {
    private const int DEFAULT_CAPACITY = 8;

    // TODO: This is a lazy implementation, could be made more efficient
    // with a proper array-based ring buffer that allowed random access
    private Queue<Snapshot> snapshots;
    private Dictionary<int, Snapshot> frameToSnapshot;

    public int NewestFrame { get; private set; }
    public int OldestFrame { get; private set; }

    public SnapshotBuffer(int capacity = SnapshotBuffer.DEFAULT_CAPACITY)
    {
      this.snapshots = new Queue<Snapshot>(capacity);
      this.frameToSnapshot = new Dictionary<int, Snapshot>(capacity);
    }

    public void Push(Snapshot snapshot)
    {
      RailgunUtil.Assert(snapshot.Frame > this.NewestFrame);

      this.snapshots.Enqueue(snapshot);
      this.frameToSnapshot.Add(snapshot.Frame, snapshot);
      this.NewestFrame = snapshot.Frame;
    }

    public Snapshot Pop()
    {
      Snapshot oldest = this.snapshots.Dequeue();
      this.frameToSnapshot.Remove(oldest.Frame);

      if (this.snapshots.Count > 0)
        this.OldestFrame = this.snapshots.Peek().Frame;
      else
        this.OldestFrame = Clock.INVALID_FRAME;
      return oldest;
    }

    public Snapshot GetOrOlder(int frame)
    {
      Snapshot latest = null;
      foreach (Snapshot snapshot in this.snapshots)
      {
        if (snapshot.Frame > frame)
          break;
        latest = snapshot;
      }

      return latest;
    }

    public bool TryGetValue(int frame, out Snapshot snapshot)
    {
      return this.frameToSnapshot.TryGetValue(frame, out snapshot);
    }

    public Snapshot Get(int frame)
    {
      return this.frameToSnapshot[frame];
    }

    /// <summary>
    /// Dumps any frames older than the given frame
    /// </summary>
    public void Drain(int frame)
    {
      while (this.IsOlderThan(frame))
        this.Pop();
    }

    private bool IsOlderThan(int frame)
    {
      return
        (this.OldestFrame < frame) && 
        (this.OldestFrame != Clock.INVALID_FRAME);
    }
  }
}
