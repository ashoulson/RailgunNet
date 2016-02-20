using System;
namespace UnityEngineInternal
{
  public struct MathfInternal
  {
    public static volatile float FloatMinNormal = 1.17549435E-38f;
    public static volatile float FloatMinDenormal = 1.401298E-45f;
    public static bool IsFlushToZeroEnabled = MathfInternal.FloatMinDenormal == 0f;
  }
}