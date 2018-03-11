using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using Julo.Util;

namespace Julo.Network
{
    public class DualNetworkManager : NetworkManager
    {
        [Header("DualNetworkManager")]

        public int minPlayers = 2;
        public int maxPlayers = 3;

        public DualGamePlayer mainPlayer = null;
        public bool joinAsSpectator = false;

        public DualClient dualClient;

        [Header("Debug Info")]
        public bool debugEnabled;
        public Text statusInfo;
        public Text hostInfo;

        private enum HostType { None, Host, Client }
        private HostType hostType = HostType.None;
        
        /*** SERVER ***/
        private int maximumSpectatorRole = 100;
        private Dictionary<int, PlayerWrapper> playerMap;
        private List<DualGamePlayer> cachedLocalPlayers;


        /*** CLIENT ***/

        /************* DUAL METHODS *************/

        public void StartAsHost()
        {
            //NetworkServer.RegisterHandler(MsgType.Connect, OnConnected);

            //Debug.LogFormat("Starting host for {0} game player", maxPlayers);
            hostType = HostType.Host;

            cachedLocalPlayers = new List<DualGamePlayer>();
            playerMap = new Dictionary<int, PlayerWrapper>();

            StartHost();
        }

        public void StartAsClient()
        {
            hostType = HostType.Client;
            StartClient();
        }

        // called on server to add already created local players
        public void AddHostedPlayer(/*short playerControllerId, */DualGamePlayer player)
        {
            if(hostType != HostType.Host)
            {
                Debug.LogError("Cannot add hosted player in client mode");
                return;
            }

            player.role = cachedLocalPlayers.Count;

            if(cachedLocalPlayers.Count == 0)
            {
                mainPlayer = player;
            }

            cachedLocalPlayers.Add(player);
        }

        // up one place
        public void PlayerUp(DualGamePlayer player)
        {
            if(player != playerMap[player.role].player)
                Debug.Log("Warning unexpected");

            int role = player.role;
            if(role == 0)
            {
                Debug.LogWarning("Cannot up");
            }
            else if(role - 1 < maxPlayers)
            {
                if(playerMap.ContainsKey(role - 1))
                {
                    SwitchRoles(role, role - 1);
                }
                else
                {
                    ChangeRole(role, role - 1);
                }
            }
            else
            {
                bool done = false;
                for(int i = role - 1; i >= maxPlayers; i--)
                {
                    if(playerMap.ContainsKey(i))
                    {
                        SwitchRoles(role, i);
                        done = true;
                        break;
                    }
                }

                if(!done)
                {
                    if(playerMap.ContainsKey(maxPlayers - 1))
                    {
                        SwitchRoles(role, maxPlayers - 1);
                    }
                    else
                    {
                        ChangeRole(role, maxPlayers - 1);
                    }
                }
            }
        }

        // down one place
        public void PlayerDown(DualGamePlayer player)
        {
            if(player != playerMap[player.role].player)
                Debug.Log("Warning unexpected");

            int role = player.role;

            if(role + 1 < maxPlayers)
            {
                if(playerMap.ContainsKey(role + 1))
                {
                    SwitchRoles(role, role + 1);
                }
                else
                {
                    ChangeRole(role, role + 1);
                }
            }
            else
            {
                bool done = false;
                for(int i = role + 1; i <= maximumSpectatorRole; i++)
                {
                    if(playerMap.ContainsKey(i))
                    {
                        SwitchRoles(role, i);
                        done = true;
                        break;
                    }
                }

                if(!done)
                {
                    ChangeRole(role, maximumSpectatorRole + 1);
                    maximumSpectatorRole++;
                }
            }
        }

        private void ChangeRole(int oldRole, int newRole)
        {
            if(playerMap.ContainsKey(newRole)) {
                Debug.LogError("Occupied");
            } else if(!playerMap.ContainsKey(oldRole)) {
                Debug.LogError("Inexistent");
            } else {
                PlayerWrapper playerWrap = playerMap[oldRole];
                playerMap.Remove(oldRole);

                playerWrap.player.role = newRole;

                playerMap.Add(newRole, playerWrap);
            }
        }


