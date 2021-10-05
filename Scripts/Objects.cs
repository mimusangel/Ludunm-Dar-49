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

    public enum DamageType
    {
        None,
        Ice,
        Fire,
        Phys
    }

    public enum StateObject
    {
        IDLE,
        Action
    }

    [Signal]
    public delegate void UpdateUI(Objects obj);

    [Export]
    public ObjectsType type = ObjectsType.Generator;

    [Export]
    public string title = "Unstable Generator";

    [Export]
    public float maxCapacity = 100.0f;

    [Export]
    public float tolerenceCapacityPct = 1.5f;

    [Export]
    public float consume = 0.0f;

    [Export]
    public float consumeIDLE = 0.0f;

    [Export]
    public DamageType damageType = DamageType.None;

    [Export]
    public float minDamage = 0.0f;

    [Export]
    public float maxDamage = 0.0f;

    [Export]
    public int scrapCost = 10;
    [Export]
    public float attackCD = 0.2f;


    private float _instability = 1.0f;
    private float _generatorSelector = 20.0f;
    private float _charge = 0.0f;
    private bool _powered = false;

    private float _eventTimer = 0.0f;

    private Spatial _canon;
    private SimpleIA _focus = null;
    private float fireCD = 0.1f;

    private StateObject state = StateObject.IDLE;

    public override void _Ready()
    {
        _canon = GetNodeOrNull<Spatial>("Canon");
        Area area = GetNodeOrNull<Area>("Area");
        if (area != null)
        {
            area.Connect("body_entered", this, "OnBodyEntered");
            area.Connect("body_exited", this, "OnBodyExited");
        }
        _generatorSelector = (float)GD.RandRange(GetProductMin(), GetProductMax());
        _eventTimer = 1.0f;
        EmitSignal(nameof(UpdateUI), this);
    }

    public void MouseEnter()
    {
        HUD.Instance?.FocusObject(this);
        EmitSignal(nameof(UpdateUI), this);
    }

    public void MouseExit()
    {
        HUD.Instance?.UnfocusObject(this);
    }

    private Queue<SimpleIA> _focusQueue = new Queue<SimpleIA>();

    public void OnBodyEntered(Node body)
    {
        
        if (body is SimpleIA)
        {
            if (_focus != null)
            {
                _focusQueue.Enqueue(body as SimpleIA);
            }
            else
            {
                _focus = body as SimpleIA;
            }
        }
    }

    public void OnBodyExited(Node body)
    {
        if (body == _focus)
        {
            _focus = null;
        }
    }

    public void Hit(float dmg)
    {
        _instability += dmg;
    }

    public override void _Process(float delta)
    {
        _eventTimer -= delta;
        _instability = Mathf.Max(_instability - 0.01f * delta, 1.0f);
        if (_focus == null)
        {
            if (_focusQueue.Count > 0)
            {
                _focus = _focusQueue.Dequeue();
            }
        }
        if (_focus == null || IsInstanceValid(_focus) == false || _focus.IsInsideTree() == false)
        {
            _focus = null;
        }
        GameData data = GetNode<GameData>("/root/GameData");
        switch (type)
        {
            case ObjectsType.Generator:
                if (_eventTimer <= 0.0f)
                {
                    _generatorSelector = Mathf.Clamp(_generatorSelector + (GD.Randf() * 2.0f - 1.0f) * GetProductVar(), GetProductMin(), GetProductMax());
                    EmitSignal(nameof(UpdateUI), this);
                }
                var links = data.GetLink(this);
                foreach (GameData.LinkData link in links)
                {
                    link.AObject.SendEnergy(_generatorSelector * delta, new List<Objects>() { this });
                    link.BObject.SendEnergy(_generatorSelector * delta, new List<Objects>() { this });
                }
                TestInstability();
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
                            fireCD += attackCD;
                            if (_charge >= consume)
                            {
                                _charge -= consume * attackCD;
                                // Fire Here
                                _focus.Hit((float)GD.RandRange(minDamage, maxDamage));
                                switch (damageType)
                                {
                                    case DamageType.Ice:
                                        _focus.Freeze();
                                        AudioChange("Canon/AttackAudio", true);
                                        EnableParticle(true);
                                        break;
                                    case DamageType.Fire:
                                        AudioChange("Canon/AttackAudio", true);
                                        EnableParticle(true);
                                        break;
                                    case DamageType.Phys:
                                        data.SpwanSound(GlobalTransform.origin, "res://Sounds/Effects/lazer basic.mp3");
                                        EnableParticle(true, true);
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (damageType)
                        {
                            case DamageType.Ice:
                            case DamageType.Fire:
                                EnableParticle(false);
                                AudioChange("Canon/AttackAudio", false);
                                break;
                        }
                    }
                    TestCharge();
                }
                else
                {
                    switch (damageType)
                    {
                        case DamageType.Ice:
                        case DamageType.Fire:
                            EnableParticle(false);
                            AudioChange("Canon/AttackAudio", false);
                            break;
                    }
                }
                TestInstability();
                break;
            case ObjectsType.Storage:
                ConsumeIDLE(delta);
                if (_powered)
                {
                    var linkedTower = GetLinkedTower(this, new List<Objects>());
                    float consu = 0.0f;
                    foreach (Objects obj in linkedTower)
                    {
                        if (obj._charge < obj.maxCapacity)
                        {
                            float a = obj.consume * delta * 2.0f;
                            if (consu + a > _charge)
                            {
                                a = _charge - consu;
                            }
                            obj._charge += a;
                            consu += a;
                        }
                        if (_charge - consu == 0.0f)
                        {
                            break;
                        }
                    }
                    _charge -= consu;
                    TestCharge();
                }
                TestInstability();
                if (state == StateObject.Action)
                {
                    _charge -= (maxCapacity / 5.0f) * delta;
                    if (_charge <= 0.0f)
                    {
                        _charge = 0.0f;
                        state = StateObject.IDLE;
                    }
                }
                break;
        }
        MeshInstance meshCharge = GetNodeOrNull<MeshInstance>("Interface/Gauge");
        if (meshCharge != null)
        {
            GetNodeOrNull<Spatial>("Interface").Visible = true;
            ShaderMaterial mat = meshCharge.GetActiveMaterial(0) as ShaderMaterial;
            mat.SetShaderParam("Charge", GetPctCharge());
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

    public void EnableParticle(bool enable, bool random = false)
    {
        Spatial weapon = _canon?.GetNodeOrNull<Spatial>("Weapon");
        if (random)
        {
            int i = (int)(GD.Randi() % weapon.GetChildCount());
            CPUParticles particle = weapon.GetChildOrNull<CPUParticles>(i);
            if (particle != null)
            {
                if (particle.Emitting != enable)
                {
                    particle.Emitting = enable;
                }
            }
            return;
        }
        for (int i = 0; i < weapon.GetChildCount(); i++)
        {
            CPUParticles particle = weapon.GetChildOrNull<CPUParticles>(i);
            if (particle != null)
            {
                if (particle.Emitting != enable)
                {
                    particle.Emitting = enable;
                }
            }
        }
    }

    public void Use()
    {
        switch (type)
        {
            case ObjectsType.Storage:
                state = StateObject.Action;
                break;
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
            Death();
        }
        else
        {
            AudioChange("Alarm", (_charge > maxCapacity));
        }
    }

    public void AudioChange(string audioSrc, bool play)
    {
        AudioStreamPlayer3D audio = GetNodeOrNull<AudioStreamPlayer3D>(audioSrc);
        if (audio != null)
        {
            if (play && audio.Playing == false)
            {
                audio.Play();
            }
            if (play == false && audio.Playing)
            {
                audio.Stop();
            }
        }
    }

    public void TestInstability()
    {
        if (_instability >= 2.0)
        {
            // dead
            Death();
        }
        else
        {
            // Critique
            AudioChange("Alarm", (_instability >= 1.6f));
        }
    }

    public void Death()
    {
        GameData data = GetNode<GameData>("/root/GameData");
        if (type == ObjectsType.Generator)
        {
            data.GameOver();
            return;
        }
        data.LinkFree(this);
        data.SpwanSound(GlobalTransform.origin);
        
        Spatial scrap = GD.Load<PackedScene>("res://Scenes/Scrap.tscn").Instance<Spatial>();
        scrap.Translation = GlobalTransform.origin;
        GetTree().CurrentScene.AddChild(scrap);

        GetParent().RemoveChild(this);
        this.QueueFree();
    }

    public float GetPctCharge()
    {
        return _charge / maxCapacity;
    }

    public float GetProduct()
    {
        return _generatorSelector;
    }

    public float GetProductPct()
    {
        return _generatorSelector / GetProductMax();
    }

    public float GetInstability()
    {
        return _instability;
    }

    public float GetProductVar()
    {
        return 5.0f * _instability;
    }

    public float GetProductMin()
    {
        return 10.0f * _instability;
    }

    public float GetProductMax()
    {
        return 75.0f * _instability;
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
