using Godot;
using System;

public class Wave : Node
{
    [Export]
    public Godot.Collections.Array<string> ennemies = new Godot.Collections.Array<string>();

    [Export]
    public int numberEnnemyPerSpawn = 5;

    public override void _Ready()
    {
        
    }
}
