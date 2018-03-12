
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

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

        [Header("Icons")]
        public Sprite nullIcon;
        public Sprite remoteIcon;
        public Sprite keyboardIcon;
        public Sprite joystickIcon;
        public Sprite cpuIcon;

        private delegate void BackButtonDelegate();
        private BackButtonDelegate backDelegate = null;

        public Color[] colors;

        // TODO it is correct to implement Start?
        private void Start()
        {
            SetServerInfo("Off", "");
            backButton.gameObject.SetActive(false);
            //StartCoroutine("ShowInfo");
        }


        public Text chatContent;
        public InputField chatInput;

        public void SendChat()
        {
            if(IsClientConnected() && mainPlayer != null) {
                ((CNMPlayer)mainPlayer).CmdSendChat(chatInput.text);
                chatInput.text = "";
            }
        }

        private void OnGUI()
        {
            
            if(Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                if(chatInput.isFocused && chatInput.text != "") {
                    SendChat();
                }
            }
        }

        /********** button handlers **********/

        public void OnClickBack()
        {
            if(backDelegate != null)
            {
                backDelegate();
                backDelegate = null; // TODO right ?
                //isInGame = false
            }
            else
            {
                JuloDebug.Warn("No back callback");
            }
        }

        public void OnClickVersus()
        {
            CNMPlayer p1 = NewPlayer(mainPlayerModel);
            CNMPlayer p2 = NewPlayer(secondaryPlayerModel);

            AddHostedPlayer(p1);
            AddHostedPlayer(p2);

            StartAsHost();
        }

        public void OnClickVersusCpu()
        {
            CNMPlayer p1 = NewPlayer(mainPlayerModel);
            CNMPlayer cpu = NewPlayer(cpuPlayerModel);

            AddHostedPlayer(p1);
            AddHostedPlayer(cpu);
            
            StartAsHost();
        }

        public void OnClickHost()
        {
            CNMPlayer p1 = NewPlayer(mainPlayerModel);
            AddHostedPlayer(p1);
            
            StartAsHost();
        }


        public void OnClickJoin()
        {
            StartAsClient();
            SwitchToConnectingMode();
            //backDelegate = this.StopClient;
        }
        
        public void OnClickPlay()
        {
            // TODO implement
            
            // ...
        }

        /******** Overriden ********/

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
        
        protected override void OnClientConnected(bool isHost)
        {
            SwitchToLobbyMode();
        }

        protected override void OnClientDisconnected()
        {
            //bool isHostedClient = NetworkServer.active;
            //if(!isHostedClient)
            SwitchToMenuMode();
        }

        /***************************/

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

        private CNMPlayer NewPlayer()
        {
            GameObject playerObj = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            return playerObj.GetComponent<CNMPlayer>();
        }
        
        //private CNMPlayer NewPlayer(string name, PlayerType playerType, ushort colorNum)
        private CNMPlayer NewPlayer(PlayerData model)
        {
            CNMPlayer ret = NewPlayer();

            ret.playerName = model.playerName;
            ret.playerType = model.playerType;
            ret.playerColorNum = model.playerColorNumber;
            
            return ret;
        }
        
        /********* overriden from DualNetworkManager *********/

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

        /************* Misc *************/

        public Color GetColor(ushort index)
        {
            return colors[index];
        }
    }
}

