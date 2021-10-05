using Godot;
using System;

public class WaveController : Node
{
    public enum WaveStat
    {
        None,
        WaveSpawn
    }
    [Export]
    public int NumberOfWave = 15;

    WaveStat stats = WaveStat.None;
    int wave = 0;
    int spawnCount = 0;
    float timer = 30.0f;


    public override void _Process(float delta)
    {
        timer -= delta;
        if (timer <= 0.0f)
        {
            if (stats == WaveStat.WaveSpawn)
            {
                if (spawnCount > 0)
                {
                    timer += 0.5f;
                    Spawn();
                    spawnCount--;
                }
                else
                {
                    stats = WaveStat.None;
                    timer += 90.0f - wave;
                }
            }
            else if (stats == WaveStat.None)
            {
                stats = WaveStat.WaveSpawn;
                timer += 0.25f;
                NextWave();
                HUD.Instance.UpdateWave($"Wave {wave}");
            }
        }
        else
        {
            if (stats == WaveStat.None)
            {
                HUD.Instance.UpdateWave($"Wave {wave + 1} in {timer.ToString("0")}");
            }
        }
    }

    public void NextWave()
    {
        wave++;
        Wave waveInfo = GetNodeOrNull<Wave>($"Wave{wave}");
        if (wave > NumberOfWave || waveInfo == null)
        {
            // Win !
            GetTree().ChangeScene("res://Scenes/MainMenu.tscn");
        }
        else
        {
            spawnCount = waveInfo.numberEnnemyPerSpawn;
        }
    }

    public void Spawn()
    {
        Wave waveInfo = GetNodeOrNull<Wave>($"Wave{wave}");
        if (waveInfo != null)
        {
            for (int i = 0; i < waveInfo.GetChildCount(); i++)
            {
                Spatial space = waveInfo.GetChildOrNull<Spatial>(i);
                if (space != null)
                {
                    int id = (int)(GD.Randi() % waveInfo.ennemies.Count);
                    Spatial ennemy = GD.Load<PackedScene>(waveInfo.ennemies[id]).Instance<Spatial>();
                    ennemy.Translation = space.GlobalTransform.origin;
                    GetTree().CurrentScene.AddChild(ennemy);
                }
            }
        }
    }

}
