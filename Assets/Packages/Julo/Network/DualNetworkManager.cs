using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

using Julo.Util;

namespace Julo.Network
{
    public class DualNetworkManager : NetworkManager, DualClient
    {
        [Header("DualNetworkManager")]

        // TODO remove
        //public int minPlayers = 2;
        //public int maxPlayers = 3;

        public DualGamePlayer mainPlayer = null;
        public bool joinAsSpectator = false;

        [Header("Debug Info")]
        public bool debugEnabled;
        public Text statusInfo;
        public Text hostInfo;

        protected enum DNMState { Off, Host, Connecting, Client }
        protected DNMState state = DNMState.Off;
        private List<DualGamePlayer> cachedLocalPlayers = new List<DualGamePlayer>();

        // TODO synchronize
        public string currentSceneName;
        public int currentMinPlayers;
        public int currentMaxPlayers;
        public int currentSize;

        /*** SERVER ***/
        public SceneData currentScene = null;

        private int maximumSpectatorRole = 100;
        private Dictionary<int, PlayerWrapper> playerMap;
        //private numPlayers = 0;

        private enum GameState { NoGame, LoadingGame, Playing }
        private GameState gameState = GameState.NoGame;

        // TODO is necessary?
        private Dictionary<int, PlayerWrapper> gamePlayers;

        private string mainPlayerName;

        /*public void SetMainPlayerName(string newName)
        {
            mainPlayerName = newName;
        }*/

        /************* DUAL METHODS *************/


        public bool StartAsHost(SceneData initialScene, MatchInfo hostInfo = null)
        {
            if(state != DNMState.Off)
            {
                Debug.Log("Invalid call of StartAsHost");
                return false;
            }

            if(cachedLocalPlayers.Count == 0)
                Debug.LogWarning("Hosting with no local players");

            playerMap = new Dictionary<int, PlayerWrapper>();
                
            NetworkClient localClient;
            if(hostInfo == null)
                localClient = StartHost();
            else
                localClient = StartHost(hostInfo);

            bool hostStarted = localClient != null;

            if(hostStarted)
            {
                state = DNMState.Host;

                if(gameState != GameState.NoGame)
                {
                    Debug.LogWarning("Already in game mode");
                    gameState = GameState.NoGame;
                }

                SetServerInfo("Hosting", networkAddress);

                currentScene = initialScene;
                // synchronized variables:
                currentSceneName  = currentScene.englishName;
                currentMinPlayers = currentScene.minPlayers;
                currentMaxPlayers = currentScene.maxPlayers;
                currentSize = currentScene.defaultSize;
            }
            else
            {
                cachedLocalPlayers.Clear();
            }

            return hostStarted;
        }

        public void StartAsClient(MatchInfo hostInfo = null)
        {
            if(state != DNMState.Off)
            {
                Debug.LogError("Invalid state");
                return;
            }

            state = DNMState.Connecting;
            SetServerInfo("Connecting", "");

            if(hostInfo != null)
                StartClient(hostInfo);
            else
                StartClient();
        }

        // called on server to add already created local players
        public void AddHostedPlayer(/*short playerControllerId, */DualGamePlayer player)
        {
            if(state != DNMState.Off)
            {
                Debug.LogError("Invalid state");
                return;
            }

            player.role = cachedLocalPlayers.Count;

            if(cachedLocalPlayers.Count == 0)
            {
                mainPlayer = player;
            }

            cachedLocalPlayers.Add(player);
        }

        // stop host / client / connection attempt
        public void Stop()
        {
            if(state == DNMState.Host)
            {
                this.StopHost();
                state = DNMState.Off;
                this.cachedLocalPlayers.Clear();
            }
            else if(state == DNMState.Connecting || state == DNMState.Client)
            {
                this.StopClient();
                state = DNMState.Off;
            }
            else
            {
                Debug.LogError("Invalid state");
            }
        }

