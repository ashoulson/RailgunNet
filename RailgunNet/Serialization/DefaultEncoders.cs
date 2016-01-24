using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal static class Encoders
  {
    internal static IntEncoder   EntityFlagEncoder  = null;

    internal static IntEncoder   UserIdEncoder      = null;
    internal static IntEncoder   EntityIdEncoder    = null;
    internal static IntEncoder   ArchetypeIdEncoder = null;
    internal static IntEncoder   StatusEncoder      = null;

    internal static FloatEncoder CoordinateEncoder  = null;
    internal static FloatEncoder AngleEncoder       = null;

    internal static void Initialize()
    {
      Encoders.EntityFlagEncoder  = new IntEncoder(0, (int)EntityState.FLAG_ALL);

      Encoders.UserIdEncoder      = new IntEncoder(0, 4095);
      Encoders.EntityIdEncoder    = new IntEncoder(0, 65535);
      Encoders.ArchetypeIdEncoder = new IntEncoder(0, 255);
      Encoders.StatusEncoder      = new IntEncoder(0, 0xFFF);

      Encoders.AngleEncoder       = new FloatEncoder(0.0f, 360.0f, 0.1f);
      Encoders.CoordinateEncoder  = new FloatEncoder(-2048.0f, 2048.0f, 0.01f);
    }
  }
}
