using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Julo.CNMProto
{
    
    public class PlayerDisplay : VisibilityToggling
    {
        [Header("Hooks")]
        public Image iconImage;

        //public Text nameDisplay;

        public InputField nameInput;

        public Image coloredImage;

        public CNMPlayer lastDisplayedPlayer = null;

        public void OnEditName()
        {
            if(lastDisplayedPlayer != null)
            {
                string value = nameInput.text;
                Debug.Log("OnValueChanged to " + value);
                lastDisplayedPlayer.CmdChangeName(value);
            }
        }

        public void DisplayPlayer(CNMPlayer player)
        {
            this.lastDisplayedPlayer = player;

            string name = "(waiting)";
            Sprite icon = CNManager.Instance.nullIcon;
            Color color = Color.black;

            bool isLocal = false;

            if(player != null)
            {
                isLocal = player.isLocalPlayer;
                name = player.playerName;
                color = player.playerColor;

                if(isLocal)
                {
                    switch(player.playerType)
                    {
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

                    nameInput.interactable = true;
                }
                else
                {
                    icon = CNManager.Instance.remoteIcon;
                }
            }
            else
            {
                //nameInput.interactable = false;
            }

            nameInput.interactable = isLocal;
            nameInput.text = name;
            iconImage.sprite = icon;
            coloredImage.color = color;
        }

        protected override void DoShow()
        {
            gameObject.SetActive(true);
        }

        protected override void DoHide()
        {
            gameObject.SetActive(false);
        }

    }
    
    
}