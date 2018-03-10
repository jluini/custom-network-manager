
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

        public PlayButton    playButton;
        public MainMenuPanel mainMenuPanel;
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

            if(!isHostedClient)
            {
                backDelegate = this.StopClient;
            }

            SwitchToLobbyMode();
        }

        public void OnClientNewMessage(string message)
        {
            chatContent.text = chatContent.text + message + "\n";
        }


        /********** UI **********/
        /*
        public void DrawLeft(CNMPlayer player) {
            DrawPlayer(leftPlayerDisplay, player);
        }
        public void DrawRight(CNMPlayer player) {
            DrawPlayer(rightPlayerDisplay, player);
        }
        private void DrawPlayer(PlayerDisplay display, CNMPlayer player) {
            if(player.debugging) JuloDebug.Log(string.Format("DrawPlayer({0})", player.playerName));
            
            display.DisplayPlayer(player);
        }
        */
        private void SwitchToMenuMode()
        {
            mainMenuPanel.Show();
            lobbyPanel.Hide();

            backButton.gameObject.SetActive(false);
            if(playButton.isVisible)
                playButton.Hide();

            //leftPlayerDisplay.Hide();
            //rightPlayerDisplay.Hide();
        }

        private void SwitchToLobbyMode()
        {
            backButton.gameObject.SetActive(true);

            mainMenuPanel.Hide();
            lobbyPanel.Show();

            if(NetworkServer.active)
                playButton.Show();
            /*
            leftPlayerDisplay.DisplayPlayer(null);
            rightPlayerDisplay.DisplayPlayer(null);

            leftPlayerDisplay.Show();
            rightPlayerDisplay.Show();
            */
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

        protected override void OnRoleChanged(DualGamePlayer playerObj, int oldRole, int newRole)
        {
            // TODO implement
        }

        protected override void OnRoleExited(DualGamePlayer player, int oldRole)
        {
            // TODO implement
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

