using Godot;
using System;

public class Spawner : Spatial
{
    float timeToSpawn;
    bool hasSpawn = false;
    public override void _Ready()
    {
        timeToSpawn = 20.0f;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (hasSpawn == false)
        {
            timeToSpawn -= delta;
            if (timeToSpawn <= 0)
            {
                Node node = GD.Load<PackedScene>("res://Scenes/SimpleIA.tscn").Instance();
                ((Spatial)node).Translation = GlobalTransform.origin;
                GetTree().CurrentScene.AddChild(node);
                hasSpawn = true;
            }
        }
    }
}
