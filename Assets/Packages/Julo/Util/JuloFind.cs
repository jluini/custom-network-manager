
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Julo.Util {
	
	public class JuloFind {
		
		public static T singleton<T>(bool required = true) where T : Component {
			List<T> all = allWithComponent<T>();
			
			if(all.Count == 1) {
				return all[0];
			} else if(all.Count == 0) {
				if(required) {
					throw new NotFoundException();
				} else {
					return null;
				}
			} else {
				throw new MoreThanOneException();
			}
		}
		
		public static GameObject byName(string name, Component context = null) {
			return byName<Transform>(name, context).gameObject;
		}
		public static T byName<T>(string name, Component context = null, bool required = true) where T : Component {
			if(context == null) {
				return oneWithName<T>(name, required);
			} else {
				return oneWithNameWithin<T>(name, context, required);
			}
		}
		public static List<T> allByName<T>(string name, Component context = null) where T : Component {
			if(context == null) {
				return allWithName<T>(name);
			} else {
				return allWithNameWithin<T>(name, context);
			}
		}
		public static T byTag<T>(string tag, Component context = null) where T : Component {
			if(context == null) {
				return oneWithTag<T>(tag);
			} else {
				return oneWithTagWithin<T>(tag, context);
			}
		}
		public static List<T> allByTag<T>(string tag, Component context = null) where T : Component {
			if(context == null) {
				return allWithTag<T>(tag);
			} else {
				return allWithTagWithin<T>(tag, context);
			}
		}
		
		private static T getComponent<T>(GameObject obj) where T : Component {
			T ret = obj.GetComponent<T>();
			
			if(ret == null)
				throw new ApplicationException("GameObject '" + obj + "' hasn't component '" + typeof(T) + "'");
			
			return ret;
		}
		
		private static List<T> allWithNameWithin<T>(string name, Component context) where T : Component {
			List<T> ret = new List<T>();
			
			foreach(Transform t in context.transform) {
				GameObject obj = t.gameObject;
				if(obj.name == name) {
					ret.Add(getComponent<T>(obj));
				}
				foreach(T nested in allWithNameWithin<T>(name, t)) {
					ret.Add(nested);
				}
			}
			
			return ret;
		}
		private static T oneWithNameWithin<T>(string name, Component context, bool required) where T : Component {
			List<T> all = allWithNameWithin<T>(name, context);
			
			if(all.Count == 0) {
				if(required) {
					throw new ApplicationException("No object named '" + name + "' within " + context);
				} else {
					return null;
				}
			} else if(all.Count > 1) {
				throw new ApplicationException("More than one object named '" + name + "' within " + context);
			} else {
				return all[0];
			}
		}
		private static List<T> allWithName<T>(string name) where T : Component {
			List<T> ret = new List<T>();
			
			foreach(GameObject o in sceneRootObjects()) {
				if(o.name == name) {
					ret.Add(getComponent<T>(o));
				}
				foreach(T rec in allWithNameWithin<T>(name, o.transform)) {
					ret.Add(rec);
				}
			}
			
			return ret;
		}
		public static List<T> allWithComponent<T>() where T : Component {
			List<T> ret = new List<T>();
			
			foreach(GameObject o in sceneRootObjects()) {
				//GameObject obj = t.gameObject;
				T comp = o.GetComponent<T>();
				if(comp != null) {
					ret.Add(comp);
				}
				foreach(T nested in allDescendants<T>(o.transform)) {
					ret.Add(nested);
				}
			}
			
			return ret;
		}
		private static T oneWithName<T>(string name, bool required = true) where T : Component {
			List<T> all = allWithName<T>(name);
			
			if(all.Count == 0) {
				if(required) {
					// TODO throw custom exception...
					throw new ApplicationException("No object named '" + name + "' in scene");
				} else {
					return null;
				}
			} else if(all.Count > 1) {
				throw new ApplicationException("More than one object named '" + name + "' in scene");
			} else {
				return all[0];
			}
		}
		public static T ancestor<T>(Component context) where T : Component {
			T comp = context.GetComponent<T>();
			if(comp != null) {
				return comp;
			} else if(context.transform.parent) {
				return ancestor<T>(context.transform.parent);
			} else {
				return null;
			}
		}
		public static T descendant<T>(Component context) where T : Component {
			T comp = context.GetComponent<T>();
			if(comp != null) {
				return comp;
			}
			foreach(Transform child in context.transform) {
				return descendant<T>(child);
			}
			return null;
		}
		public static List<T> allDescendants<T>(Component context) where T : Component {
			List<T> ret = new List<T>();
			
			foreach(Transform t in context.transform) {
				//GameObject obj = t.gameObject;
				T comp = t.GetComponent<T>();
				if(comp != null) {
					ret.Add(comp);
				}
				foreach(T nested in allDescendants<T>(t)) {
					ret.Add(nested);
				}
			}
			
			return ret;
		}
		/*
		public static GameObject getChild(string name, Transform parent) {
			Transform tr = parent.Find(name);
			if(tr == null)
				throw new ApplicationException("Child '" + name + "' of '" + parent + "' not found");
			
			return tr.gameObject;
		}
		public static T getChild<T>(string name, Transform parent) {
			GameObject obj = getChild(name, parent);
			T comp = obj.GetComponent<T>();
			
			if(comp == null)
				throw new ApplicationException("GameObject '" + name + "' hasn't component " + typeof(T));
			
			return comp;
		}
		*/
		private static List<T> allWithTagWithin<T>(string tag, Component context) where T : Component {
			List<T> ret = new List<T>();
			
			foreach(Transform t in context.transform) {
				GameObject obj = t.gameObject;
				if(obj.tag == tag) {
					ret.Add(getComponent<T>(obj));
				}
				foreach(T rec in allWithTagWithin<T>(tag, t)) {
					ret.Add(rec);
				}
			}
			
			return ret;
		}
		private static T oneWithTagWithin<T>(string tag, Component context) where T : Component {
			List<T> all = allWithTagWithin<T>(tag, context);
			
			if(all.Count == 0) {
				throw new ApplicationException("No object tagged '" + tag + "' within " + context);
			} else if(all.Count > 1) {
				throw new ApplicationException("More than one object tagged '" + tag+ "' within " + context);
			}
			
			return all[0];
		}
		private static List<T> allWithTag<T>(string tag) where T : Component {
			List<T> ret = new List<T>();
			
			foreach(GameObject o in sceneRootObjects()) {
				if(o.tag == tag) {
					ret.Add(getComponent<T>(o));
				}
				foreach(T rec in allWithTagWithin<T>(tag, o.transform)) {
					ret.Add(rec);
				}
			}
			
			return ret;
		}
		private static T oneWithTag<T>(string tag) where T : Component {
			List<T> all = allWithTag<T>(tag);
			
			if(all.Count == 0) {
				throw new ApplicationException("No object tagged '" + tag + "' in scene");
			} else if(all.Count > 1) {
				throw new ApplicationException("More than one object tagged '" + tag + "' in scene");
			}
			
			return all[0];
		}
		
		private static GameObject[] sceneRootObjects() {
			try {
				return SceneManager.GetActiveScene().GetRootGameObjects();
			} catch(ArgumentException e) {
				Debug.LogWarning("GetRootGameObjects: " + e.Message);
				return new GameObject[] {};
			}
		}
	}
	
	public class NotFoundException : Exception {
		
	}
	
	public class MoreThanOneException : Exception {
		
	}
}
