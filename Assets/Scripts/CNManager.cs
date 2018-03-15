
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

using UnityEngine.SceneManagement;

using Julo.Util;
using Julo.Network;

namespace Julo.CNMProto
{
    public class CNManager : DualNetworkManager
    {
        public static CNManager Instance {
            get {
                return (CNManager)singleton;
            }
        }

        [Header("GameManager")]

        public string playSceneName;
        public SceneData[] scenes;

        
        public GameObject matchPrefab;
        public GameObject unitPrefab;

        public Color[] colors;

        public PlayerData mainPlayerModel;
        public PlayerData secondaryPlayerModel;
        public PlayerData cpuPlayerModel;
        public PlayerData remotePlayerModel;

        [Header("Hooks")]

        public Text title;

        public Button backButton;

        public PlayerList playerList;

        public Button playButton;
        public Toggle joinAsSpectatorToggle;

        public PanelManager panelManager;

        public Panel mainMenuPanel;
        public Panel lobbyPanel;
        public Panel connectingPanel;
        public Panel onlinePanel;

        public VisibilityToggling gameOptions;
        public VisibilityToggling serverOptions;

        /** CHAT **/
        public Text chatContent;
        public InputField chatInput;

        /** Online **/
        public Transform matchList;

        public InputField newMatchName;


        [Header("Icons")]
        public Sprite nullIcon;
        public Sprite remoteIcon;
        public Sprite keyboardIcon;
        public Sprite joystickIcon;
        public Sprite cpuIcon;

        // internal

        private delegate void BackButtonDelegate();
        private BackButtonDelegate backDelegate = null;

        // server
        bool gameOver;
        int currentPlayer;
        //bool isPlaying = false;
        List<Unit> units;
        int[] numberOfUnitsPerPlayer;
        NetworkConnection lastOwningConnection;

        //List<DualGamePlayer> gamePlayers;

        // TODO it is correct to implement Start?
        private void Start()
        {
            SetServerInfo("Off", "");
            backButton.gameObject.SetActive(false);
        }


        public void SendChat()
        {
            if(IsClientConnected() && mainPlayer != null) {
                ((CNMPlayer)mainPlayer).CmdSendChat(chatInput.text);
                chatInput.text = "";
            }
        }

        private void OnGUI()
        {
            if(Input.GetKey(KeyCode.H))
            {
                chatInput.ActivateInputField();
            }
            if(Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                if(chatInput.isFocused && chatInput.text != "") {
                    SendChat();
                    chatInput.ActivateInputField();
                }
            }
        }

        /********** button handlers **********/

        /****************************************************/

        public void OnClickPlayOnline()
        {
            SwitchToOnlineMode();
        }

        public void OnClickNewMatch()
        {
            string gameName = newMatchName.text;
            if(gameName == "")
                return;
                
            if(matchMaker == null)
            {
                StartMatchMaker();
            }

            JuloDebug.Log(string.Format("Creating game {0}", gameName));
            if(matchMaker != null)
            {
                matchMaker.CreateMatch(gameName, 16, true, "", "", "", 0, 0, OnInternetMatchCreate);
            }
            else
            {
                Debug.LogWarning("No matchmaker");
            }
        }

        public void OnClickFindMatches()
        {
            if(matchMaker == null)
            {
                StartMatchMaker();
            }

            matchMaker.ListMatches(0, 10, "", false, 0, 0, OnInternetMatchList);
        }

        private void OnInternetMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            // TODO

            if(success)
            {
                Debug.Log("Match created!");

                MatchInfo hostInfo = matchInfo;
                NetworkServer.Listen(hostInfo, 9000);

                List<CNMPlayer> players = new List<CNMPlayer>();
                players.Add(NewPlayer(mainPlayerModel));
                StartAsHost(players, hostInfo);
            }
            else
            {
                Debug.Log("Match creation failed");
            }

        }

        //private string matchName = "New game";

        private void OnInternetMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
        {
            if(success)
            {
                if(matches.Count != 0)
                {
                    Debug.Log("A list of matches was returned");

                    int i = 0;
                    foreach(MatchInfoSnapshot match in matches)
                    {
                        Debug.LogFormat("{0}. {1}", i, match.name);

                        GameObject newMatchObj;
                        //#if UNITY_EDITOR
                        //newMatchObj = PrefabUtility.InstantiatePrefab(matchPrefab) as GameObject;
                        //#else
                        newMatchObj = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                        //#endif

                        MatchDisplay matchDisplay = newMatchObj.GetComponent<MatchDisplay>();
                        matchDisplay.DisplayMatch(match, OnInternetMatchJoin);

                        matchDisplay.transform.SetParent(matchList);
                    }


                    // matchMaker.JoinMatch(matches[matches.Count - 1].networkId, "", "", "", 0, 0, OnInternetMatchJoin);
                }
                else
                {
                    Debug.Log("No matches!!!");
                }
            }
        }

