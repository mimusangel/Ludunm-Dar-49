using Godot;
using System;
using System.Collections.Generic;

public class Objects : Spatial
{
    public enum ObjectsType
    {
        Generator,
        Repeater,
        Storage,
        Tower
    }

    [Signal]
    public delegate void UpdateUI(Objects obj);

    [Export]
    public ObjectsType type = ObjectsType.Generator;

    [Export]
    public float maxCapacity = 100.0f;

    [Export]
    public float tolerenceCapacityPct = 1.5f;

    [Export]
    public float consume = 0.0f;

    [Export]
    public float consumeIDLE = 0.0f;

    private float _instability = 1.0f;
    private float _generatorSelector = 20.0f;
    private float _charge = 0.0f;
    private bool _powered = false;

    private float _eventTimer = 0.0f;

    private Spatial _canon;
    private SimpleIA _focus = null;
    private float fireCD = 0.1f;

    public override void _Ready()
    {
        _canon = GetNodeOrNull<Spatial>("Canon");
        Area area = GetNodeOrNull<Area>("Area");
        if (area != null)
        {
            area.Connect("body_entered", this, "OnBodyEntered");
            area.Connect("body_exited", this, "OnBodyExited");
        }
        _generatorSelector = GD.Randf();
        _eventTimer = 1.0f;
        EmitSignal(nameof(UpdateUI), this);
    }

    public void OnBodyEntered(Node body)
    {
        if (_focus != null)
        {
            return;
        }
        if (body is SimpleIA)
        {
            _focus = body as SimpleIA;
        }
    }

    public void OnBodyExited(Node body)
    {
        if (body == _focus)
        {
            _focus = null;
        }
    }

    public override void _Process(float delta)
    {
        _eventTimer -= delta;
        _instability = Mathf.Max(_instability - 0.01f * delta, 1.0f);
        switch (type)
        {
            case ObjectsType.Generator:
                if (_eventTimer <= 0.0f)
                {
                    _generatorSelector = Mathf.Clamp(_generatorSelector + (GD.Randf() * 2.0f - 1.0f) * 20.0f * _instability, 50.0f * _instability, 150.0f * _instability);
                    EmitSignal(nameof(UpdateUI), this);
                }
                GameData data = GetNode<GameData>("/root/GameData");
                var links = data.GetLink(this);
                foreach (GameData.LinkData link in links)
                {
                    link.AObject.SendEnergy(_generatorSelector * delta, new List<Objects>() { this });
                    link.BObject.SendEnergy(_generatorSelector * delta, new List<Objects>() { this });
                }
                break;
            case ObjectsType.Tower:
                ConsumeIDLE(delta);
                if (_powered)
                {
                    if (_focus != null)
                    {
                        fireCD -= delta;
                        if (fireCD <= 0.0f)
                        {
                            fireCD += 0.1f;
                            if (_charge >= consume)
                            {
                                _charge -= consume;
                                // Fire Here
                            }
                        }
                    }

                    TestCharge();
                }
                break;
            case ObjectsType.Storage:
                ConsumeIDLE(delta);
                if (_powered)
                {
                    var linkedTower = GetLinkedTower(this, new List<Objects>());
                    float consu = 0.0f;
                    foreach (Objects obj in linkedTower)
                    {
                        float a = obj.consume * delta;
                        if (consu + a > _charge)
                        {
                            a = _charge - consu;
                        }
                        obj._charge += a;
                        consu += a;
                        if (_charge - consu == 0.0f)
                        {
                            break;
                        }
                    }
                    _charge -= consu;
                    TestCharge();
                }
                break;
        }
        if (_eventTimer <= 0.0f)
        {
            if (_powered)
            {
                EmitSignal(nameof(UpdateUI), this);
            }
            _eventTimer += 1.0f;
        }

        if (_powered)
        {
            if (_focus != null)
            {
                if (_canon != null)
                {
                    Vector3 dir = _focus.GlobalTransform.origin - _canon.GlobalTransform.origin;
                    dir = dir.Normalized();
                    _canon.LookAt(_canon.GlobalTransform.origin - dir, Vector3.Up);
                }
            }
        }
    }

    public void ConsumeIDLE(float delta)
    {
        float sub = consumeIDLE * delta;
        if (_charge >= sub)
        {
            _charge -= sub;
            _powered = true;
        }
        else
        {
            _charge = 0.0f;
            _powered = false;
        }
    }

    public void TestCharge()
    {
        if (_charge > maxCapacity * tolerenceCapacityPct)
        {
            // Etat dead
        }
        else if (_charge > maxCapacity)
        {
            // Etat Critique
        }
        else
        {
            // Etat Ok !
        }
    }

    public float GetPctCharge()
    {
        return _charge / maxCapacity;
    }

    public List<Objects> GetLinkedTower(Objects actual, List<Objects> blackList)
    {
        List<Objects> towers = new List<Objects>();
        if (blackList.Contains(actual))
        {
            return towers;
        }
        blackList.Add(actual);
        if (actual.type == ObjectsType.Tower)
        {
            towers.Add(actual);
        }
        GameData data = GetNode<GameData>("/root/GameData");
        var links = data.GetLink(actual);
        foreach (GameData.LinkData link in links)
        {
            towers.AddRange(link.AObject.GetLinkedTower(link.AObject, blackList));
            towers.AddRange(link.BObject.GetLinkedTower(link.BObject, blackList));
        }
        return towers;
    }

    public void SendEnergy(float energy, List<Objects> listLastObjs)
    {
        if (listLastObjs.Contains(this) == false)
        {
            GameData data = GetNode<GameData>("/root/GameData");
            var links = data.GetLink(this);
            switch (type)
            {
                case ObjectsType.Repeater:
                    float sending = energy / (float)links.Count;
                    foreach (GameData.LinkData link in links)
                    {
                        var nList = new List<Objects>(listLastObjs);
                        nList.Add(this);
                        link.AObject.SendEnergy(sending, nList);
                        link.BObject.SendEnergy(sending, nList);
                    }
                    break;
                case ObjectsType.Storage:
                case ObjectsType.Tower:
                    _charge += energy;
                    break;
            }

        }
    }

}
