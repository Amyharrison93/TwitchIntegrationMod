using System.Collections.Generic;
using UnityEngine;
using UMod;
using Synth.mods.utils;
using System.IO;
using Synth.mods.info;
using TwitchLib.Client.Models;
using TwitchLib.Unity;
using System;
using TwitchLib.Client.Events;
using Synth.mods.interactions;
using Synth.mods.events;
using System.Collections;

namespace TwitchIntegrationScript
{
    public class TwitchBot : MonoBehaviour
    {

        public static Settings settings;

        private static float colorTime;
        private static bool colorRunning;
        private static float speedTime;
        private static bool speedRunning;
        private static float nameTime;
        private static bool nameRunning;

        public static GameObject userText;

        private static Client client;

        public static bool inLevel;

        private static Material leftMaterial;
        private static Material rightMaterial;

        private static Material leftIndicator;
        private static Material rightIndicator;

        public static List<string> tracks;

        public static List<string> queue;

        private static Credentials mycredentials;

        public static Color leftColor;
        public static Color rightColor;

        public static Color mleftColor;
        public static Color mrightColor;

        public static Action<float> setPitchCallback;
        public static Action looseLifeCallback;
        public static Action disableScoreCallback;

        public static TwitchBot s_instance;

        void Awake()
        {
            if (s_instance != null)
            {
                Destroy(this);
            }

            s_instance = this;
            DontDestroyOnLoad(this);

            Setup();
        }

        public static void Setup()
        {
            colorTime = 0;
            colorRunning = false;
            speedTime = 0;
            speedRunning = false;
            nameTime = 0;
            nameRunning = false;

            queue = new List<string>();

            tracks = null;

            //get file path
            var dataPath = Application.dataPath;
            var credentialsPath = dataPath.Substring(0, dataPath.LastIndexOf('/')) + "/twith.auth.bin";
            var settingsPath = dataPath.Substring(0, dataPath.LastIndexOf('/')) + "/TwitchSettings.json";

            if (File.Exists(settingsPath))
            {
                //load  
                string str = "";
                using (StreamReader streamReader = new StreamReader(settingsPath))
                {
                    str = streamReader.ReadToEnd();
                }

                //deserialize
                settings = JsonUtility.FromJson<Settings>(str);
            }
            else
            {
                settings = new Settings();
            }

            RequestButton.UpdateButtons();

            //load  
            string text = "";
            using (StreamReader streamReader = new StreamReader(credentialsPath))
            {
                text = streamReader.ReadToEnd();
            }

            //deserialize
            mycredentials = JsonUtility.FromJson<Credentials>(text);
            if (mycredentials.Channel == null || mycredentials.Channel == "")
            {
                mycredentials.Channel = mycredentials.Username;
            }
            else if (mycredentials.Username == null || mycredentials.Username == "")
            {
                mycredentials.Username = mycredentials.Channel;
            }

            ConnectionCredentials credentials = new ConnectionCredentials(mycredentials.Username, mycredentials.OauthKey);
            client = new Client();
            client.Initialize(credentials, mycredentials.Channel);
            client.OnChatCommandReceived += OnChatCommandReceived;
            client.OnMessageReceived += OnMessageReceived;
            client.OnLog += OnLog;
            client.Connect();
        }

        public void Update()
        {
            if (s_instance == null) { return; }

            float time = Time.time;

            if (colorRunning)
            {
                if (colorTime - time < 0 && colorTime - time > -60)
                {
                    if (leftMaterial == null || rightMaterial == null || leftIndicator == null || rightIndicator == null)
                    {
                        GetMaterials();
                    }

                    else
                    {
                        colorRunning = false;
                        mleftColor = leftColor;
                        leftMaterial.SetColor("_EmissionColor", leftColor);
                        leftIndicator.SetColor("_EmissionColor", leftColor);

                        mrightColor = rightColor;
                        rightMaterial.SetColor("_EmissionColor", rightColor);
                        rightIndicator.SetColor("_EmissionColor", rightColor);
                    }
                }
            }

            if (speedRunning)
            {
                if (speedTime - time < 0 && speedTime - time > -60)
                {
                    speedRunning = false;
                    setPitchCallback(1f);
                }
            }

            if (nameRunning)
            {
                if (nameTime - time < 0 && nameTime - time > -60)
                {
                    nameRunning = false;
                    UserTextScript.HideMe();
                }
            }

            if (mleftColor != null && mrightColor != null && leftIndicator != null && rightIndicator != null)
            {
                if (leftIndicator.GetColor("_EmissionColor") != mleftColor || rightIndicator.GetColor("_EmissionColor") != mrightColor)
                {
                    leftIndicator.SetColor("_EmissionColor", mleftColor);
                    rightIndicator.SetColor("_EmissionColor", mrightColor);
                }
            }
        }

        private static void OnLog(object sender, OnLogArgs e)
        {
            log(e.Data);
        }

        private static void OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {

        }

        private static void OnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            log("Command " + e.Command.CommandText);

