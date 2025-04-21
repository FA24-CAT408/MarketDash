using System;


[Serializable]
public class GameSettingsData
{
    public int SaveDataVersion = 1;
    public float Sensitivity = 1.0f;
    public float Volume = 0.5f;
    public bool InvertCamera = false;
}