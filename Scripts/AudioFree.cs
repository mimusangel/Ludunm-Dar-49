using Godot;
using System;

public class AudioFree : AudioStreamPlayer3D
{
    public override void _Ready()
    {
        
    }

    public void EndSound()
    {
        QueueFree();
    }
}
