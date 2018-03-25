using System;
using System.Collections.Generic;
using System.Collections;

using UnityEngine;


namespace Julo.CNMProto
{

    public class ChatMessage
    {
        public CNMPlayer emisor;
        public string text;
        public DateTime time;
        public Color color;

        public ChatMessage(CNMPlayer emisor, string text)
        {
            this.emisor = emisor;
            this.text = text;
            this.time = DateTime.Now;
            this.color = emisor.GetColor();
        }
    }
}