        private void OnInternetMatchJoin(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            if(success)
            {
                Debug.Log("Able to join a match");
                StartAsClient(matchInfo);
            }
            else
            {
                Debug.LogError("Join match failed");
            }
        }

        /****************************************************/

        public void OnClickVersus()
        {
            List<CNMPlayer> players = new List<CNMPlayer>();
            players.Add(NewPlayer(mainPlayerModel));
            players.Add(NewPlayer(secondaryPlayerModel));
            StartAsHost(players);
        }
        
        public void OnClickVersusCpu()
        {
            List<CNMPlayer> players = new List<CNMPlayer>();
            players.Add(NewPlayer(mainPlayerModel));
            players.Add(NewPlayer(cpuPlayerModel));
            StartAsHost(players);
        }

        public void OnClickHost()
        {
            List<CNMPlayer> players = new List<CNMPlayer>();
            players.Add(NewPlayer(mainPlayerModel));
            StartAsHost(players);
        }

        public void OnClickJoin()
        {
            StartAsClient();
            SwitchToConnectingMode();
        }
        
        public void OnClickBack()
        {
            if(backDelegate != null)
            {
                backDelegate();
                backDelegate = null; // TODO right ?
            }
            else
            {
                JuloDebug.Warn("No back callback");
            }
        }

        // only server
        public void OnClickPlay()
        {
            if(!NetworkServer.active) {
                Debug.LogWarning("Client cannot click play");
                return;
            }

            if(TryToStartGame())
            {
                //isPlaying = true;
                units = new List<Unit>();
                numberOfUnitsPerPlayer = new int[currentMaxPlayers];
            }
        }

        public void OnClientNewMessage(string message)
        {
            chatContent.text = chatContent.text + message + "\n";
        }
        
        /********** UI **********/

        private void SwitchToMenuMode()
        {
            backButton.gameObject.SetActive(false);

            panelManager.OpenPanel(mainMenuPanel);

            /*
            mainMenuPanel.Show();
            lobbyPanel.Hide();
            gamePanel.Hide();
            connectingPanel.Hide();
            onlinePanel.Hide();
            */
        }

        private void SwitchToLobbyMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            panelManager.OpenPanel(lobbyPanel);

            /*
            mainMenuPanel.Hide();
            lobbyPanel.Show();
            gamePanel.Show();
            connectingPanel.Hide();
            onlinePanel.Hide();
            */