            if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
            {
                switch (e.Command.CommandText)
                {
                    case "add":
                        log("Add: " + e.Command.ArgumentsAsString);
                        AddRequest(e.Command.ArgumentsAsList);
                        break;
                    case "top":
                        log("Top: " + e.Command.ArgumentsAsString);
                        QueueNext(e.Command.ArgumentsAsList);
                        break;
                    case "remove":
                        log("Remove: " + e.Command.ArgumentsAsString);
                        RemoveRequest(e.Command.ArgumentsAsList);
                        break;
                    case "enable":
                        SetCommand(true, e.Command.ArgumentsAsList);
                        break;
                    case "disable":
                        SetCommand(false, e.Command.ArgumentsAsList);
                        break;
                    default:
                        break;
                }
            }


            switch (e.Command.CommandText)
            {
                case "Colour":
                case "colour":
                case "Color":
                case "color":
                    if (settings.colorEnabled) { ColorCommand(settings.colorDuration, e.Command.ChatMessage.Username); }
                    else { SendChatMessage("Command disabled"); }
                    break;
                case "Queue":
                case "queue":
                    PrintQueue();
                    break;
                case "Speed":
                case "speed":
                    if (settings.speedEnabled) { SpeedCommand(settings.speedValue, settings.speedDuration, e.Command.ChatMessage.Username); }
                    else { SendChatMessage("Command disabled"); }
                    break;
                case "Superspeed":
                case "SuperSpeed":
                case "superspeed":
                    if (settings.superspeedEnabled) { SpeedCommand(settings.superspeedValue, settings.superspeedDuration, e.Command.ChatMessage.Username); }
                    else { SendChatMessage("Command disabled"); }
                    break;
                case "Timewarp":
                case "TimeWarp":
                case "timewarp":
                    if (settings.timewarpEnabled) { SpeedCommand(settings.timewarpValue, settings.timewarpDuration, e.Command.ChatMessage.Username); }
                    else { SendChatMessage("Command disabled"); }
                    break;
                case "SRR":
                case "srr":
                    if (settings.queueEnabled)
                    {
                        log("Request: " + e.Command.ArgumentsAsString);
                        AddRequest(e.Command.ArgumentsAsList);
                    }
                    else { SendChatMessage("Queue closed"); }

                    break;
                default:
                    break;
            }
        }

        public static void SetCommand(bool val, List<string> parameters)
        {
            string str = parameters[0];
            if (val == true)
            {
                str += " enabled";
            }
            else
            {
                str += " disabled";
            }

            if (parameters[0].Equals("speed"))
            {
                settings.speedEnabled = val;
            }
            else if (parameters[0].Equals("superspeed"))
            {
                settings.superspeedEnabled = val;
            }
            else if (parameters[0].Equals("timewarp"))
            {
                settings.timewarpEnabled = val;
            }
            else if (parameters[0].Equals("color"))
            {
                settings.colorEnabled = val;
            }
            else if (parameters[0].Equals("queue"))
            {
                settings.queueEnabled = val;
            }
            else if (parameters[0].Equals("reply"))
            {
                settings.chatReply = val;
            }

            RequestButton.UpdateButtons();
            SendChatMessage(str);
        }

        public static void PrintQueue()
        {
            string tmp = queue.Count.ToString() + " songs in the queue: ";
            for (int i = 0; i < queue.Count && i < 10; i++)
            {
                tmp += queue[i] + ", ";
            }
            if (queue.Count > 10)
            {
                tmp += " and more";
            }
            SendChatMessage(tmp);
        }

        private static void AddRequest(List<string> requests)
        {
            string c = SearchRequests(tracks, requests);
            if (!c.Equals(""))
            {
                if (!queue.Contains(c))
                {
                    queue.Add(c);
                    SendChatMessage(c + " added to the queue");
                    RequestButton.UpdateText();
                }
                else
                {
                    SendChatMessage(c + " already in the queue");
                }
            }
        }

        private static void QueueNext(List<string> requests)
        {
            string c = SearchRequests(tracks, requests);
            if (!c.Equals(""))
            {
                if (queue.Contains(c))
                {
                    queue.Remove(c);
                    SendChatMessage(c + " moved to the front");
                }
                else
                {
                    SendChatMessage(c + " added to the front");
                }
                queue.Insert(0, c);
                RequestButton.UpdateText();
            }
        }

        private static void RemoveRequest(List<string> requests)
        {
            string c = SearchRequests(queue, requests);
            if (!c.Equals(""))
            {
                if (queue.Contains(c))
                {
                    queue.Remove(c);
                    SendChatMessage(c + " removed from the queue");
                    RequestButton.UpdateText();
                }
                else
                {
                    SendChatMessage(c + " already removed");
                }
            }
        }