        public bool ThereIsEnoughPlayers()
        {
            if(state != DNMState.Host)
            {
                Debug.LogError("Invalid state");
                return false;
            }
            if(gameState != GameState.NoGame)
            {
                Debug.LogError("Invalid state");
                return false;
            }

            int numPlayers = 0;

            for(int i = 0; i < currentMaxPlayers; i++)
            {
                if(playerMap.ContainsKey(i))
                {
                    PlayerWrapper wrapper = playerMap[i];
                    if(wrapper != null && wrapper.player != null/* && wrapper.player.role == i*/)
                    {
                        numPlayers++;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Invalid player: {0}.({1})({2})({3})", i, wrapper != null, wrapper.player != null, wrapper.player.role);
                    }
                }
            }

            bool ret = numPlayers >= currentMinPlayers;

            return ret;
        }

        public bool TryToStartGame()
        {
            if(state != DNMState.Host)
            {
                Debug.LogError("Invalid state");
                return false;
            }
            if(gameState != GameState.NoGame)
            {
                Debug.LogError("Invalid state");
                return false;
            }
            if(currentSize < 1 || currentSize < currentScene.minSize || currentSize > currentScene.maxSize)
            {
                Debug.LogError("Invalid size");
                return false;
            }
            
            //currentPlayers = new List<DualGamePlayer>();
            gamePlayers = new Dictionary<int, PlayerWrapper>();

            for(int i = 0; i < currentMaxPlayers; i++)
            {
                if(playerMap.ContainsKey(i))
                {
                    PlayerWrapper wrapper = playerMap[i];
                    if(wrapper != null && wrapper.player != null && wrapper.player.role == i)
                    {
                        gamePlayers[i] = wrapper;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Invalid player: {0}.({1})({2})({3})", i, wrapper != null, wrapper.player != null, wrapper.player.role);
                    }
                }
            }

            int numPlayers = gamePlayers.Count;

            if(numPlayers >= currentMinPlayers)
            {
                // start game
                //Debug.LogFormat("Starting game: {0} (size={1}, players={2})", currentSceneName, currentSize, numPlayers);

                gameState = GameState.LoadingGame;
                SetServerInfo("Loading", "");

                // this sets all clients as not-ready
                ServerChangeScene(currentScene.assetName);

                return true;
            }
            else
            {
                Debug.Log("Not enough players");
                return false;
            }
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
            else if(role - 1 < currentMaxPlayers)
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
                for(int i = role - 1; i >= currentMaxPlayers; i--)
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
                    if(playerMap.ContainsKey(currentMaxPlayers - 1))
                    {
                        SwitchRoles(role, currentMaxPlayers - 1);
                    }
                    else
                    {
                        ChangeRole(role, currentMaxPlayers - 1);
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

            if(role + 1 < currentMaxPlayers)
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

        // only server
        public DualGamePlayer GetPlayer(int role)
        {
            if(playerMap.ContainsKey(role))
            {
                PlayerWrapper wrapper = playerMap[role];

                if(wrapper != null && wrapper.player != null)
                {
                    return wrapper.player;
                }
                else
                {
                    Debug.LogError("Invalid player");
                    return null;
                }
            }
            Debug.LogWarning("Missing player");
            return null;
        }
        
        /********** MAIN CALLBACKS **********/

        public override void OnStartHost()
        {
            //Debug.Log("OnStartHost");
        }
        public override void OnStartClient(NetworkClient client)
        {
            //Debug.Log("OnStartClient");
        }

        public override void OnStopServer()
        {
            // Debug.Log("--- DualNetworkManager::OnStopServer()");
            // TODO cleanup something?
            SetServerInfo("Off", "");
        }

        public override void OnStopClient()
        {
            state = DNMState.Off;
            this.cachedLocalPlayers.Clear();

            OnClientDisconnected();
        }

        /************* SERVER CALLBACKS *************/
        // call on server when a client just connected
        public override void OnServerConnect(NetworkConnection connectionToClient)
        {
            //Debug.LogFormat("Connecting client {0} / {1}", state, gameState);
            //Debug.Log("OnServerConnect");
        }

        
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
            // JuloDebug.Log(string.Format("OnServerReady {0} / {1}", state, gameState));

            if(state != DNMState.Host)
            {
                Debug.LogErrorFormat("Invalid state: {0}", state);
                return;
            }

            NetworkServer.SetClientReady(connectionToClient);

            if(gameState == GameState.NoGame)
            {
                //Debug.Log("Do something?");
            }
            else if(gameState == GameState.LoadingGame)
            {
                int numReady = 0;
                int numTotal = NetworkServer.connections.Count;
                
                for(int i = 0; i < numTotal; i++)
                {
                    NetworkConnection conn = NetworkServer.connections[i];
                    bool isReady = conn.isReady;
                    if(isReady)
                    {
                        numReady++;
                    }
                    //Debug.LogFormat("{0} is {1} ready", conn.connectionId, isReady ? "" : "NOT");
                }
                
                //JuloDebug.Log(string.Format("OnServerReady ({0}/{1} ready)", numReady, numTotal));
                
                // TODO only actual players should be required to be ready
                if(numReady == numTotal)
                {
                    //Debug.Log("ALL READY");
                    gameState = GameState.Playing;
                    SetServerInfo("Playing", "");
                    SpawnUnits();
                    StartGame();
                }
                
            }
            else if(gameState == GameState.Playing)
            {
                // Do something?
            }
            else
            {
                Debug.LogErrorFormat("Invalid state: {0}", gameState);
                return;
            }
        }

        // called on server when a client requests to add a player for it
        // TODO should receive a message with initial name/color
        public override void OnServerAddPlayer(NetworkConnection connectionToClient,  short playerControllerId, NetworkReader extraMessage)
        {
            string playerName = "Guest";
            if(extraMessage != null)
            {
                var s = extraMessage.ReadMessage<NewPlayerMessage>();
                playerName = s.playerName;
            }
            else
            {
                Debug.LogWarning("Mo message");
            }

            if(state != DNMState.Host)
            {
                Debug.LogErrorFormat("Invalid state: {0}", state);
                return;
            }

            DualGamePlayer newPlayer = null;

            bool isLocal = (connectionToClient.connectionId == 0);

            if(isLocal)
            { // if the player is local it should be cached
                if(gameState != GameState.NoGame)
                {
                    Debug.LogErrorFormat("Invalid state: {0}", gameState);
                }
                else if(playerControllerId < cachedLocalPlayers.Count)
                {
                    newPlayer = cachedLocalPlayers[playerControllerId];
                }
                else
                {
                    JuloDebug.Err("Player " + playerControllerId + " should be cached");
                    //return;
                }
            }
            else
            { // if is remote should be created
                if(connectionToClient.playerControllers.Count > 0)
                {
                    Debug.LogWarning("Already a player for that connection");
                    return;
                }

                bool isPlaying = gameState != GameState.NoGame;

                newPlayer = CreatePlayer(connectionToClient.connectionId, playerControllerId);
                newPlayer.playerName = playerName;

                int newRole = -1;

                if(isPlaying || joinAsSpectator)
                {
                    // join as spectator
                    newRole = maximumSpectatorRole + 1;
                }
                else
                {
                    // try to join as player
                    bool assigned = false;
                    
                    // try to enter as player
                    for(int i = 0; !assigned && i < currentMaxPlayers; i++)
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

        public override void OnServerSceneChanged(string sceneName)
        {
            //JuloDebug.Log("On server scene changed: waiting for clients to be ready");
            //base.OnServerSceneChanged(sceneName);

            if(state != DNMState.Host) { Debug.LogError("Invalid state"); return; }
            if(gameState != GameState.LoadingGame) { Debug.LogError("Invalid state"); return; }
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
            bool isLocalClient = NetworkServer.active;

            if(isLocalClient)
            {
                if(state != DNMState.Host) { Debug.LogError("Invalid state"); }
                if(gameState != GameState.NoGame) { Debug.LogError("Invalid state"); }

                // add hosted players
                for(int i = 0; i < cachedLocalPlayers.Count; i++)
                {
                    NewPlayerMessage newPlayerMessage = new NewPlayerMessage();
                    newPlayerMessage.playerName = cachedLocalPlayers[i].playerName;

                    ClientScene.AddPlayer(connectionToServer, (short)i, newPlayerMessage);
                }

                OnClientConnected(true);
            }
            else
            {
                if(state != DNMState.Connecting) { Debug.LogError("Invalid state"); }
                state = DNMState.Client;
                SetServerInfo("Client", "");

                //ClientScene.Ready(connectionToServer); // TODO is ready ??

                NewPlayerMessage newPlayerMessage = new NewPlayerMessage();
                newPlayerMessage.playerName = GetPlayerName();

                // ask the server to add player for this remote client
                ClientScene.AddPlayer(connectionToServer, 0, newPlayerMessage);

                OnClientConnected(false);
            }
        }

        /***/
        protected virtual string GetPlayerName() 
        {
            return "Player";
        }

        /***/
        // called on client when it disconnects from server
        public override void OnClientDisconnect(NetworkConnection connectionToServer)
        {
            //JuloDebug.Log("Dual::OnClientDisonnect");
            StopClient();
        }

        public override void OnClientSceneChanged(NetworkConnection connectionToServer)
        {
            //JuloDebug.Log("On client scene changed");
            //base.OnClientSceneChanged(conn);

            if(ClientScene.ready)
            {
                JuloDebug.Warn("Is already ready");
            }
            else
            {
                ClientScene.Ready(connectionToServer);
            }
        }

        // called on client when a network error occurs
        public override void OnClientError(NetworkConnection connectionToServer, int errorCode)
        {
            SetServerInfo("Error", string.Format("({0})", errorCode));
        }

        // called on client when told to be not-ready by a server
        public override void OnClientNotReady(NetworkConnection connectionToServer)
        {
            //JuloDebug.Warn("OnClientNotReady " + connectionToServer.isReady);
        }

        /*********** DUAL CLIENT METHODS ***********/

        public virtual void OnPlayerAdded(DualGamePlayer player) { }
        public virtual void OnPlayerRemoved(DualGamePlayer player) { }
        public virtual void OnRoleChanged(DualGamePlayer player, int oldRole) { }
        public virtual void OnRoomSizeChanged(int minPlayers, int maxPlayers) { }

        /*********** Methods to override ***********/

        protected virtual void OnClientConnected(bool isHost) { }
        protected virtual void OnClientDisconnected() { }

        protected virtual DualGamePlayer CreatePlayer(int connectionId, short playerControllerId)
        {
            Debug.LogWarning("This should be overriden");

            if(playerPrefab == null)
                return null;
            
            GameObject playerObj = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            return playerObj.GetComponent<DualGamePlayer>();
        }

        protected virtual void SpawnUnit(DualGamePlayer owner, int unitId, Transform location)
        {
            Debug.LogWarning("Should override this");
        }

        protected virtual void StartGame()
        {
            Debug.LogWarning("Should override this");
        }
        /*********** MISC ***********/

        public void SetServerInfo(string status, string host)
        {
            if(statusInfo != null)
                statusInfo.text = status;

            if(hostInfo != null)
                hostInfo.text = host;
        }

        /**** Server internal ****/

        private void ChangeRole(int oldRole, int newRole)
        {
            if(playerMap.ContainsKey(newRole)) {
                Debug.LogError("Occupied");
            } else if(!playerMap.ContainsKey(oldRole)) {
                Debug.LogError("Inexistent");
            } else {
                PlayerWrapper playerWrap = playerMap[oldRole];
                playerMap.Remove(oldRole);

                playerMap.Add(newRole, playerWrap);

                playerWrap.player.role = newRole;
            }
        }

        private void SwitchRoles(int role1, int role2)
        {
            PlayerWrapper player1 = playerMap[role1];
            PlayerWrapper player2 = playerMap[role2];

            playerMap.Remove(role1);
            playerMap.Remove(role2);

            playerMap.Add(role2, player1);
            playerMap.Add(role1, player2);

            player1.player.role = role2;
            player2.player.role = role1;
        }

        // called in server to start the game spawning the initial units
        private void SpawnUnits()
        {
            List<SpawnPoint> spawnPoints = JuloFind.allWithComponent<SpawnPoint>();

            foreach(SpawnPoint sp in spawnPoints)
            {
                int role = sp.playerId;
                if(playerMap.ContainsKey(role))
                {
                    PlayerWrapper wrapper = playerMap[role];
                    DualGamePlayer player = wrapper.player;

                    if(role >= 0 && role < numPlayers)
                    {
                        SpawnUnit(player, sp.unitId, sp.transform);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Invalid role");
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Ignoring unit for player {0}", role);
                }
            }
        }
    }
}
