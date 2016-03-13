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

  /// <summary>
  /// Packet sent from client to server
  /// </summary>
  internal class RailClientPacket : IRailPoolable
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }

    internal Tick ClientTick { get; private set; }
    internal Tick LastReceivedServerTick { get; private set; }
    internal EventId LastReceivedEventId { get; private set; }
    internal IEnumerable<RailCommand> Commands { get { return this.commands; } }
    internal RailView View { get { return this.view; } }

    private readonly RailView view;
    private readonly List<RailCommand> commands;

    public RailClientPacket()
    {
      this.commands = new List<RailCommand>();
      this.view = new RailView();
      this.Reset();
    }

    public void Initialize(
      Tick tick,
      Tick lastReceivedServerTick,
      EventId lastReceivedEventId,
      IEnumerable<RailCommand> commands,
      RailView view)
    {
      this.ClientTick = tick;
      this.LastReceivedServerTick = lastReceivedServerTick;
      this.LastReceivedEventId = lastReceivedEventId;
      this.AddCommands(commands);
      this.view.Integrate(view);
    }

    protected void Reset()
    {
      this.ClientTick = Tick.INVALID;
      this.LastReceivedServerTick = Tick.INVALID;

      // Note: We don't free the commands because they 
      // will always be stored elsewhere
      this.commands.Clear();
      this.view.Clear();
    }

    private void AddCommands(IEnumerable<RailCommand> commands)
    {
      this.commands.Clear();
      foreach (RailCommand command in commands.Reverse())
      {
        if (this.commands.Count >= RailConfig.COMMAND_SEND_COUNT)
          break;
        this.commands.Add(command);
      }
    }

    #region Encode/Decode
    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [View]
      this.view.Encode(buffer);

      // Write: [Commands]
      foreach (RailCommand command in this.commands)
        command.Encode(buffer);

      // Write: [CommandCount]
      buffer.Push(RailEncoders.CommandCount, this.commands.Count);

      // Write: [LastReceivedEventId]
      buffer.Push(RailEncoders.EventId, this.LastReceivedEventId);

      // Write: [LastReceivedServerTick]
      buffer.Push(RailEncoders.Tick, this.LastReceivedServerTick);

      // Write: [Tick]
      buffer.Push(RailEncoders.Tick, this.ClientTick);
    }

    internal static RailClientPacket Decode(
      BitBuffer buffer)
    {
      RailClientPacket packet = RailResource.Instance.AllocateClientPacket();

      // Read: [Tick]
      packet.ClientTick = buffer.Pop(RailEncoders.Tick);

      // Read: [LastReceivedServerTick]
      packet.LastReceivedServerTick = buffer.Pop(RailEncoders.Tick);

      // Read: [LastReceivedEventId]
      packet.LastReceivedEventId = buffer.Pop(RailEncoders.EventId);

      // Read: [CommandCount]
      int commandCount = buffer.Pop(RailEncoders.CommandCount);

      // Read: [Commands]
      for (int i = 0; i < commandCount; i++)
      {
        RailCommand command = RailCommand.Decode(buffer);
        packet.commands.Add(command);
      }

      // Read: [View]
      packet.view.Decode(buffer);

      return packet;
    }
    #endregion
  }
}
