using System;
using System.Collections.Generic;

using UnityEngine;

namespace Julo.Util {
	public class JuloDebug : Singleton<JuloDebug> {
		public Color backgroundColor;
		
		public Color messageColor = Color.white;
		public Color warningColor = Color.yellow;
		public Color errorColor   = Color.red;
		
		public int maxMessages = 20;
		
		public int x = 10;
		public int y = 10;
		public int height = 20;
		public int vspace = 2;
		public int hmargin = 2;
		public int width = 200;
		
		int index = 0;
		
		Message[] messages;
		
		void Awake() {
            DontDestroyOnLoad(gameObject);
			messages = new Message[maxMessages];
			for(int i = 0; i < maxMessages; i++) {
				messages[i] = null;
			}
		}
		
		void OnGUI() {
			if(Event.current.type == EventType.Repaint) {
				for(int i = 0; i < maxMessages; i++) {
					Message msg = messages[i];
					if(msg != null) {
						int m = i;
						Rect backRect = new Rect(x, y + m * (height + vspace), width, height);
						Rect textRect = new Rect(x + hmargin, y + m * (height + vspace), width - hmargin, height);
		
						DrawQuad(backRect, backgroundColor);
						GUI.color = msg.color;
						GUI.Label(textRect, msg.text);
					}
				}
			}
		}
		
		void DrawQuad(Rect position, Color color) {
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0,0,color);
			texture.Apply();
			GUI.skin.box.normal.background = texture;
			GUI.Box(position, GUIContent.none);
		}

		public static void Log(string message)  { if(Instance) Instance.ShowMessage(message); }
		public static void Warn(string message) { if(Instance) Instance.ShowWarning(message); }
		public static void Err(string message)  { if(Instance) Instance.ShowError(message);   }
		
		public void ShowMessage(string message) { Debug.Log(message);        AddMessage(new Message(message, messageColor)); }
		public void ShowWarning(string message) { Debug.LogWarning(message); AddMessage(new Message(message, warningColor)); }
		public void ShowError(string message)   { Debug.LogError(message);   AddMessage(new Message(message, errorColor)); }
		
		void AddMessage(Message msg) {
			messages[index] = msg;
			index = (index + 1) % maxMessages;
		}
		
		class Message {
			public string text;
			public Color color;
			
			public Message(string text, Color color) {
				this.text = text;
				this.color = color;
			}
		}
	}
}