        private static string SearchRequests(List<string> tracks, List<string> request)
        {
            if (request != null && request.Count > 0)
            {
                if (tracks != null)
                {
                    string par = "";
                    for (int i = 0; i < request.Count - 1; i++)
                    {
                        par += request[i] + " ";
                    }
                    par += request[request.Count - 1];

                    int lastTrackSearchScore = 0;
                    List<string> c = new List<string>();
                    foreach (string t in tracks)
                    {
                        if (t.ToLower().Equals(par.ToLower()))
                        {
                            return par;
                        }

                        int x = 0;
                        foreach (string s in request)
                        {
                            if (t.ToLower().Contains(s.ToLower()))
                            {
                                x++;
                            }
                        }
                        if (x > lastTrackSearchScore)
                        {
                            c = new List<string>();
                            c.Add(t);
                            lastTrackSearchScore = x;
                        }
                        else if (x != 0 && x == lastTrackSearchScore)
                        {
                            c.Add(t);
                        }
                    }

                    if (c.Count > 0)
                    {
                        if (c.Count > 1)
                        {
                            string tmp = c.Count.ToString() + " alternatives found: ";
                            for (int i = 0; i < c.Count && i < 10; i++)
                            {
                                tmp += c[i] + ", ";
                            }
                            if (c.Count > 10)
                            {
                                tmp += " and more";
                            }
                            SendChatMessage(tmp);
                        }
                        else
                        {
                            return c[0];
                        }
                    }
                    else
                    {
                        SendChatMessage("No matches found");
                    }
                }
            }
            return "";
        }

        public static void GetMaterials()
        {
            GameObject notesParent = GameObject.Find("[SongTrack]");
            int count = notesParent.transform.GetChild(0).transform.childCount;

            for (int i = 0; i < count; i++)
            {
                Transform note = notesParent.transform.GetChild(0).transform.GetChild(i);

                MeshRenderer[] mrs = note.GetComponentsInChildren<MeshRenderer>(true);

                if (mrs[0].sharedMaterial != null)
                {
                    if (mrs[0].sharedMaterial.name.Equals("LeftHandNoteMat"))
                    {
                        leftMaterial = mrs[0].sharedMaterial;
                        if (leftColor == null)
                        {
                            leftColor = leftMaterial.GetColor("_EmissionColor");
                        }
                    }
                    else if (mrs[0].sharedMaterial.name.Equals("RightHandNoteMat"))
                    {
                        rightMaterial = mrs[0].sharedMaterial;
                        if (rightColor == null)
                        {
                            rightColor = rightMaterial.GetColor("_EmissionColor");
                        }
                    }
                }
            }

            leftIndicator = GameObject.Find("Left Indicator").GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial;
            rightIndicator = GameObject.Find("Right Indicator").GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial;
        }

        private static void ColorCommand(float time, string user)
        {
            if (inLevel && !colorRunning)
            {
                if (leftMaterial == null || rightMaterial == null || leftIndicator == null || rightIndicator == null)
                {
                    GetMaterials();
                }

                if (leftMaterial != null || rightMaterial != null || leftIndicator != null || rightIndicator != null)
                {
                    SendChatMessage("Randomizing Color");
                    NameCommand(user, time);
                    colorTime = Time.time + time;
                    colorRunning = true;

                    int rand = UnityEngine.Random.Range(0, 1000);

                    Color col = Color.HSVToRGB(rand / 1000f, 1, 1);

                    mleftColor = col;
                    leftMaterial.SetColor("_EmissionColor", col);
                    leftIndicator.SetColor("_EmissionColor", col);

                    rand = (rand + 611) % 1000;
                    col = Color.HSVToRGB(rand / 1000f, 1, 1);

                    mrightColor = col;
                    rightMaterial.SetColor("_EmissionColor", col);
                    rightIndicator.SetColor("_EmissionColor", col);
                }
            }
            else
            {
                SendChatMessage("Command unavailable");
            }
        }

        public static void SpeedCommand(float speed, float time, string user)
        {
            if (inLevel && !speedRunning)
            {
                if (speed < 1)
                {
                    disableScoreCallback();
                }

                SendChatMessage("Speed " + speed.ToString() + "x");
                NameCommand(user, time / speed);
                speedTime = Time.time + time;
                speedRunning = true;

                setPitchCallback(speed);
            }
            else
            {
                SendChatMessage("Command unavailable");
            }
        }

        public static void NameCommand(string user, float time)
        {
            if (settings.showUserText == true)
            {
                nameTime = Time.time + time;
                nameRunning = true;

                GameObject platform = GameObject.Find("HeadsetFollower");
                userText.transform.position = platform.transform.position;

                UserTextScript.SetText(user);
                UserTextScript.ShowMe();
            }
        }

        public static void SendChatMessage(string message)
        {
            if (settings.chatReply == true)
            {
                client.SendMessage(mycredentials.Channel, settings.botMessagePrefix + message);
            }
        }

        public void OnDestroy()
        {
            Disconnect();
        }

        public static void Disconnect()
        {
            client.Disconnect();

            var dataPath = Application.dataPath;
            var settingsPath = dataPath.Substring(0, dataPath.LastIndexOf('/')) + "/TwitchSettings.json";

            client.OnChatCommandReceived -= OnChatCommandReceived;
            client.OnMessageReceived -= OnMessageReceived;
            client.OnLog -= OnLog;

            using (StreamWriter streamWriter = new StreamWriter(settingsPath))
            {
                streamWriter.Write(JsonUtility.ToJson(settings, true));
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
