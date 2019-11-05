using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public struct PlayerMessage
    {
        public string message; 
        public Color colorToUse;
        public float duration;
        public float fadeIn;
        public float fadeOut;

        public PlayerMessage(string message, float duration, float fadeIn, float fadeOut, Color colorToUse)
        {
            this.message = message;
            this.colorToUse = colorToUse;
            this.duration = duration;
            this.fadeIn = fadeIn;
            this.fadeOut = fadeOut;
        }
    }
