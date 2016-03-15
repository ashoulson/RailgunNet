using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  interface IRailServerPacket : IRailPacket
  {
    IEnumerable<RailState> States { get; }
  }
}
