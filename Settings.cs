using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Settings
{
    public int speedDuration = 20;
    public int superspeedDuration = 10;
    public int timewarpDuration = 20;
    public int colorDuration = 20;

    public float speedValue = 1.25f;
    public float superspeedValue = 1.5f;
    public float timewarpValue = 0.5f;

    public bool showUserText = true;

    public bool chatReply = true;

    public string botMessagePrefix = "[Bot] ";

    public bool speedEnabled = true;
    public bool superspeedEnabled = true;
    public bool timewarpEnabled = true;
    public bool colorEnabled = true;

    public bool queueEnabled = true;


}
