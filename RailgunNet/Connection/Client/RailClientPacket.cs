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
  /// Packet sent from client to server.
  /// </summary>
  public class RailClientPacket : RailPacket, IRailPoolable
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }

    /// <summary>
    /// A brief history of commands from the client. May not be sent in order.
    /// </summary>
    internal IEnumerable<RailCommand> Commands { get { return this.commands; } }

    /// <summary>
    /// A (partial) view indicating the last update frame for each entity.
    /// </summary>
    internal RailView View { get { return this.view; } }

    private readonly List<RailCommand> commands;
    private readonly RailView view;

    public RailClientPacket() : base()
    {
      this.view = new RailView();
      this.commands = new List<RailCommand>();

      this.Reset();
    }

    internal void InitializeClient(
      IEnumerable<RailCommand> commands,
      RailView view)
    {
      this.AddCommands(commands);
      this.view.Integrate(view);
    }

    protected override void Reset()
    {
      base.Reset();

      // We don't free the commands because they will be stored elsewhere
      this.commands.Clear();
      this.view.Clear();
    }

    private void AddCommands(IEnumerable<RailCommand> commands)
    {
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
      // Write: [Header]
      this.EncodeHeader(buffer);

      // Write: [Commands]
      this.EncodeCommands(buffer);

      // Write: [View]
      this.view.Encode(buffer);

      // Write: [Events]
      this.EncodeEvents(buffer);
    }

    internal static RailClientPacket Decode(
      BitBuffer buffer)
    {
      RailClientPacket packet = RailResource.Instance.AllocateClientPacket();

      // Read: [Header]
      packet.DecodeHeader(buffer);

      // Read: [Commands]
      packet.DecodeCommands(buffer);

      // Read: [View]
      packet.view.Decode(buffer);

      // Read: [Events]
      packet.DecodeEvents(buffer);

      return packet;
    }

    #region Commands
    protected void EncodeCommands(BitBuffer buffer)
    {
      // Write: [CommandCount]
      buffer.Write(RailEncoders.CommandCount, this.commands.Count);

      // Write: [Commands]
      foreach (RailCommand command in this.commands)
        command.Encode(buffer);
    }

    protected void DecodeCommands(BitBuffer buffer)
    {
      // Read: [CommandCount]
      int commandCount = buffer.Read(RailEncoders.CommandCount);

      // Read: [Commands]
      for (int i = 0; i < commandCount; i++)
        this.commands.Add(RailCommand.Decode(buffer));
    }
    #endregion

    #endregion
  }
}
