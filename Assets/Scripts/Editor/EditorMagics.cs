
using UnityEngine;



#if UNITY_EDITOR

using UnityEditor;
using Julo.Util;
using Julo.CNMProto;

#endif

public class EditorMagics : MonoBehaviour {

    #if UNITY_EDITOR

    static GameObject mainMenu
    {
        get {
            return JuloFind.byName("MainMenu");
        }
    }
    static GameObject gameOptions
    {
        get {
            return JuloFind.byName("GameOptions");
        }
    }
    static GameObject lobbyPanel
    {
        get {
            return JuloFind.byName("LobbyPanel");
        }
    }
    static GameObject onlinePanel {
        get {
            return JuloFind.byName("OnlinePanel");
        }
    }

    static PlayerList playerList
    {
        get {
            return JuloFind.singleton<PlayerList>();
        }
    }

    static GameObject playerPrefab
    {
        get {
            return (GameObject)Resources.Load("Prefabs/GamePlayer", typeof(GameObject));
        }
    }
    static GameObject wildcardPrefab
    {
        get {
            return (GameObject)Resources.Load("Prefabs/Wildcard", typeof(GameObject));
        }
    }

    static Transform _list = null;
    static Transform list
    {
        get {
            if(_list == null)
                _list = playerList.playerContainer.transform;
            return _list;
        }
    }

    [MenuItem("Magia/Play")]
    static void SwitchToStartMode()
    {
        mainMenu.SetActive(true);
        lobbyPanel.SetActive(false);
        onlinePanel.SetActive(false);

        DeleteMockPlayers();
    }

    static void DeleteMockPlayers()
    {
        Transform list = playerList.playerContainer.transform;

        bool found = true;
        while(found)
        {
            found = false;
            for(int i = 0; i < list.childCount; i++) {
                Transform elem = list.GetChild(i);
                if(elem.gameObject.name != "Separator") {
                    DestroyImmediate(elem.gameObject);
                    found = true;
                }
            }
        }
    }

    [MenuItem("Magia/Lobby design")]
    static void SwitchToLobbyMode()
    {
        mainMenu.SetActive(false);
        lobbyPanel.SetActive(true);
        onlinePanel.SetActive(false);

        CreateMockPlayers();
    }

    static void CreateMockPlayers()
    {
        //GameObject playerPrefab = 
        //GameObject wildcardPrefab = (GameObject)Resources.Load("Prefabs/Wildcard", typeof(GameObject));

        //Transform list = playerList.playerContainer.transform;

        if(list.childCount == 1 && list.GetChild(0).name == "Separator")
        {
            newPlayer("Julo", Color.blue, "p1");
            newPlayer("Bubba", Color.yellow, "p2");
            newWildcard();
            list.GetChild(0).SetAsLastSibling();
            newPlayer("Jeday", Color.cyan, "o.o");
            newPlayer("Eze", Color.red, "o.o");
            newPlayer("Nano", Color.magenta, "o.o");
        }
    }

    [MenuItem("Magia/Online mode")]
    static void SwitchToOnlineMode()
    {
        mainMenu.SetActive(false);
        onlinePanel.SetActive(true);

        DeleteMockPlayers();
    }

    static void newPlayer(string name, Color color, string role)
    {
        GameObject newPlayerObj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity) as GameObject;

        CNMPlayer newPlayer = newPlayerObj.GetComponent<CNMPlayer>();
        newPlayer.nameInput.text = name;
        newPlayer.coloredImage.color = color;
        newPlayer.roleDisplay.text = role;
        newPlayer.transform.SetParent(list);
    }
    static void newWildcard()
    {
        GameObject newWildcard = Instantiate(wildcardPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        newWildcard.transform.SetParent(list);
    }


    #endif
}

