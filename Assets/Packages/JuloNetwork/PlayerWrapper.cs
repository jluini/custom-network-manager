using System;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Network
{
    public class PlayerWrapper
    {
        public int connectionId = -1;
        public short playerControllerId = -1;
        public DualGamePlayer player;

        public PlayerWrapper(int connectionId, short playerControllerId, DualGamePlayer player)
        {
            this.connectionId = connectionId;
            this.playerControllerId = playerControllerId;
            this.player = player;
        }
    }
}

