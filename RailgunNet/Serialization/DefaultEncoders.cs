using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal static class DefaultEncoders
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
      DefaultEncoders.EntityFlagEncoder  = new IntEncoder(0, (int)EntityState.FLAG_ALL);

      DefaultEncoders.UserIdEncoder      = new IntEncoder(0, 4095);
      DefaultEncoders.EntityIdEncoder    = new IntEncoder(0, 65535);
      DefaultEncoders.ArchetypeIdEncoder = new IntEncoder(0, 255);
      DefaultEncoders.StatusEncoder      = new IntEncoder(0, 0xFFF);

      DefaultEncoders.AngleEncoder       = new FloatEncoder(0.0f, 360.0f, 0.1f);
      DefaultEncoders.CoordinateEncoder  = new FloatEncoder(-2048.0f, 2048.0f, 0.01f);
    }
  }
}
