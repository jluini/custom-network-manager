
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using Julo.Util;


namespace Julo.Network
{
    // <summary>
    // Represents a networked player.
    // Each player has a "role" in the game. Spectators have role = -1,
    // and roles starting form 0 are given to each actual player of the game.
    // </summary>
    public class DualGamePlayer : NetworkBehaviour {

        [SyncVar(hook="OnRoleChanged")]
        public int role= -1;

        private DualNetworkManager manager {
            get {
                return (DualNetworkManager)NetworkManager.singleton;
            }
        }

        public override void OnStartLocalPlayer()
        {
            if(isServer != NetworkServer.active)
                Debug.LogError("Wrong 3");
            
            if(!isServer)
            {
                if(manager.mainPlayer == null)
                {
                    manager.mainPlayer = this;
                }
                else
                {
                    Debug.LogWarning("Already a remote main player here");
                }
            }
        }

        public override void OnStartClient()
        {
            DualNetworkManager manager = (DualNetworkManager)NetworkManager.singleton;
            //Debug.Log("Starting player client");

            manager.OnClientPlayerAdded(this);
        }

        private void OnRoleChanged(int newRole)
        {
            int oldRole = this.role;
            this.role = newRole;

            // don't call this if the client is not setup (will call OnStartClient and then OnClienPlayerAdded)
            if(isClient)
            {
                //Debug.LogFormat("Role changed {0} -> {1}", oldRole, newRole);
                manager.OnClientRoleChanged(this, oldRole);
            }
            else
            {
                // Debug.LogWarning("Not spawned");
            }

            OnPlayerChanged();
        }

        public virtual void OnPlayerChanged() { }

        // called on client when object destroyed by server
        public override void OnNetworkDestroy()
        {
            base.OnNetworkDestroy();
            ///*Julo*/Debug.Log(string.Format("{0} is network destroyed", netId));

            if(role >= 0)
            {
                manager.OnClientPlayerRemoved(this);
            }
        }
    }
}
