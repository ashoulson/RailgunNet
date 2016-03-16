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

namespace Railgun
{
  public class RailConfig
  {
    /// <summary>
    /// Number of outgoing commands to send per packet.
    /// </summary>
    internal const int COMMAND_SEND_COUNT = 4;

    /// <summary>
    /// Number of commands to buffer for prediction.
    /// </summary>
    internal const int COMMAND_BUFFER_COUNT = 50;

    /// <summary>
    /// The real time in seconds per simulation tick.
    /// </summary>
    internal const float FIXED_DELTA_TIME = 0.02f;

    /// <summary>
    /// Network send rate in frames/packet.
    /// </summary>
    internal const int NETWORK_SEND_RATE = 2;

    /// <summary>
    /// Number of entries to store in a dejitter buffer.
    /// </summary>
    internal const int DEJITTER_BUFFER_LENGTH = 50;

    /// <summary>
    /// Data buffer size used for packet I/O. 
    /// Don't change this without a good reason.
    /// </summary>
    internal const int DATA_BUFFER_SIZE = 2048;

    /// <summary>
    /// The maximum message size that a packet can contain, based on known
    /// MTUs for internet traffic. Don't change this without a good reason.
    /// </summary>
    internal const int MAX_MESSAGE_SIZE = 1400;

    #region Encoding Parameters
    /// <summary>
    /// Maximum number of entities supported. For best results. Make this
    /// one less than a power of two (i.e. 64 -> 63, 1024 -> 1023, etc.).
    /// </summary>
    internal const int MAX_ENTITY_COUNT = 4094;

    /// <summary>
    /// Maximum tick available. Make this two less than a power of two.
    /// </summary>
    internal const int MAX_TICK = 1048573; // 5.8hrs at 20Hz
    #endregion
  }
}
