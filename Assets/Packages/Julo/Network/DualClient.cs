using UnityEngine;
using System.Collections;

namespace Julo.Network
{
    public interface DualClient
    {
        
        void OnPlayerAdded(DualGamePlayer newPlayer);
        void OnPlayerRemoved(DualGamePlayer player);
        void OnRoleChanged(DualGamePlayer player, int oldRole);
        void OnRoomSizeChanged(int minPlayers, int maxPlayers);

    }
}
