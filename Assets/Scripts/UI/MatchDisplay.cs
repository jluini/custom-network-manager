using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class MatchDisplay : MonoBehaviour
{

    //public delegate void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo);

    public Text nameDisplay;
    public Button joinButton;
    public MatchInfoSnapshot currentMatch;

    public void DisplayMatch(MatchInfoSnapshot match, NetworkMatch.DataResponseDelegate<MatchInfo> joinCallback)
    {
        this.currentMatch = match;

        nameDisplay.text = match.name;
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => OnClickJoin(joinCallback));
    }

    private void OnClickJoin(NetworkMatch.DataResponseDelegate<MatchInfo> joinCallback)
    {
        NetworkManager.singleton.matchMaker.JoinMatch(currentMatch.networkId, "", "", "", 0, 0, joinCallback);
    }

}

