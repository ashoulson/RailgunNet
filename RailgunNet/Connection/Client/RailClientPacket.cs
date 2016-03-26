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

namespace Railgun
{
#if SERVER
  interface IRailClientPacket
  {
  }
#endif

  /// <summary>
  /// Packet sent from client to server.
  /// </summary>
  internal class RailClientPacket 
    : RailPacket
    , IRailPoolable<RailClientPacket>
#if SERVER
    , IRailClientPacket
#endif
  {
    internal static RailClientPacket Create()
    {
      return RailResource.Instance.CreateClientPacket();
    }

    #region Interface
    IRailPool<RailClientPacket> IRailPoolable<RailClientPacket>.Pool { get; set; }
    void IRailPoolable<RailClientPacket>.Reset() { this.Reset(); }
    #endregion

#if SERVER
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
#endif

#if CLIENT
    public void AddCommands(IEnumerable<RailCommand> commands)
    {
      // We reverse the list to take the latest commands until we reach
      // capacity. Order doesn't matter on arrival since they go into a
      // dejitter buffer on the server's end.
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
#endif

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
    protected override void EncodePayload(BitBuffer buffer)
    {
#if CLIENT
      // Write: [Commands]
      this.EncodeCommands(buffer);

      // Write: [View]
      this.EncodeView(buffer);
    }

    protected void EncodeCommands(BitBuffer buffer)
    {
      buffer.PackAll(
        this.commands,
        (command) => command.Encode(buffer));
    }

    protected void EncodeView(BitBuffer buffer)
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
#endif
    }

    protected override void DecodePayload(BitBuffer buffer)
    {
#if SERVER
      // Read: [Commands]
      this.DecodeCommands(buffer);

      // Read: [View]
      this.DecodeView(buffer);
    }

    protected void DecodeCommands(BitBuffer buffer)
    {
      IEnumerable<RailCommand> decoded =
        buffer.UnpackAll(
          () => RailCommand.Decode(buffer));
      this.commands.AddRange(decoded);
    }

    public void DecodeView(BitBuffer buffer)
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
#endif
    }
    #endregion
  }
}
