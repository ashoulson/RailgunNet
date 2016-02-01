using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class Client
  {
    private const int PAYLOAD_CHOKE = 3;
    private const int BUFFER_SIZE = 60;

    //public event Action Connected;
    //public event Action Disconnected;

    public Peer Host { get; private set; }
    private Interpreter interpreter;
    private int lastReceived;

    /// <summary>
    /// A complete snapshot history of all received snapshots. 
    /// New incoming snapshots from the host will be reconstructed from these.
    /// </summary>
    internal RingBuffer<Snapshot> Snapshots { get; private set; }

    public Client(Peer host)
    {
      this.Host = host;
      this.Snapshots = new RingBuffer<Snapshot>(BUFFER_SIZE);
      this.interpreter = new Interpreter();
      this.lastReceived = Clock.INVALID_FRAME;
    }

    internal void Receive()
    {
      for (int i = 0; i < Client.PAYLOAD_CHOKE; i++)
        if (this.Host.Incoming.Count > 0)
          this.Process(this.Host.Incoming.Dequeue());
    }

    private void Process(byte[] data)
    {
      Snapshot snapshot = this.interpreter.Decode(data, this.Snapshots);
      this.Snapshots.Store(snapshot);
      if (snapshot.Frame > this.lastReceived)
        this.lastReceived = snapshot.Frame;
    }
  }
}
