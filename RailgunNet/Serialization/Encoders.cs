using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public static class Encoders
  {
    internal static int PushIf<T>(
      BitPacker bitPacker,
      bool condition,
      T value,
      IEncoder<T> encoder,
      int flag)
    {
      if (condition)
      {
        bitPacker.Push(value, encoder);
        return flag;
      }
      return 0;
    }

    internal static T PopIf<T>(
      BitPacker bitPacker,
      int flags,
      int requiredFlag,
      IEncoder<T> encoder,
      T basisVal)
    {
      if ((flags & requiredFlag) == requiredFlag)
        return bitPacker.Pop(encoder);
      return basisVal;
    }

    internal static IntEncoder   EntityFlag  = null;

    internal static IntEncoder   UserId      = null;
    internal static IntEncoder   EntityId    = null;
    internal static IntEncoder   ArchetypeId = null;
    internal static IntEncoder   Status      = null;

    internal static FloatEncoder Coordinate  = null;
    internal static FloatEncoder Angle       = null;

    public static void Initialize()
    {
      Encoders.EntityFlag  = new IntEncoder(0, (int)EntityState.FLAG_ALL);

      Encoders.UserId      = new IntEncoder(0, 4095);
      Encoders.EntityId    = new IntEncoder(0, 65535);
      Encoders.ArchetypeId = new IntEncoder(0, 255);
      Encoders.Status      = new IntEncoder(0, 0xFFF);

      Encoders.Angle       = new FloatEncoder(0.0f, 360.0f, 0.1f);
      Encoders.Coordinate  = new FloatEncoder(-2048.0f, 2048.0f, 0.01f);
    }
  }
}
