
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

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

        
        public GameObject unitPrefab;

        public Color[] colors;

        public PlayerData mainPlayerModel;
        public PlayerData secondaryPlayerModel;
        public PlayerData cpuPlayerModel;
        public PlayerData remotePlayerModel;

        [Header("Hooks")]

        public Button backButton;

        public PlayerList playerList;

        public VisibilityToggling serverOptions;
        public Button playButton;
        public Toggle joinAsSpectatorToggle;

        public MainMenuPanel mainMenuPanel;
        public VisibilityToggling gamePanel;
        public LobbyPanel    lobbyPanel;
        public VisibilityToggling connectingPanel;
        
        public Text chatContent;
        public InputField chatInput;

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
        bool isPlaying = false;
        List<Unit> units;
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
                isPlaying = true;
                units = new List<Unit>();
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
            
            mainMenuPanel.Show();
            lobbyPanel.Hide();
            gamePanel.Hide();
            connectingPanel.Hide();
        }

        private void SwitchToLobbyMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            mainMenuPanel.Hide();
            lobbyPanel.Show();
            gamePanel.Show();
            connectingPanel.Hide();

            joinAsSpectatorToggle.interactable = NetworkServer.active;
            playButton.interactable = NetworkServer.active;
        }

        private void SwitchToConnectingMode()
        {
            backButton.gameObject.SetActive(true);
            backDelegate = this.Stop;

            mainMenuPanel.Hide();
            gamePanel.Hide();
            lobbyPanel.Hide();
            connectingPanel.Show();
        }

        /********** internal **********/
        
        private void StartAsHost(List<CNMPlayer> players)
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
            
            if(!StartAsHost(scenes[0]))
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
            
            newUnit.playerId = owner.role;
            
            NetworkServer.Spawn(newUnit.gameObject);
        }

        // TODO avoid override from NetworkManager...
        public override void OnClientSceneChanged(NetworkConnection conn)
        {
            base.OnClientSceneChanged(conn);
            gamePanel.Hide();
        }
        
        /************* Misc *************/

        public Color GetColor(ushort index)
        {
            return colors[index];
        }
    }
}

