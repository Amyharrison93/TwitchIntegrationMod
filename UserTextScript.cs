using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchIntegrationScript
{
    public class UserTextScript : MonoBehaviour
    {
        public Text text;
        public Canvas canvas;

        public static UserTextScript s_instance;

        void Awake()
        {
            if (s_instance != null)
            {
                Destroy(this);
            }

            s_instance = this;
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            if (s_instance == null) { return; }

            HideMe();
        }

        public static void SetText(string user)
        {
            if (!s_instance) { return; }

            s_instance.text.text = user;
        }

        public static void ShowMe()
        {
            if (!s_instance) { return; }

            s_instance.canvas.enabled = true;
        }

        public static void HideMe()
        {
            if (!s_instance) { return; }

            s_instance.canvas.enabled = false;
        }

        public static void DestroyMe()
        {
            if (s_instance == null) { return; }

            Destroy(s_instance);
        }
    }
}
