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

using CommonTools;

namespace Railgun
{

  /// <summary>
  /// Packet sent from client to server
  /// </summary>
  internal class RailClientPacket : IRailPoolable, IRailRingValue
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    int IRailRingValue.Tick { get { return this.Tick; } }

    internal int Tick { get; private set; }
    internal int LastAckedTick { get; private set; }
    internal IEnumerable<RailCommand> Commands { get { return this.commands; } }

    private readonly List<RailCommand> commands;

    public RailClientPacket()
    {
      this.commands = new List<RailCommand>();
      this.Reset();
    }

    public void Initialize(
      int tick, 
      int lastAcked, 
      IEnumerable<RailCommand> commands)
    {
      this.Tick = tick;
      this.LastAckedTick = lastAcked;
      this.commands.Clear();
      this.commands.AddRange(commands);
    }

    protected void Reset()
    {
      this.Tick = RailClock.INVALID_TICK;
      this.LastAckedTick = RailClock.INVALID_TICK;

      foreach (RailCommand command in this.commands)
        RailPool.Free(command);
      this.commands.Clear();
    }

    #region Encode/Decode
    /// Packet encoding order: | TICK | LASTACKED | COMMAND COUNT | COMMAND | COMMAND | ... |
    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Commands]
      foreach (RailCommand command in this.commands)
        command.Encode(buffer);

      // Write: [Command Count]
      buffer.Push(StandardEncoders.CommandCount, this.commands.Count);

      // Write: [LastAcked]
      buffer.Push(StandardEncoders.Tick, this.LastAckedTick);

      // Write: [Tick]
      buffer.Push(StandardEncoders.Tick, this.Tick);
    }

    internal static RailClientPacket Decode(
      BitBuffer buffer)
    {
      RailClientPacket packet = RailResource.Instance.AllocateClientPacket();

      // Read: [Tick]
      packet.Tick = buffer.Pop(StandardEncoders.Tick);

      // Read: [LastAcked]
      packet.LastAckedTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [Command Count]
      int commandCount = buffer.Pop(StandardEncoders.CommandCount);

      // Read: [Commands]
      for (int i = 0; i < commandCount; i++)
      {
        RailCommand command = RailCommand.Decode(buffer);
        packet.commands.Add(command);
      }

      return packet;
    }
    #endregion
  }
}
