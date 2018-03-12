
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using Julo.Util;
using Julo.Network;


namespace Julo.CNMProto
{
    public enum PlayerType
    {
        KeyboardPlayer,
        JoystickPlayer,
        CpuPlayer
    }

    public class CNMPlayer : DualGamePlayer {
        [SyncVar]
        public PlayerType playerType;

        [SyncVar(hook="OnNameChanged")]
        public string playerName;

        [SyncVar(hook="OnColorChanged")]
        public ushort playerColorNum;

        public Color playerColor
        {
            get {
                return CNManager.Instance.GetColor(playerColorNum);
            }

        }

        [Header("Debug")]

        public bool debugging = false;

        [Header("Hooks")]

        public Image iconImage;
        // public Text nameDisplay;
        public InputField nameInput;
        public Button colorInput;
        public Text roleDisplay;

        public Image coloredImage;
        public Text coloredText;

        public Button moveUp;
        public Button moveDown;

        /************/

        public override void OnStartClient()
        {
            base.OnStartClient();
            Draw();
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // if is a local player in the server needs to be redrawn as local
            if(NetworkServer.active)
            {
                Draw();
            }
        }

        private void OnNameChanged(string newPlayerName)
        {
            playerName = newPlayerName;
            Draw();
        }

        private void OnColorChanged(ushort newColorNum)
        {
            playerColorNum = newColorNum;
            Draw();
        }

        /**************************************************/

        [Command]
        public void CmdChangeColor(ushort newColorNum)
        {
            playerColorNum = newColorNum;
        }

        [Command]
        public void CmdChangeName(string newName)
        {
            playerName = newName;
        }

        [Command]
        public void CmdSendChat(string message)
        {
            RpcNewChatMessage(string.Format("<<b>{0}</b> says>: {1}", playerName, message));
        }

        [ClientRpc]
        public void RpcNewChatMessage(string message)
        {
            CNManager.Instance.OnClientNewMessage(message);
        }

        /********************************/

        public void Draw()
        {
            string name = playerName;
            Color color = playerColor;

            Sprite icon = CNManager.Instance.nullIcon;
            //if(!NetworkServer.active && isLocalPlayer || NetworkServer.active && ) {
            if(isLocalPlayer) {
                switch(playerType) {
                case PlayerType.KeyboardPlayer:
                    icon = CNManager.Instance.keyboardIcon;
                    break;
                case PlayerType.JoystickPlayer:
                    icon = CNManager.Instance.joystickIcon;
                    break;
                case PlayerType.CpuPlayer:
                    icon = CNManager.Instance.cpuIcon;
                    break;
                }
            } else {
                icon = CNManager.Instance.remoteIcon;
            }

            nameInput.text = name;
            nameInput.interactable = isLocalPlayer;
            colorInput.interactable = isLocalPlayer;

            string roleText;
            if(role < CNManager.Instance.maxPlayers)
            {
                roleText = "p" + (role + 1);
            }
            else
            {
                roleText = "o.o";
            }

            roleDisplay.text = roleText;

            if(coloredImage)
                coloredImage.color = color;
            if(iconImage)
                iconImage.sprite = icon;
            if(coloredText)
                coloredText.color = color;
        }

        public override void OnPlayerChanged()
        {
            Draw();
        }

        /************* Editing *************/

        public void OnEditColor()
        {
            CmdChangeColor((ushort)((playerColorNum + 1) % CNManager.Instance.colors.Length));
        }

        public void OnEditName()
        {
            string newName = nameInput.text;
            CmdChangeName(newName);
        }

        public void OnClickUp()
        {
            if(NetworkServer.active)
            {
                CNManager.Instance.PlayerUp(this);
            }
        }

        public void OnClickDown()
        {
            if(NetworkServer.active)
            {
                CNManager.Instance.PlayerDown(this);
            }
        }

        /***********************************/
    }
}
