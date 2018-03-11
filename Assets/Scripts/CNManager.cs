
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

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

        public ushort defaultPlayerOneColor = 0;
        public ushort defaultPlayerTwoColor = 1;
        public ushort defaultPlayerCpuColor = 2;
        public ushort defaultPlayerRemoteColor = 3;

        [Header("Hooks")]

        public Button backButton;

        public PlayerList playerList;

        public VisibilityToggling serverOptions;
        public Button playButton;
        public Toggle joinAsSpectatorToggle;

        public MainMenuPanel mainMenuPanel;
        public VisibilityToggling gamePanel;
        public LobbyPanel    lobbyPanel;

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
            StartAsHost();

            CNMPlayer p1 = NewPlayer("Player1", PlayerType.KeyboardPlayer, defaultPlayerOneColor);
            CNMPlayer p2 = NewPlayer("Player2", PlayerType.KeyboardPlayer, defaultPlayerTwoColor);

            AddHostedPlayer(p1);
            AddHostedPlayer(p2);
        }

        public void OnClickVersusCpu()
        {
            StartAsHost();

            CNMPlayer p1 = NewPlayer("Player1", PlayerType.KeyboardPlayer, defaultPlayerOneColor);
            CNMPlayer p2 = NewPlayer("CPU", PlayerType.CpuPlayer, defaultPlayerCpuColor);

            AddHostedPlayer(p1);
            AddHostedPlayer(p2);
        }

        public void OnClickHost()
        {
            StartAsHost();

            CNMPlayer p1 = NewPlayer("Player1", PlayerType.KeyboardPlayer, defaultPlayerOneColor);
            AddHostedPlayer(p1);
        }


        public void OnClickJoin()
        {
            StartAsClient();
        }
        
        public void OnClickPlay()
        {
            // TODO implement
            
            // ...
        }
        
        public override void OnStopHost()
        {
            base.OnStopHost();
            //SwitchToMenuMode();
        }
        public override void OnStopClient()
        {
            bool isHostedClient = NetworkServer.active;
            JuloDebug.Log(string.Format("GameManager::OnStopClient ({0})", isHostedClient ? "server here" : "remote"));
            base.OnStopClient();

            //if(!isHostedClient)
            SwitchToMenuMode();
        }

        public override void OnStartHost()
        {
            base.OnStartHost();

            //JuloDebug.Log(string.Format("GameManager::OnStartHost"));

            backDelegate = this.StopHost;
        }

        public override void OnStartClient(NetworkClient client)
        {
            base.OnStartClient(client);

            bool isHostedClient = NetworkServer.active;

            if(isHostedClient)
            {
                SwitchToLobbyMode();
            }
            else
            {
                backDelegate = this.StopClient;
            }
        }

        public void OnClientNewMessage(string message)
        {
            chatContent.text = chatContent.text + message + "\n";
        }

        protected override void OnClientConnected()
        {
            if(!NetworkServer.active)
            {
                SwitchToLobbyMode();
            }
        }


        /********** UI **********/

        private void SwitchToMenuMode()
        {
            mainMenuPanel.Show();
            lobbyPanel.Hide();
            gamePanel.Hide();

            backButton.gameObject.SetActive(false);
        }

        private void SwitchToLobbyMode()
        {
            backButton.gameObject.SetActive(true);

            mainMenuPanel.Hide();
            lobbyPanel.Show();
            gamePanel.Show();

            joinAsSpectatorToggle.interactable = NetworkServer.active;
            playButton.interactable = NetworkServer.active;
        }

        /********** internal **********/

        private CNMPlayer NewPlayer()
        {
            GameObject playerObj = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            return playerObj.GetComponent<CNMPlayer>();
        }
        
        private CNMPlayer NewPlayer(string name, PlayerType playerType, ushort colorNum)
        {
            CNMPlayer ret = NewPlayer();

            ret.playerName = name;
            ret.playerType = playerType;
            ret.playerColorNum = colorNum;
            
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
                player = NewPlayer("Remote", PlayerType.KeyboardPlayer, defaultPlayerRemoteColor);
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

