using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public struct EntityState
  {
    #region Flags
    private const uint  FLAG_USER_ID        = 0x01;
    private const uint  FLAG_ENTITY_ID      = 0x02;
    private const uint  FLAG_ARCHETYPE_ID   = 0x04;
    private const uint  FLAG_X              = 0x08;
    private const uint  FLAG_Y              = 0x10;
    private const uint  FLAG_ANGLE          = 0x20;
    private const uint  FLAG_STATUS         = 0x40;

    internal const uint FLAG_ALL            = 0x7F;
    #endregion

    private int userId;
    private int entityId;
    private int archetypeId;

    private float x;
    private float y;
    private float angle;

    private uint status;
  }
}
