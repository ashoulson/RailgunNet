/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System.Collections.Generic;

namespace Railgun
{
  internal class RailCommandUpdate
    : IRailPoolable<RailCommandUpdate>
  {
    private const int BUFFER_CAPACITY = 
      RailConfig.COMMAND_SEND_COUNT;
    private static readonly int BUFFER_COUNT_BITS =
      RailUtil.Log2(RailCommandUpdate.BUFFER_CAPACITY) + 1;

    #region Pooling
    IRailPool<RailCommandUpdate> IRailPoolable<RailCommandUpdate>.Pool { get; set; }
    void IRailPoolable<RailCommandUpdate>.Reset() { this.Reset(); }
    #endregion

    internal static RailCommandUpdate Create(
      EntityId entityId, 
      IEnumerable<RailCommand> commands)
    {
      RailCommandUpdate update = RailResource.Instance.CreateCommandUpdate();
      update.Initialize(entityId, commands);
      return update;
    }

#if CLIENT
    internal RailEntity Entity { get; set; }
#endif

    internal EntityId EntityId { get { return this.entityId; } }
    internal IEnumerable<RailCommand> Commands { get { return this.commands.GetValues(); } }

    private EntityId entityId;
    private readonly RailRollingBuffer<RailCommand> commands;

    public RailCommandUpdate()
    {
      this.entityId = EntityId.INVALID;
      this.commands = 
        new RailRollingBuffer<RailCommand>(
          RailCommandUpdate.BUFFER_CAPACITY);
    }

    private void Initialize(
      EntityId entityId,
      IEnumerable<RailCommand> outgoingCommands)
    {
      this.entityId = entityId;
      foreach (RailCommand command in outgoingCommands)
        this.commands.Store(command);
    }

    private void Reset()
    {
      this.entityId = EntityId.INVALID;
      this.commands.Clear();
    }

#if CLIENT
    internal void Encode(RailBitBuffer buffer)
    {
      // Write: [EntityId]
      buffer.WriteEntityId(this.entityId);

      // Write: [Count]
      buffer.Write(BUFFER_COUNT_BITS, (uint)this.commands.Count);

      // Write: [Commands]
      foreach (RailCommand command in this.commands.GetValues())
        command.Encode(buffer);
    }
#endif

#if SERVER
    internal static RailCommandUpdate Decode(RailBitBuffer buffer)
    {
      RailCommandUpdate update = RailResource.Instance.CreateCommandUpdate();

      // Read: [EntityId]
      update.entityId = buffer.ReadEntityId();

      // Read: [Count]
      int count = (int)buffer.Read(BUFFER_COUNT_BITS);

      // Read: [Commands]
      for (int i = 0; i < count; i++)
        update.commands.Store(RailCommand.Decode(buffer));

      return update;
    }
#endif
  }
}
