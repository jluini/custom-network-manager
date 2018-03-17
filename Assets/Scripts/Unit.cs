using System.Collections;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Util;

namespace Julo.CNMProto
{
    public class Unit : NetworkBehaviour
    {
        [SyncVar]
        public int playerRole = -1;

        [SyncVar]
        public NetworkInstanceId playerNetId;

        [Header("Hooks")]

        public new SpriteRenderer renderer;

        public override void OnStartClient()
        {
            CNManager manager = CNManager.Instance;
            //CNMPlayer player = (CNMPlayer)manager.GetPlayer(playerId);
            CNMPlayer player = ClientScene.objects[playerNetId].GetComponent<CNMPlayer>();
            Color color = manager.colors[player.playerColorNum];
            SetColor(color);
        }

        private void SetColor(Color newColor)
        {
            if(renderer != null)
                renderer.color = newColor;
        }

        // called on client when object destroyed by server
        public override void OnNetworkDestroy()
        {
            base.OnNetworkDestroy();
            // JuloDebug.Log(string.Format("Unit {0} is network destroyed", netId));
        }
    }
}

