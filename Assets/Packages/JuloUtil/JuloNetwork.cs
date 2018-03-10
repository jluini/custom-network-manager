
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Util {
	
	public class JuloNetwork {
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