            // TODO
            joinAsSpectatorToggle.interactable = NetworkServer.active;
            playButton.interactable = NetworkServer.active;
        }

        private void SwitchToOnlineMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            panelManager.OpenPanel(onlinePanel);
            /*
            mainMenuPanel.Hide();
            gamePanel.Hide();
            lobbyPanel.Hide();
            onlinePanel.Show();
            */
        }

        private void SwitchToConnectingMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            // TODO...
            panelManager.OpenPanel(connectingPanel);

            /*
            mainMenuPanel.Hide();
            gamePanel.Hide();
            lobbyPanel.Hide();
            connectingPanel.Show();
            onlinePanel.Hide();
            */
        }

        /********** internal **********/
        
        private void StartAsHost(List<CNMPlayer> players, MatchInfo hostInfo = null)
        {
            if(scenes.Length == 0)
            {
                Debug.LogError("No scenes");
                return;
            }
            
            foreach(CNMPlayer player in players)
            {
                AddHostedPlayer(player);
            }
            
            if(!StartAsHost(scenes[0], hostInfo))
            {
                foreach(CNMPlayer player in players)
                {
                    Destroy(player.gameObject);
                }
            }
        }

        private CNMPlayer NewPlayer()
        {
            GameObject playerObj = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            return playerObj.GetComponent<CNMPlayer>();
        }
        
        private CNMPlayer NewPlayer(PlayerData model)
        {
            CNMPlayer ret = NewPlayer();

            ret.playerName = model.playerName;
            ret.playerType = model.playerType;
            ret.playerColorNum = model.playerColorNumber;
            
            return ret;
        }
        
        /********* overriden from DualNetworkManager *********/

        protected override void OnClientConnected(bool isHost)
        {
            SwitchToLobbyMode();

            title.text = matchName;
        }
        
        protected override void OnClientDisconnected()
        {
            SwitchToMenuMode();
        }
        
        public override void OnPlayerAdded(DualGamePlayer newPlayer)
        {
            playerList.OnPlayerAdded(newPlayer);
        }
        public override void OnPlayerRemoved(DualGamePlayer player)
        {
            playerList.OnPlayerRemoved(player);
        }
        public override void OnRoleChanged(DualGamePlayer player, int oldRole)
        {
            playerList.OnRoleChanged(player, oldRole);
        }
        public override void OnRoomSizeChanged(int minPlayers, int maxPlayers)
        {
            playerList.OnRoomSizeChanged(minPlayers, maxPlayers);
        }
        
        protected override DualGamePlayer CreatePlayer(int connectionId, short playerControllerId)
        {
            bool isLocal = (connectionId == 0);
            DualGamePlayer player = null;

            if(isLocal)
            {
                JuloDebug.Err("Should not be called for hosted players");
            }
            else
            {
                player = NewPlayer(remotePlayerModel);
            }

            return player;
        }

        // only server
        protected override void SpawnUnit(DualGamePlayer owner, int unitId, Transform location)
        {
            if(unitPrefab == null)
            {
                Debug.LogError("No unit prefab");
                return;
            }
            
            GameObject newUnitObj;
            //#if UNITY_EDITOR
            //newUnitObj = PrefabUtility.InstantiatePrefab(unitPrefab) as GameObject;
            //newUnitObj.transform.position = location.position;
            //newUnitObj.transform.rotation = location.rotation;
            //#else
            newUnitObj = Instantiate(unitPrefab, location.position, location.rotation) as GameObject;
            //#endif
            
            Unit newUnit = newUnitObj.GetComponent<Unit>();
            
            units.Add(newUnit);
            numberOfUnitsPerPlayer[owner.role]++;

            newUnit.playerNetId = owner.netId;
            newUnit.playerRole  = owner.role;
            
            NetworkServer.Spawn(newUnit.gameObject);
        }

        protected override void StartGame()
        {
            StartCoroutine("RunGame");
        }

        private IEnumerator RunGame()
        {
            yield return new WaitForSeconds(1f);

            lastOwningConnection = null;
            currentPlayer = -1;
            NextTurn();
            yield break;
        }

        public void NextTurn()
        {
            gameOver = UpdateUnitNumbers();

            if(gameOver)
            {
                Debug.Log("Game is over!!!");
            }
            else
            {
                do {
                    currentPlayer = (currentPlayer + 1) % currentMaxPlayers;
                } while(numberOfUnitsPerPlayer[currentPlayer] == 0);

                Debug.LogFormat("Is turn of {0}", currentPlayer);

                DualGamePlayer current = GetPlayer(currentPlayer);

                if(current.connectionToClient != lastOwningConnection)
                {
                    Debug.Log("Change authority");
                    foreach(Unit unit in units)
                    {
                        NetworkIdentity unitIdentity = unit.GetComponent<NetworkIdentity>();
                        if(lastOwningConnection != null)
                        {
                            unitIdentity.RemoveClientAuthority(lastOwningConnection);
                        }
                        unitIdentity.AssignClientAuthority(current.connectionToClient);
                    }

                    lastOwningConnection = current.connectionToClient;
                }

                ((CNMPlayer)current).RpcPlay();
            } 
        }

        private bool UpdateUnitNumbers()
        {
            for(int i = 0; i < currentMaxPlayers; i++)
            {
                numberOfUnitsPerPlayer[i] = 0;
            }

            int numAliveTeams = 0;

            //Debug.Log("TOTAL = " + units.Count);

            foreach(Unit unit in units)
            {
                int role = unit.playerRole;
                //Debug.Log("One of player " + role);
                if(numberOfUnitsPerPlayer[role] == 0)
                {
                    numAliveTeams++;
                }
                numberOfUnitsPerPlayer[role]++;
            }

            return numAliveTeams <= 1;
        }


        // TODO avoid override from NetworkManager...
        public override void OnClientSceneChanged(NetworkConnection conn)
        {
            base.OnClientSceneChanged(conn);
            gameOptions.Hide();
        }
        
        /************* Misc *************/

        public Color GetColor(ushort index)
        {
            return colors[index];
        }
    }
}

