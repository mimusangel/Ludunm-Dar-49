using Godot;
using System;

public class MainMenu : VBoxContainer
{
    public override void _Ready()
    {
        
    }

    public void MainScene()
    {
        GetTree().ChangeScene("res://Scenes/MainMenu.tscn");
    }

    public void Play()
    {
        GetTree().ChangeScene("res://Scenes/Game.tscn");
    }

    public void Exit()
    {
        GetTree().Quit();
    }
}
