using Synth.mods.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchIntegrationScript
{
    public class RequestButton : MonoBehaviour
    {

        public Button button;

        public Button colorButton;

        public Text text;

        public Text colorText;

        public GameObject wrapper;

        public GameObject canvas;

        public static RequestButton s_instance;

        private bool canvasSet = false;

        private Action<GameObject> setUICanvasCallback;

        public static Action<GameObject> SetUICanvasCallback
        {
            get
            {
                return s_instance.setUICanvasCallback;
            }

            set
            {
                s_instance.setUICanvasCallback = value;
            }
        }

        private Action<int> setSelectedTrackCallback;

        public static Action<int> SetSelectedTrackCallback
        {
            get
            {
                return s_instance.setSelectedTrackCallback;
            }

            set
            {
                s_instance.setSelectedTrackCallback = value;
            }
        }

        private List<TrackData> tracks;

        public static List<TrackData> Tracks
        {
            get
            {
                return s_instance.tracks;
            }

            set
            {
                s_instance.tracks = value;
            }
        }

        void Awake()
        {
            if (s_instance != null)
            {
                Destroy(this);
            }

            s_instance = this;
            DontDestroyOnLoad(this);
            button.onClick.AddListener(QueueNext);
            colorButton.onClick.AddListener(ToggleColor);
        }

        void Start()
        {
            if (s_instance == null) { return; }

            wrapper.SetActive(false);
        }

        private void Update()
        {

        }

        public static void ShowMe()
        {
            if (!s_instance) { return; }

            s_instance.wrapper.SetActive(true);

            UpdateText();
        }

        public static void HideMe()
        {
            if (!s_instance) { return; }

            s_instance.wrapper.SetActive(false);
        }

        public static void DestroyMe()
        {
            if (s_instance == null) { return; }

            Destroy(s_instance);
        }

        public static void InitCanvasVRTK()
        {
            if (!s_instance.canvasSet && s_instance.setUICanvasCallback != null)
            {
                s_instance.canvasSet = true;
                s_instance.setUICanvasCallback(s_instance.canvas);
            }
        }

        public static void QueueNext()
        {
            UpdateText();

            if (TwitchBot.queue.Count > 0)
            {
                foreach (TrackData t in s_instance.tracks)
                {
                    if (t.name.Equals(TwitchBot.queue[0]))
                    {
                        int index = s_instance.tracks.IndexOf(t);
                        if (index >= 0)
                        {
                            s_instance.setSelectedTrackCallback(index);
                        }
                    }
                }
                TwitchBot.queue.RemoveAt(0);
            }
        }

        public static void UpdateText()
        {
            if (!s_instance) { return; }

            s_instance.text.text = "Queue [" + TwitchBot.queue.Count.ToString() + "]" + Environment.NewLine;
            foreach (string s in TwitchBot.queue)
            {
                s_instance.text.text += s + Environment.NewLine;
            }

        }

        public static void ToggleColor()
        {
            TwitchBot.modificationEnabled = !TwitchBot.modificationEnabled;

            if (TwitchBot.modificationEnabled)
            {
                s_instance.colorText.text = "!Mods on";
            }
            else
            {
                s_instance.colorText.text = "!Mods off";
            }
        }

        private static void log(string str)
        {
            //get file path
            var dataPath = Application.dataPath;
            var filePath = dataPath.Substring(0, dataPath.LastIndexOf('/')) + "/Novalog.txt";

            //write
            using (var streamWriter = new StreamWriter(filePath, true))
            {
                streamWriter.WriteLine(str);
            }
        }
    }
}