        private void SwitchRoles(int role1, int role2)
        {
            PlayerWrapper player1 = playerMap[role1];
            PlayerWrapper player2 = playerMap[role2];

            playerMap.Remove(role1);
            playerMap.Remove(role2);

            player1.player.role = role2;
            player2.player.role = role1;

            playerMap.Add(role2, player1);
            playerMap.Add(role1, player2);
        }

        /********** MAIN CALLBACKS **********/

        public override void OnStartServer()
        {
            SetServerInfo("Hosting", networkAddress);
        }
        /*
        public override void OnStartClient()
        {
            //
        }
        */
        public override void OnStopServer()
        {
            // TODO cleanup something?
            SetServerInfo("Offline", "");
        }

        /************* SERVER CALLBACKS *************/
        
        /*// called on server when a client just connected
        public override void OnServerConnect(NetworkConnection connectionToClient)
        {
            // WARNING: called two times (at least for local client)
        }*/

        // called on server when a client disconnected
        public override void OnServerDisconnect(NetworkConnection connectionToClient)
        {
            bool playerFound;
            do {
                playerFound = false;
                int keyToRemove = -1;
                foreach(KeyValuePair<int, PlayerWrapper> pair in playerMap)
                {
                    int role = pair.Key;
                    PlayerWrapper playerWrapper = pair.Value;

                    if(playerWrapper == null || playerWrapper.player == null)
                    {
                        Debug.LogErrorFormat("Invalid player");
                    }
                    else if(role != playerWrapper.player.role)
                    {
                        Debug.LogErrorFormat("Wrong role: {0} != {1}", role, playerWrapper.player.role);
                    }
                    else if(playerWrapper.connectionId == connectionToClient.connectionId)
                    {
                        playerFound = true;
                        keyToRemove = role;
                        break;
                    }
                }

                if(playerFound)
                {
                    playerMap.Remove(keyToRemove);
                    // auto up
                }

            } while(playerFound);

            NetworkServer.DestroyPlayersForConnection(connectionToClient);
        }

        // called on server when a client is ready
        public override void OnServerReady(NetworkConnection connectionToClient)
        {
            NetworkServer.SetClientReady(connectionToClient);
        }

        // called on server when a client requests to add a player for it
        // TODO should receive a message with initial name/color
        public override void OnServerAddPlayer(NetworkConnection connectionToClient, short playerControllerId)
        {
            //JuloDebug.Log(string.Format("OnSeverAddPlayer({0}, {1})", connectionToClient.connectionId, playerControllerId)); 
            DualGamePlayer newPlayer = null;

            bool isLocal = (connectionToClient.connectionId == 0);

            if(isLocal)
            { // if the player is local it should be cached
                if(playerControllerId < cachedLocalPlayers.Count)
                {
                    newPlayer = cachedLocalPlayers[playerControllerId];
                }
                else
                {
                    JuloDebug.Err("Player " + playerControllerId + " should be cached");
                }
            }
            else
            { // if is remote should be created
                if(connectionToClient.playerControllers.Count > 0)
                {
                    Debug.LogWarning("Already a player for that connection");
                }
                newPlayer = CreatePlayer(connectionToClient.connectionId, playerControllerId);

                int newRole = -1;

                if(joinAsSpectator)
                {
                    newRole = maximumSpectatorRole + 1;
                }
                else
                {
                    bool assigned = false;
                    
                    // try to enter as player
                    for(int i = 0; !assigned && i < maxPlayers; i++)
                    {
                        if(!playerMap.ContainsKey(i))
                        {
                            newRole = i;
                            assigned = true;
                        }
                    }
                    
                    if(!assigned)
                    {
                        newRole = maximumSpectatorRole + 1;
                    }
                }

                maximumSpectatorRole = Math.Max(maximumSpectatorRole, newRole);

                newPlayer.role = newRole;
            }

            if(newPlayer != null)
            {
                PlayerWrapper wrapper = new PlayerWrapper(connectionToClient.connectionId, playerControllerId, newPlayer);

                //Debug.LogFormat("ADDING {0}: {1}-{2}", newRole, connectionToClient.connectionId, playerControllerId);
                playerMap.Add(newPlayer.role, wrapper);

                newPlayer.gameObject.SetActive(true);
                NetworkServer.AddPlayerForConnection(connectionToClient, newPlayer.gameObject, playerControllerId);
            }
            else
            {
                JuloDebug.Warn("Player could not be added: " + connectionToClient.connectionId + ":" + playerControllerId);
            }
        }

