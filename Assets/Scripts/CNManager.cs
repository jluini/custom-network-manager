
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Header("Panels")]

        public Button backButton;

        public PanelManager panelManager;
        public Panel mainMenuPanel;
        public Panel lobbyPanel;
        public Panel onlinePanel;
        public Panel connectingPanel;
        private const string isPlayingParameterName = "IsPlaying";
        private int isPlayingParameterId;

        [Header("Online")]
        public InputField yourName;
        public Transform matchContainer;
        public InputField newMatchName;

        public GameObject creatingDisplay;
        public GameObject findingDisplay;
        public GameObject noMatchesNotice;
        public GameObject errorFindingNotice;

        [Header("Game")]
        public Text title;
        public Button playButton;
        public Toggle joinAsSpectatorToggle;
        public VisibilityToggling gameOptions;
        public VisibilityToggling serverOptions;

        [Header("Lobby")]
        public PlayerList playerList;
        [Header("Chat")]
        public ChatManager chatManager;
        public InputField chatInput;

        [Header("Icons")]
        public Sprite nullIcon;
        public Sprite remoteIcon;
        public Sprite keyboardIcon;
        public Sprite joystickIcon;
        public Sprite cpuIcon;

        // internal
        bool isPlaying = false;

        private string gameName = "New game";
        private Dictionary<UnityEngine.Networking.Types.NetworkID, MatchInfoSnapshot> matchDict;

        private delegate void BackButtonDelegate();
        private BackButtonDelegate backDelegate = null;

        /********/

        public void OnOnlinePlayerNameChanged(string newName)
        {
            mainPlayerModel.playerName = newName;
            SaveData();
        }

        /*******/

        private void SaveData()
        {
            string destination = DataFilePath();
            FileStream fileStream;

            if(File.Exists(destination))
                fileStream = File.OpenWrite(destination);
            else
                fileStream = File.Create(destination);

            GameData data = new GameData(mainPlayerModel.playerName);

            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(fileStream, data);
            fileStream.Close();
        }

        private void LoadData()
        {
            string destination = DataFilePath();
            FileStream fileStream;

            if(!File.Exists(destination))
            {
                return;
            }
            fileStream = File.OpenRead(destination);

            BinaryFormatter bf = new BinaryFormatter();
            GameData data = (GameData)bf.Deserialize(fileStream);
            fileStream.Close();

            mainPlayerModel.playerName = data.playerName;
        }

        private string DataFilePath()
        {
            return Application.persistentDataPath + "/save.dat";
        }


        /*******/


        protected override string GetPlayerName()
        {
            return mainPlayerModel.playerName;
        }

        /********/

        // server
        bool gameOver;
        int currentPlayer;
        List<Unit> units;
        int[] numberOfUnitsPerPlayer;
        NetworkConnection lastOwningConnection;

        //List<DualGamePlayer> gamePlayers;

        // TODO it is correct to implement Start?
        private void Start()
        {
            SetServerInfo("Off", "");
            backButton.gameObject.SetActive(false);

            isPlayingParameterId = Animator.StringToHash(isPlayingParameterName);

            LoadData();
        }


        public void SendChat()
        {
            if(IsClientConnected() && mainPlayer != null)
            {
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

        /******************* ONLINE *************************/

        public void OnClickPlayOnline()
        {
            SwitchToOnlineMode();
            newMatchName.text = gameName;
        }

        public void OnClickNewMatch()
        {
            if(newMatchName.text == "")
                return;

            gameName = newMatchName.text;
                
            if(matchMaker == null)
            {
                StartMatchMaker();
            }

            JuloDebug.Log(string.Format("Creating game {0}", gameName));
            if(matchMaker != null)
            {
                matchMaker.CreateMatch(gameName, 16, true, "", "", "", 0, 0, OnInternetMatchCreate);
                creatingDisplay.SetActive(true);
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
            findingDisplay.SetActive(true);
        }

        private void OnInternetMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            creatingDisplay.SetActive(false);

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

        private void OnInternetMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
        {
            findingDisplay.SetActive(false);
            if(success)
            {
                errorFindingNotice.SetActive(false);

                if(matchDict == null)
                    matchDict = new Dictionary<UnityEngine.Networking.Types.NetworkID, MatchInfoSnapshot>();
                else
                    matchDict.Clear();

                int numChilds = matchContainer.transform.childCount;

                for(int i = 0; i < numChilds; i++)
                {
                    Transform child = matchContainer.transform.GetChild(i);
                    if(child.GetComponent<MatchDisplay>() != null)
                    {
                        Destroy(child.gameObject);
                    }
                }

                if(matchList.Count != 0)
                {
                    //
                    noMatchesNotice.SetActive(false);

                    foreach(MatchInfoSnapshot match in matchList)
                    {
                        matchDict.Add(match.networkId, match);

                        GameObject newMatchObj;
                        //#if UNITY_EDITOR
                        //newMatchObj = PrefabUtility.InstantiatePrefab(matchPrefab) as GameObject;
                        //#else
                        newMatchObj = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                        //#endif

                        MatchDisplay matchDisplay = newMatchObj.GetComponent<MatchDisplay>();
                        matchDisplay.DisplayMatch(match, OnMatchJoinClicked(match.networkId));

                        matchDisplay.transform.SetParent(matchContainer, false);
                    }
                }
                else
                {
                    //Debug.Log("No matches!!!");
                    noMatchesNotice.SetActive(true);
                }
            }
            else
            {
                errorFindingNotice.SetActive(true);
            }
        }

        public delegate void OnClickMatchJoin();

        private OnClickMatchJoin OnMatchJoinClicked(UnityEngine.Networking.Types.NetworkID matchId)
        {
            return new OnClickMatchJoin(() => JoinToMatch(matchId));
        }

        private void JoinToMatch(UnityEngine.Networking.Types.NetworkID matchId)
        {
            matchMaker.JoinMatch(matchId, "", "", "", 0, 0, OnInternetMatchJoin);
        }

        private void OnInternetMatchJoin(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            if(success)
            {
                MatchInfoSnapshot match = matchDict[matchInfo.networkId];
                gameName = match.name;
                Debug.LogFormat("Able to join match {0}", match.name);
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
                units = new List<Unit>();
                numberOfUnitsPerPlayer = new int[currentMaxPlayers];
            }
        }

        public void OnClientNewMessage(ChatMessage message)
        {
            chatManager.NewMessage(message);
        }
        
        /********** UI **********/

        private void SwitchToMenuMode()
        {
            backButton.gameObject.SetActive(false);

            panelManager.OpenPanel(mainMenuPanel);
        }

        private void SwitchToLobbyMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            panelManager.OpenPanel(lobbyPanel);

            joinAsSpectatorToggle.interactable = NetworkServer.active;

            playButton.interactable = false;
            if(NetworkServer.active)
                UpdatePlayButton();
        }

        private void SwitchToOnlineMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.SwitchToMenuMode;

            panelManager.OpenPanel(onlinePanel);

            yourName.text = mainPlayerModel.playerName;
        }

        private void SwitchToConnectingMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            panelManager.OpenPanel(connectingPanel);
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
            //connectingNotice.Hide();

            title.text = gameName;
            //Debug.LogFormat("Starting game with name '{0}'", gameName);
        }

        protected override void OnClientDisconnected()
        {
            Debug.Log("OnClientDisconnected");
            SwitchToMenuMode();
        }
        
        public override void OnPlayerAdded(DualGamePlayer newPlayer)
        {
            playerList.OnPlayerAdded(newPlayer);
            if(NetworkServer.active && !isPlaying)
                UpdatePlayButton();
        }
        public override void OnPlayerRemoved(DualGamePlayer player)
        {
            playerList.OnPlayerRemoved(player);
            if(NetworkServer.active && !isPlaying)
                UpdatePlayButton();
        }
        public override void OnRoleChanged(DualGamePlayer player, int oldRole)
        {
            playerList.OnRoleChanged(player, oldRole);
            if(NetworkServer.active && !isPlaying)
                UpdatePlayButton();
        }
        public override void OnRoomSizeChanged(int minPlayers, int maxPlayers)
        {
            playerList.OnRoomSizeChanged(minPlayers, maxPlayers);
            if(NetworkServer.active && !isPlaying)
                UpdatePlayButton();
        }

        private void UpdatePlayButton()
        {
            //Debug.Log("Updated with " + );
            playButton.interactable = ThereIsEnoughPlayers();
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
            #if UNITY_EDITOR
            newUnitObj = PrefabUtility.InstantiatePrefab(unitPrefab) as GameObject;
            newUnitObj.transform.position = location.position;
            newUnitObj.transform.rotation = location.rotation;
            #else
            newUnitObj = Instantiate(unitPrefab, location.position, location.rotation) as GameObject;
            #endif
            
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

                //Debug.LogFormat("Is turn of {0}", currentPlayer);

                DualGamePlayer current = GetPlayer(currentPlayer);

                if(current.connectionToClient != lastOwningConnection)
                {
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

            foreach(Unit unit in units)
            {
                int role = unit.playerRole;
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
            isPlaying = true;
            //gameOptions.Hide();
            chatManager.StartCleaning();
            lobbyPanel.animator.SetBool(isPlayingParameterId, true);

            //LayoutRebuilder.MarkLayoutForRebuild(playerList.GetComponent<RectTransform>());
            StartCoroutine("RebuildLayout");
        }

        private const string playingStateName = "Playing";
        private IEnumerator RebuildLayout()
        {
            Animator anim = lobbyPanel.animator;

            bool finalStateReached = false;

            while(!finalStateReached) //anim.IsInTransition(0))
            {
                Debug.Log("Rebuilding");
                LayoutRebuilder.MarkLayoutForRebuild(playerList.GetComponent<RectTransform>());
                yield return new WaitForEndOfFrame();

                if(!anim.IsInTransition(0))
                {
                    finalStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(playingStateName);
                }
            }

            Debug.Log("Rebuilding");
            LayoutRebuilder.MarkLayoutForRebuild(playerList.GetComponent<RectTransform>());

            yield break;
        }

        /************* Misc *************/

        public Color GetColor(ushort index)
        {
            return colors[index];
        }
    }
}

