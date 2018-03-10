using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using Julo.Network;

namespace Julo.CNMProto
{
    public class PlayerList : DualClient
    {
        public Transform wildcardPrefab;

        // TODO make private
        public int minPlayers;
        public int maxPlayers;

        [Header("Hooks")]
        public Transform playerContainer;

        //private int dummyPlayers = 0;

        private List<Transform> wildcards;

        //private Dictionary<int, CNMPlayer> playerMap;

        private Transform separator;

        void Start()
        {
            minPlayers = CNManager.Instance.minPlayers;
            maxPlayers = CNManager.Instance.maxPlayers;

            int childCount = playerContainer.childCount;

            if(childCount != 1)
            {
                Debug.LogError("Invalid start");
            }

            separator = playerContainer.GetChild(0);

            wildcards = new List<Transform>();

            for(int i = 0; i < maxPlayers; i++)
            {
                Transform wildcard = (Transform)Instantiate(wildcardPrefab, Vector3.zero, Quaternion.identity);

                wildcard.SetParent(playerContainer);
                wildcard.SetAsLastSibling();

                wildcards.Add(wildcard);
            }
            separator.SetAsLastSibling();
        }

        public override void OnPlayerAdded(DualGamePlayer player)
        {
            // Debug.LogFormat("Adding in client player with role {0}", player.role);

            AddPlayerToList(player);

            if(!NetworkServer.active)
            {
                ((CNMPlayer)player).moveUp.gameObject.SetActive(false);
                ((CNMPlayer)player).moveDown.gameObject.SetActive(false);
            }

            RedrawMoveButtons();
        }

        private void AddPlayerToList(DualGamePlayer player)
        {
            int newRole = player.role;
            //Debug.LogFormat("Adding player {0}", newRole);

            if(newRole < maxPlayers)
            { // is an actual player
                ReplaceWildcard(newRole, player.transform);
            }
            else
            { // is spectator
                bool added = false;

                if(playerContainer.GetChild(separator.GetSiblingIndex()) != separator)
                    Debug.LogError("Wrong 2");

                for(int i = separator.GetSiblingIndex() + 1; i < playerContainer.childCount; i++)
                {
                    Transform child = playerContainer.GetChild(i);
                    DualGamePlayer other = child.GetComponent<DualGamePlayer>();

                    if(other != null)
                    {
                        if(player.role < other.role)
                        {
                            added = true;
                            player.transform.SetParent(playerContainer);
                            player.transform.SetSiblingIndex(i);
                        }
                    }
                    else
                    {
                        Debug.LogError("Null player");
                    }
                }

                if(!added)
                {
                    player.transform.SetParent(playerContainer);
                    player.transform.SetAsLastSibling();
                }
            }
        }

        public override void OnPlayerRemoved(DualGamePlayer player)
        {
            if(player.role < 0)
            {
                Debug.LogErrorFormat("Invalid role {0}", player.role);
                return;
            }

            if(player.role < maxPlayers)
            { // if it was an actual player
                ShowWildcard(player.role);

            }
            else
            { // if it was spectator
                
            }

            player.transform.SetParent(null);

            RedrawMoveButtons();
        }

        public override void OnRoleChanged(DualGamePlayer player, int oldRole)
        {
            int newRole = player.role;
            if(oldRole == newRole)
            {
                Debug.LogWarning("It didn't change!");
                return;
            }

            if(oldRole < maxPlayers)
            {
                bool alreadyReplaced = false;

                for(int i = 0; i < playerContainer.childCount; i++)
                {
                    CNMPlayer p = playerContainer.GetChild(i).GetComponent<CNMPlayer>();

                    //Debug.Log("Checking one...");
                    if(p != null)
                    {
                        if(p.role == oldRole)
                        {
                            //Debug.Log("Already replaced");

                            alreadyReplaced = true;
                            break;
                        }
                        /*else if(p.role > oldRole)
                        {
                            Debug.Log("Breaking here");
                            break;
                        }
                        else
                        {
                            Debug.LogFormat("p.role ({0}) < ({1}) oldRole", p.role, oldRole);
                        }*/
                    }
                }

                if(!alreadyReplaced)
                    ShowWildcard(oldRole);
            }

            int oldCount = playerContainer.childCount;
            player.transform.SetParent(null);
            int newCount = playerContainer.childCount;

            if(newCount != oldCount - 1)
                Debug.LogError("Wrong 1");

            AddPlayerToList(player);

            RedrawMoveButtons();
        }

        public override void OnRoomSizeChanged(int newSize)
        {
            Debug.LogError("Not implemented");
        }

        private void ShowWildcard(int index)
        {
            if(wildcards[index].gameObject.activeSelf)
                Debug.LogWarningFormat("Wildcard {0} already shown", index);
            wildcards[index].gameObject.SetActive(true);
        }
        private void ReplaceWildcard(int index, Transform with)
        {
            if(!wildcards[index].gameObject.activeSelf)
                Debug.LogWarningFormat("Wildcard {0} already hidden", index);
            //else
            //    Debug.LogWarningFormat("Wildcard {0} not hidden, hiding", index);
            wildcards[index].gameObject.SetActive(false);

            with.SetParent(playerContainer);
            with.SetSiblingIndex(wildcards[index].GetSiblingIndex());
        }

        private void RedrawMoveButtons()
        {
            int lastRole = -1;

            int offset = 0; // could be used for dummy objects

            for(int i = offset; i < playerContainer.childCount; i++)
            {
                Transform child = playerContainer.GetChild(i);

                CNMPlayer player = child.GetComponent<CNMPlayer>();

                if(player != null)
                {
                    // Debug.Log("++++++++++++++ Is a player");
                    int thisRole = player.role;
                    
                    if(thisRole <= lastRole)
                    {
                        // Debug.LogError("Invalid role order");
                        return;
                    }
                    
                    lastRole = thisRole;

                    if(NetworkServer.active)
                    {
                        bool isFirst = i == offset;
                        player.moveUp.gameObject.SetActive(!isFirst);
    
                        bool showDown = thisRole < maxPlayers || i < playerContainer.childCount - 1;
                        //player.moveDown.interactable = !isLast;
                        player.moveDown.gameObject.SetActive(showDown);
                    }

                    // TODO
                }
                else
                {
                    //Debug.Log("-------------- Is a wildcard");

                }

            }
        }
    }
}