using Godot;
using System;

public class Pickup : Spatial
{
    public override void _Ready()
    {
        AnimationPlayer anim = GetNode<AnimationPlayer>("AnimationPlayer");
        if (anim != null)
        {
            anim.Play("Loop");
            anim.PlaybackSpeed = 0.35f;
        }
    }
    public void PickupScrap(Node body)
    {
        if (body is Player)
        {
            ((Player)body).AddScrap((int)(5 + GD.Randi() % 10));


            GameData data = GetNode<GameData>("/root/GameData");
            data.SpwanSound(GlobalTransform.origin, "res://Sounds/Effects/pick up.mp3");
            this.QueueFree();
        }
    }

}
