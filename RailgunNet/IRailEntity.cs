/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
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
  public interface IRailEntity
  {
    /// <summary>
    /// Used internally within Railgun to downcast.
    /// </summary>
    RailEntity AsBase { get; }

    RailRoom Room { get; }
    bool IsRemoving { get; }
    bool IsFrozen { get; }
    RailController Controller { get; }

    EntityId Id { get; }

#if CLIENT
    bool IsControlled { get; }

    /// <summary>
    /// The tick of the last authoritative state.
    /// </summary>
    Tick AuthTick { get; }

    /// <summary>
    /// The tick of the next authoritative state. May be invalid.
    /// </summary>
    Tick NextTick { get; }

    /// <summary>
    /// Returns the number of ticks ahead we are, for extrapolation.
    /// Note that this does not take client-side prediction into account.
    /// </summary>
    int TicksAhead { get; }
#endif

#if CLIENT
    float ComputeInterpolation(float tickDeltaTime, float timeSinceTick);
#endif
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state.
  /// </summary>
  public interface IRailEntity<TState> : IRailEntity
    where TState : RailState, new()
  {
    TState State { get; }

#if CLIENT
    TState AuthState { get; }
    TState NextState { get; }
#endif
  }
}
