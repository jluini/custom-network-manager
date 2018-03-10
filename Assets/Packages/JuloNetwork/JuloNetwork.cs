
using System;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Network {
	
	public class NetworkUtils {

		/// 
		/// Returns a client object by its network id,
		/// or null if its not found.
		/// 
		public static T GetById<T>(NetworkInstanceId id) where T : NetworkBehaviour {
			GameObject obj = ClientScene.FindLocalObject(id);
			if(obj != null) {
				T ret = obj.GetComponent<T>();
				if(ret == null) {
					throw new ApplicationException("Object found but hasn't component " + typeof(T));
				}
				return ret;
			} else {
				return null;
			}
		}
	}

}
