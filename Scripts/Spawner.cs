using Godot;
using System;

public class Spawner : Spatial
{
    [Export]
    public float firstSpawnTime = 5.0f;
    [Export]
    public float timeToSpawn = 5.0f;

    string[] spawnList = {
        "res://Scenes/Drone.tscn",
        "res://Scenes/Weta.tscn"
    };

    float nextSpawn;
    public override void _Ready()
    {
        nextSpawn = firstSpawnTime;
    }

    public override void _PhysicsProcess(float delta)
    {
        nextSpawn -= delta;
        if (nextSpawn <= 0)
        {
            nextSpawn += timeToSpawn;
            Node node = GD.Load<PackedScene>(spawnList[GD.Randi() % spawnList.Length]).Instance();
            ((Spatial)node).Translation = GlobalTransform.origin;
            GetTree().CurrentScene.AddChild(node);
        }
    }
}
