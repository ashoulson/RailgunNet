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

using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

using CommonTools;

namespace Railgun
{
  interface IRailClientPacket
  {
  }

  /// <summary>
  /// Packet sent from client to server.
  /// </summary>
  internal class RailClientPacket :
    RailPacket, IRailClientPacket, IRailPoolable<RailClientPacket>
  {
    // String hashes (md5):
    private const int KEY_RESERVE = 0x509B1C56;
    private const int KEY_ROLLBACK = 0x42A09755;

    IRailPool<RailClientPacket> IRailPoolable<RailClientPacket>.Pool { get; set; }
    void IRailPoolable<RailClientPacket>.Reset() { this.Reset(); }

    #region Data Read
    /// <summary>
    /// A brief history of commands from the client. May not be sent in order.
    /// </summary>
    public IEnumerable<RailCommand> Commands
    {
      get { return this.commands; }
    }

    /// <summary>
    /// A (partial) view indicating the last update frame for each entity.
    /// </summary>
    public RailView View
    {
      get { return this.view; }
    }
    #endregion

    #region Data Write
    public void AddCommands(IEnumerable<RailCommand> commands)
    {
      // The commands are sent in backwards order, but that's fine because
      // they're all just being put in the dejitter buffer anyway
      foreach (RailCommand command in commands.Reverse())
      {
        if (this.commands.Count >= RailConfig.COMMAND_SEND_COUNT)
          break;
        this.commands.Add(command);
      }
    }

    public void AddView(RailView view)
    {
      this.view.Integrate(view);
    }
    #endregion

    private readonly List<RailCommand> commands;
    private readonly RailView view;

    public RailClientPacket()
      : base()
    {
      this.view = new RailView();
      this.commands = new List<RailCommand>();
    }

    protected override void Reset()
    {
      base.Reset();

      this.commands.Clear();
      this.view.Clear();
    }

    #region Encode/Decode
    protected override void EncodePayload(
      ByteBuffer buffer)
    {
      // Write: [View]
      this.EncodeView(buffer);

      // Write: [Commands]
      this.EncodeCommands(buffer);
    }

    protected override void DecodePayload(
      ByteBuffer buffer,
      IRailLookup<EntityId, RailEntity> entityLookup)
    {
      // Read: [Commands]
      this.DecodeCommands(buffer);

      // Read: [View]
      this.DecodeView(buffer);
    }

    #region Commands
    protected void EncodeCommands(ByteBuffer buffer)
    {
      // Write: [Commands]
      foreach (RailCommand command in this.commands)
        command.Encode(buffer);

      // Write: [CommandCount] TODO: BYTE
      buffer.WriteInt(this.commands.Count);
    }

    protected void DecodeCommands(ByteBuffer buffer)
    {
      // Read: [CommandCount]
      int commandCount = buffer.ReadInt();

      // Read: [Commands]
      for (int i = 0; i < commandCount; i++)
        this.commands.Add(RailCommand.Decode(buffer));
    }
    #endregion

    #region View
    protected void EncodeView(ByteBuffer buffer)
    {
      int count = 0;
      foreach (KeyValuePair<EntityId, Tick> pair in this.view.GetOrdered())
      {
        // Write: [Tick]
        buffer.WriteTick(pair.Value);

        // Write: [EntityId]
        buffer.WriteEntityId(pair.Key);

        count++;
      }

      // Write: [Count]
      buffer.WriteInt(count);
    }


    public void DecodeView(ByteBuffer buffer)
    {
      // Read: [Count]
      int count = buffer.ReadInt();

      for (int i = 0; i < count; i++)
      {
        // Read: [EntityId]
        EntityId id = buffer.ReadEntityId();

        // Read: [Tick]
        Tick tick = buffer.ReadTick();

        this.view.RecordUpdate(id, tick);
      }
    }
    #endregion
    #endregion
  }
}
