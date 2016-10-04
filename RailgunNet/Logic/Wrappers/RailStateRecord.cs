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

namespace Railgun
{
  /// <summary>
  /// Used to differentiate/typesafe state records. Not strictly necessary.
  /// </summary>
  internal class RailStateRecord 
    : IRailTimedValue
    , IRailPoolable<RailStateRecord>
  {
    #region Pooling
    IRailPool<RailStateRecord> IRailPoolable<RailStateRecord>.Pool { get; set; }
    void IRailPoolable<RailStateRecord>.Reset() { this.Reset(); }
    #endregion

    #region Interface
    Tick IRailTimedValue.Tick { get { return this.tick; } }
    #endregion

    internal RailState State { get { return this.state; } }
    internal Tick Tick { get { return this.tick; } }

    private Tick tick;
    private RailState state;

    public RailStateRecord()
    {
      this.state = null;
      this.tick = Tick.INVALID;
    }

    public void Initialize(
      Tick tick,
      RailState state)
    {
      this.state = state;
      this.tick = tick;
    }

    public void Overwrite(
      Tick tick,
      RailState state)
    {
      this.tick = tick;
      this.state.OverwriteFrom(state);
    }

    private void Reset()
    {
      RailPool.SafeReplace(ref this.state, null);
      this.tick = Tick.INVALID;
    }
  }
}
