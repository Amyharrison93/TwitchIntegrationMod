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


namespace TwitchIntegrationScript
{
    public class TwitchBot
    {

        public static bool colorEnabled = false;

        private static Client client;

        private static float timelast;

        //private static List<Material> sharedMaterials;
        private static Material leftMaterial;
        private static Material rightMaterial;

        private static Material leftIndicator;
        private static Material rightIndicator;

        public static List<TrackData> tracks;

        public static List<string> queue;

        private static Credentials mycredentials;

        public static Color leftColor;
        public static Color rightColor;
        public static Color twoColor;
        public static Color oneColor;

        private static Color mleftColor;
        private static Color mrightColor;

        public static void Setup()
        {
            colorEnabled = false;

            timelast = 0;

            queue = new List<string>();

            tracks = null;

            //get file path
            var dataPath = Application.dataPath;
            var filePath = dataPath.Substring(0, dataPath.LastIndexOf('/')) + "/twith.auth.bin";

            //load  
            string text = "";
            using (StreamReader streamReader = new StreamReader(filePath))
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

        public static void Update()
        {
            if (mleftColor != null && mrightColor != null && leftIndicator != null && rightIndicator)
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
            switch (e.Command.CommandText)
            {
                case "Colour":
                case "colour":
                case "Color":
                case "color":
                    ChangeColor();
                    break;
                case "Queue":
                case "queue":
                    PrintQueue();
                    break;
                case "SRR":
                case "srr":
                    log("Request: " + e.Command.ArgumentsAsString);
                    AddRequest(e.Command.ArgumentsAsList);
                    break;
                default:
                    break;
            }
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
            SendMessage(tmp);
        }

        private static void AddRequest(List<string> request)
        {
            if (request.Count > 0)
            {
                if (tracks != null)
                {
                    int lastTrackSearchScore = 0;
                    List<TrackData> c = new List<TrackData>();
                    foreach (TrackData t in tracks)
                    {
                        int x = 0;
                        foreach (string s in request)
                        {
                            if (t.name.ToLower().Contains(s.ToLower()))
                            {
                                x++;
                            }
                        }
                        if (x > lastTrackSearchScore)
                        {
                            c = new List<TrackData>();
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
                                tmp += c[i].name + ", ";
                            }
                            if (c.Count > 10)
                            {
                                tmp += " and more";
                            }
                            SendMessage(tmp);
                        }
                        else
                        {
                            if (!queue.Contains(c[0].name))
                            {
                                queue.Add(c[0].name);
                                SendMessage(c[0].name + " added to the queue");
                                RequestButton.UpdateText();
                            }
                            else
                            {
                                SendMessage(c[0].name + " already in the queue");
                            }
                        }
                    }
                    else
                    {
                        SendMessage("No maps found");
                    }
                }
            }
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
                    if (mrs[0].sharedMaterial.GetColor("_EmissionColor") == leftColor)
                    {
                        leftMaterial = mrs[0].sharedMaterial;
                    }
                    else if (mrs[0].sharedMaterial.GetColor("_EmissionColor") == rightColor)
                    {
                        rightMaterial = mrs[0].sharedMaterial;
                    }
                }
            }

            leftIndicator = GameObject.Find("Left Indicator").GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial;
            rightIndicator = GameObject.Find("Right Indicator").GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial;

        }

        private static void ChangeColor()
        {
            if (colorEnabled)
            {
                if (leftMaterial == null || rightMaterial == null || leftIndicator == null || rightIndicator == null)
                {
                    GetMaterials();
                }

                if (Time.time - timelast > 15 && leftMaterial != null && rightMaterial != null && leftIndicator != null && rightIndicator != null)
                {
                    SendMessage("Randomizing Color");
                    timelast = Time.time;

                    int rand = UnityEngine.Random.Range(0, 1000);

                    Color col = Color.HSVToRGB(rand / 1000f, 1, 1);
                    if (col == leftColor)
                    {
                        rand = (rand + 611) % 1000;
                        col = Color.HSVToRGB(rand / 1000f, 1, 1);
                    }
                    mleftColor = col;
                    leftMaterial.SetColor("_EmissionColor", col);
                    leftIndicator.SetColor("_EmissionColor", col);

                    rand = (rand + 611) % 1000;
                    col = Color.HSVToRGB(rand / 1000f, 1, 1);
                    if (col == rightColor)
                    {
                        rand = (rand + 611) % 1000;
                        col = Color.HSVToRGB(rand / 1000f, 1, 1);
                    }
                    mrightColor = col;
                    rightMaterial.SetColor("_EmissionColor", col);
                    rightIndicator.SetColor("_EmissionColor", col);
                }
                else
                {
                    SendMessage("Color command on cooldown for " + (15 - (int)(Time.time - timelast)).ToString() + " seconds");
                }
            }
            else
            {
                SendMessage("Command disabled");
            }
        }

        public static void SendMessage(string message)
        {
            client.SendMessage(mycredentials.Channel, "[Bot] " + message);
        }

        public static void Disconnect()
        {
            client.Disconnect();
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
