using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// A rolling history buffer to keep track of seen SequenceIds.
  /// </summary>
  internal class RailHistory
  {
    /// <summary>
    /// A multi-chunk bit array that supports shifting.
    /// </summary>
    private class HistoryBits
    {
      internal const int CHUNK_SIZE = 32;

      public int Capacity { get { return this.capacity; } }

      private readonly uint[] chunks;
      private readonly int capacity;

      public HistoryBits(int chunks)
      {
        this.chunks = new uint[chunks];
        this.capacity = chunks * HistoryBits.CHUNK_SIZE;
      }

      public void Set(int index)
      {
        int chunk = index / HistoryBits.CHUNK_SIZE;
        int position = index % HistoryBits.CHUNK_SIZE;

        if (chunk >= this.chunks.Length)
          throw new ArgumentOutOfRangeException("index");
        this.chunks[chunk] |= 0x1U << position;
      }

      public bool Get(int index)
      {
        int chunk = index / HistoryBits.CHUNK_SIZE;
        int position = index % HistoryBits.CHUNK_SIZE;

        if (chunk >= this.chunks.Length)
          throw new ArgumentOutOfRangeException("index");
        return (this.chunks[chunk] & (1U << position)) != 0;
      }

      public void Shift(int count)
      {
        int numChunks = count / HistoryBits.CHUNK_SIZE;
        int numBits = count % HistoryBits.CHUNK_SIZE;

        // Clear the top chunks since they're shifted out
        for (int i = 0; i < numChunks; i++)
        {
          this.chunks[(this.chunks.Length - 1) - i] = 0;
        }

        // Perform the shift
        for (int i = this.chunks.Length - 1; i >= numChunks; i--)
        {
          // Get the high and low bits for shifting
          ulong bits =
            this.chunks[i - numChunks] |
            ((ulong)this.chunks[i] << HistoryBits.CHUNK_SIZE);

          // Perform the mini-shift
          bits <<= numBits;

          // Separate and re-apply
          this.chunks[i] = (uint)bits;
          if ((i + 1) < this.chunks.Length)
            this.chunks[i + 1] |= (uint)(bits >> HistoryBits.CHUNK_SIZE);
        }

        // Clear the bottom chunks since they're shifted out
        for (int i = 0; i < numChunks; i++)
        {
          this.chunks[i] = 0;
        }
      }

      public override string ToString()
      {
        StringBuilder raw = new StringBuilder();
        for (int i = this.chunks.Length - 1; i >= 0; i--)
          raw.Append(
            Convert.ToString(
              this.chunks[i], 2).PadLeft(HistoryBits.CHUNK_SIZE, '0'));

        StringBuilder spaced = new StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
          spaced.Append(raw[i]);
          if (((i + 1) % 8) == 0)
            spaced.Append(" ");
        }

        return spaced.ToString();
      }
    }

    private SequenceId latest;
    private readonly HistoryBits history;

    public SequenceId Latest { get { return this.latest; } }
    public bool IsValid { get { return latest.IsValid; } }

    public RailHistory(int chunks)
    {
      this.latest = SequenceId.START;
      this.history = new HistoryBits(chunks);
    }

    public void Store(SequenceId value)
    {
      int difference = this.latest - value;
      if (difference > 0)
      {
        this.history.Set(difference - 1);
      }
      else
      {
        int offset = -difference;
        this.history.Shift(offset);
        this.history.Set(offset - 1);
        this.latest = value;
      }
    }

    public bool Contains(SequenceId value)
    {
      int difference = (this.Latest - value);
      if (difference == 0)
        return true;
      return this.history.Get(difference - 1);
    }

    public bool IsNewId(SequenceId id)
    {
      if (this.ValueTooOld(id))
        return false;
      if (this.Contains(id))
        return false;
      return true;
    }

    public bool AreInRange(SequenceId lowest, SequenceId highest)
    {
      return (highest - lowest) <= this.history.Capacity;
    }

    public bool ValueTooOld(SequenceId value)
    {
      return ((this.Latest - value) > this.history.Capacity);
    }
  }
}