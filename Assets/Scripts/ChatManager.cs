using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Julo.CNMProto
{
    public class ChatManager : MonoBehaviour
    {
        public int maxMessages = 10;
        public float chatMemory = 7f;
        public float cleanInterval = 2f;
        public bool cleanMessages = false;

        public Text chatContent;

        private List<ChatMessage> lastMessages = new List<ChatMessage>();

        private bool isCleaning;

        public void NewMessage(ChatMessage message)
        {
            lastMessages.Add(message);

            if(lastMessages.Count > maxMessages)
                lastMessages.RemoveAt(0);

            RedrawMessages();
            
            if(cleanMessages && !isCleaning)
            {
                isCleaning = true;
                StartCoroutine("CleanChat");
            }
        }

        public void StartCleaning()
        {
            cleanMessages = true;
            if(!isCleaning)
            {
                isCleaning = true;
                StartCoroutine("CleanChat");
            }
        }

        private IEnumerator CleanChat()
        {
            while(lastMessages.Count > 0 && cleanMessages)
            {
                yield return new WaitForSeconds(cleanInterval);

                bool changed = false;
                bool done = false;

                do {
                    DateTime now = DateTime.Now;
                    DateTime oldestMessageTime = lastMessages[0].time;
                    
                    TimeSpan ellapsed = now - oldestMessageTime;
                    if(ellapsed.TotalSeconds >= chatMemory)
                    {
                        lastMessages.RemoveAt(0);
                        changed = true;
                        done = (lastMessages.Count == 0);
                    }
                    else
                    {
                        done = true;
                    }
                } while(!done);

                if(changed)
                {
                    RedrawMessages();
                }
            }

            isCleaning = false;
            yield break;
        }

        private void RedrawMessages()
        {
            StringBuilder builder = new StringBuilder();

            foreach(ChatMessage msg in lastMessages)
            {
                AppendMessage(builder, msg);
            }

            chatContent.text = builder.ToString();
        }

        private void AppendMessage(StringBuilder builder, ChatMessage message)
        {
            string name = message.emisor.playerName;
            string text = message.text;
            Color color = CNManager.Instance.colors[message.emisor.playerColorNum];
            
            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGBA(color));
            builder.Append("><<b>");
            AppendEscaped(builder, name);
            builder.Append("</b> says> ");
            AppendEscaped(builder, text);
            builder.Append("</color>");
            builder.Append('\n');
        }
        
        public static void AppendEscaped(StringBuilder builder, string original)
        {
            for(int i = 0; i < original.Length; i++)
            {
                if(original[i] == '<' || original[i] == '>')
                    builder.Append(' ');
                else
                    builder.Append(original[i]);
            }
        }
    }
}
