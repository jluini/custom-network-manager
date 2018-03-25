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
        public ScrollRect scroll;

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

                foreach(ChatMessage m in lastMessages)
                {
                    // refresh message time
                    m.time = DateTime.Now;
                }

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

            bool isFirst = true;
            foreach(ChatMessage msg in lastMessages)
            {
                if(!isFirst)
                    builder.Append('\n');
                AppendMessage(builder, msg);
                isFirst = false;
            }

            chatContent.text = builder.ToString();
            StartCoroutine("ScrollToBottomDelayed");
        }

        private IEnumerator ScrollToBottomDelayed()
        {
            // wait a frame
            yield return null;

            // scroll to bottom
            scroll.normalizedPosition = new Vector2(0f, 0f);

            yield break;
        }

        private void AppendMessage(StringBuilder builder, ChatMessage message)
        {
            string name = message.emisor.playerName;
            string text = message.text;
            Color color = message.color;
            
            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGBA(color));
            builder.Append("><<b>");
            AppendEscaped(builder, name);
            builder.Append("</b> says> ");
            AppendEscaped(builder, text);
            builder.Append("</color>");
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
