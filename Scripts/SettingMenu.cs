using Godot;
using System;

public class SettingMenu : Panel
{
    public override void _Ready()
    {
        GetNode<Slider>("VBoxContainer/Master").Value = GetVolume("Master");
        GetNode<Slider>("VBoxContainer/Effect").Value = GetVolume("Effect");
        GetNode<Slider>("VBoxContainer/Music").Value = GetVolume("Music");
        GetNode<Slider>("VBoxContainer/Ambiance").Value = GetVolume("Ambiance");
    }

    public float GetVolume(string bus)
    {
        int id = AudioServer.GetBusIndex(bus);
        return AudioServer.GetBusVolumeDb(id);
    }

    public void SetVolume(string bus, float db)
    {
        int id = AudioServer.GetBusIndex(bus);
        AudioServer.SetBusVolumeDb(id, db);
    }

    public void ChangeMaster(float value)
    {
        SetVolume("Master", value);
    }

    public void ChangeEffect(float value)
    {
        SetVolume("Effect", value);
    }

    public void ChangeMusic(float value)
    {
        SetVolume("Music", value);
    }

    public void ChangeAmbiance(float value)
    {
        SetVolume("Ambiance", value);
    }
}
