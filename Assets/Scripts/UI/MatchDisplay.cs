using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace Julo.CNMProto
{
    public class MatchDisplay : MonoBehaviour
    {

        //public delegate void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo);

        public Text nameDisplay;
        public Button joinButton;
        //public MatchInfoSnapshot currentMatch;

        public void DisplayMatch(MatchInfoSnapshot match, CNManager.OnClickMatchJoin joinCallback)
        {
            //this.currentMatch = match;

            nameDisplay.text = match.name;
            joinButton.onClick.RemoveAllListeners();
            //joinButton.onClick.AddListener(() => OnClickJoin(joinCallback));
            joinButton.onClick.AddListener(() => joinCallback());
        }
    }
}
