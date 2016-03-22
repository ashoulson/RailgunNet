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
    #region Interpreter
    IRailPool<RailClientPacket> IRailPoolable<RailClientPacket>.Pool { get; set; }
    void IRailPoolable<RailClientPacket>.Reset() { this.Reset(); }
    #endregion

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
      // We reverse the list to take the latest commands until we reach
      // capacity. This is also compatible with LIFO packing, so they will
      // arrive in order when packed. (Plus they're just dejittered anyway.)
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
    protected override void EncodePayload(ByteBuffer buffer)
    {
      // Write: [Commands]
      this.EncodeCommands(buffer);

      // Write: [View]
      this.EncodeView(buffer);
    }

    protected override void DecodePayload(ByteBuffer buffer)
    {
      // Read: [Commands]
      this.DecodeCommands(buffer);

      // Read: [View]
      this.DecodeView(buffer);
    }

    #region Commands
    protected void EncodeCommands(ByteBuffer buffer)
    {
      buffer.PackAll(
        this.commands,
        (command) => command.Encode(buffer));
    }

    protected void DecodeCommands(ByteBuffer buffer)
    {
      IEnumerable<RailCommand> decoded =
        buffer.UnpackAll(
          () => RailCommand.Decode(buffer));
      this.commands.AddRange(decoded);
    }
    #endregion

    #region View
    protected void EncodeView(ByteBuffer buffer)
    {
      buffer.PackToSize(
        RailConfig.MESSAGE_MAX_SIZE,
        int.MaxValue,
        this.view.GetOrdered(),
        (pair) =>
        {
          buffer.WriteEntityId(pair.Key); // Write: [EntityId]
          buffer.WriteTick(pair.Value);   // Write: [Tick]
        });
    }

    public void DecodeView(ByteBuffer buffer)
    {
      IEnumerable<KeyValuePair<EntityId, Tick>> decoded =
        buffer.UnpackAll(
          () =>
          {
            return new KeyValuePair<EntityId, Tick>(
              buffer.ReadEntityId(),  // Read: [EntityId] 
              buffer.ReadTick());     // Read: [Tick]
          });

      foreach (var pair in decoded)
        this.view.RecordUpdate(pair.Key, pair.Value);
    }
    #endregion
    #endregion
  }
}
