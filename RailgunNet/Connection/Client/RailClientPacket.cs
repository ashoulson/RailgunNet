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
#if SERVER
  interface IRailClientPacket
  {
    IEnumerable<RailCommandUpdate> CommandUpdates { get; }
  }
#endif

  /// <summary>
  /// Packet sent from client to server.
  /// </summary>
  internal class RailClientPacket 
    : RailPacket
#if SERVER
    , IRailClientPacket
#endif
  {
    #region Interface
#if SERVER
    IEnumerable<RailCommandUpdate> IRailClientPacket.CommandUpdates { get { return this.commandUpdates.Received; } }
#endif
    #endregion

#if SERVER
    internal RailView View { get { return this.view; } }
#endif
#if CLIENT
    internal IEnumerable<RailCommandUpdate> Sent { get { return this.commandUpdates.Sent; } }
#endif

    private readonly RailView view;
    private readonly RailPackedListC2S<RailCommandUpdate> commandUpdates;

    public RailClientPacket()
      : base()
    {
      this.view = new RailView();
      this.commandUpdates = new RailPackedListC2S<RailCommandUpdate>();
    }

    internal override void Reset()
    {
      base.Reset();

      this.view.Clear();
      this.commandUpdates.Clear();
    }

#if CLIENT
    internal void Populate(
      IEnumerable<RailCommandUpdate> commandUpdates,
      RailView view)
    {
      this.commandUpdates.AddPending(commandUpdates);
      this.view.Integrate(view);
    }
#endif

    #region Encode/Decode
    protected override void EncodePayload(
      RailBitBuffer buffer, 
      int reservedBytes)
    {
#if CLIENT
      // Write: [Commands]
      this.EncodeCommands(buffer);

      // Write: [View]
      this.EncodeView(buffer, reservedBytes);
    }

    protected void EncodeCommands(RailBitBuffer buffer)
    {
      this.commandUpdates.Encode(
        buffer,
        RailConfig.PACKCAP_COMMANDS,
        RailConfig.MAXSIZE_COMMANDUPDATE,
        (commandUpdate) => commandUpdate.Encode(buffer));
    }

    protected void EncodeView(RailBitBuffer buffer, int reservedBytes)
    {
      buffer.PackToSize(
        RailConfig.PACKCAP_MESSAGE_TOTAL - reservedBytes,
        int.MaxValue,
        this.view.GetOrdered(),
        (pair) =>
        {
          buffer.WriteEntityId(pair.Key);        // Write: [EntityId]
          buffer.WriteTick(pair.Value.Tick);     // Write: [Tick]
          buffer.WriteBool(pair.Value.IsFrozen); // Write: [IsFrozen]
        });
#endif
    }

    protected override void DecodePayload(RailBitBuffer buffer)
    {
#if SERVER
      // Read: [Commands]
      this.DecodeCommands(buffer);

      // Read: [View]
      this.DecodeView(buffer);
    }

    protected void DecodeCommands(RailBitBuffer buffer)
    {
      this.commandUpdates.Decode(
        buffer,
        () => RailCommandUpdate.Decode(buffer));
    }

    public void DecodeView(RailBitBuffer buffer)
    {
      IEnumerable<KeyValuePair<EntityId, RailViewEntry>> decoded =
        buffer.UnpackAll(
          () =>
          {
            return new KeyValuePair<EntityId, RailViewEntry>(
              buffer.ReadEntityId(),  // Read: [EntityId] 
              new RailViewEntry(
                buffer.ReadTick(),    // Read: [Tick]
                buffer.ReadBool()));  // Read: [IsFrozen]
          });

      foreach (var pair in decoded)
        this.view.RecordUpdate(pair.Key, pair.Value);
#endif
    }
    #endregion
  }
}
