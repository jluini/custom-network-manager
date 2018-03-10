using UnityEngine;
using System.Collections;

namespace Julo.Network
{
    public abstract class DualClient : MonoBehaviour
    {

        public abstract void OnPlayerAdded(DualGamePlayer newPlayer);
        public abstract void OnPlayerRemoved(DualGamePlayer player);
        public abstract void OnRoleChanged(DualGamePlayer player, int oldRole);
        public abstract void OnRoomSizeChanged(int newSize);

    }
}