        // called on server when a client requests to remove a player for it
        public override void OnServerRemovePlayer(NetworkConnection connectionToClient, PlayerController player)
        {
            Debug.LogWarning("This is not expected to be called");
            //JuloDebug.Log(string.Format("OnSeverRemovePlayer({0}, {1})", connectionToClient.connectionId, player.playerControllerId));

            NetworkServer.Destroy(player.gameObject);
        }

        // called on server when an error occurs
        public override void OnServerError(NetworkConnection connectionToClient, int errorCode)
        {
            SetServerInfo("Error", string.Format("{0} ({1})", connectionToClient.connectionId, errorCode));
        }

        /************* CLIENT CALLBACKS *************/

        // called on client when connected to a server
        public override void OnClientConnect(NetworkConnection connectionToServer)
        {
            OnClientConnected();
            bool isLocalClient = NetworkServer.active;

            if(isLocalClient)
            {
                //JuloDebug.Log("OnClientConnect: local");
                // add hosted players
                for(int i = 0; i < cachedLocalPlayers.Count; i++)
                {
                    ClientScene.AddPlayer(connectionToServer, (short)i);
                }
            }
            else
            {
                //JuloDebug.Log("OnClientConnect: remote");
                ClientScene.Ready(connectionToServer); // TODO is ready ??

                // ask the server to add player for this remote client
                ClientScene.AddPlayer(connectionToServer, 0);
            }
        }

        // called on client when it disconnects from server
        public override void OnClientDisconnect(NetworkConnection connectionToServer)
        {
            //JuloDebug.Log("Dual::OnClientDisonnect");
            StopClient();
        }

        // called on client when a network error occurs
        public override void OnClientError(NetworkConnection connectionToServer, int errorCode)
        {
            SetServerInfo("Error", string.Format("({0})", errorCode));
        }

        // called on client when told to be not-ready by a server
        public override void OnClientNotReady(NetworkConnection connectionToServer)
        {
            Debug.LogWarning("OnClientNotReady");
        }

        /*********** DUAL CLIENT METHODS ***********/

        public void OnClientPlayerAdded(DualGamePlayer player)
        {
            if(dualClient != null)
                dualClient.OnPlayerAdded(player);
        }

        public void OnClientPlayerRemoved(DualGamePlayer player)
        {
            if(dualClient != null)
                dualClient.OnPlayerRemoved(player);
        }

        public void OnClientRoleChanged(DualGamePlayer player, int oldRole)
        {
            if(dualClient != null)
                dualClient.OnRoleChanged(player, oldRole);
        }

        /**********************/

        /*********** Methods to override ***********/

        protected virtual void OnClientConnected() { }

        protected virtual DualGamePlayer CreatePlayer(int connectionId, short playerControllerId)
        {
            Debug.LogWarning("This should be overriden");

            if(playerPrefab == null)
                return null;
            
            GameObject playerObj = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            return playerObj.GetComponent<DualGamePlayer>();
        }

        /*********** MISC ***********/

        public void SetServerInfo(string status, string host)
        {
            if(statusInfo != null)
                statusInfo.text = status;

            if(hostInfo != null)
                hostInfo.text = host;
        }
    }
}
