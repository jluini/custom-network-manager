using System.Collections;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Util;

namespace Julo.CNMProto
{
    public class Unit : NetworkBehaviour
    {

        public int playerId = -1;

        public override void OnStartClient()
        {
            Debug.Log("Unit::OnStartClient()");
        }

        // called on client when object destroyed by server
        public override void OnNetworkDestroy()
        {
            base.OnNetworkDestroy();
            JuloDebug.Log(string.Format("Unit {0} is network destroyed", netId));
        }
    }
}

