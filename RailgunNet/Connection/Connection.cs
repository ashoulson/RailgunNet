using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class Connection
  {
    public abstract event Action<byte[]> Received;
    public abstract void Send(byte[] payload);
  }
}
