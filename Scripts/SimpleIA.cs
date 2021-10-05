using Godot;
using System;

public class SimpleIA : KinematicBody
{
    public enum IABrain
    {
        LessFar,
        Objectif,
        Player,
        Build
    }

    [Export]
    public IABrain brain = IABrain.Objectif;

    [Export]
    public float targetDist = 10.0f;
    [Export]
    public float attackDist = 2.0f;

    [Export]
    public float speed = 5.0f;

    Spatial _target = null;
    Spatial _currentPath = null;
    bool _currentPathIsEnd = false;

    Vector3 _pathFeature = new Vector3();

    float pctFreeze;
    float freezeTime = 0.0f;

    [Export]
    public float durability = 10.0f;

    [Export]
    public bool deathOnAttack = false;
    [Export]
    public float attackDamage = 0.005f;
    [Export]
    public float attackDamageRand = 0.01f;
    [Export]
    public float attackCD = 0.25f;
    [Export]
    public string NameOfRender = "";

    Vector3 _lastPos = new Vector3();
    float _lastPosTimer = 1.0f;
    public override void _Ready()
    {
        var area = GetNode<Area>("Area");
        area.Scale = Vector3.One * targetDist;
        area.Connect("body_entered", this, "OnBodyEntered");
        area.Connect("body_exited", this, "OnBodyExited");
        speed = speed + GD.Randf() - 0.5f;

        var animation = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animation != null)
        {
            if (animation.HasAnimation("Idle"))
            {
                animation.Play("Idle");
            }
        }
        _atkCD = attackCD;
        _lastPos = GlobalTransform.origin;
    }

    public void OnBodyEntered(Node body)
    {
        Spatial nTarget = null;
        Node parent = null;
        switch (brain)
        {
            case IABrain.Objectif:
                parent = body.GetNode("../..");
                if (parent != null && parent is Objects)
                {
                    if (((Objects)parent).type == Objects.ObjectsType.Generator)
                    {
                        _target = (Spatial)parent;
                    }
                }
                return;
            case IABrain.LessFar:
                if (body is Player)
                {
                    nTarget = body as Spatial;
                }
                else
                {
                    parent = body.GetNode("../..");
                    if (parent != null && parent is Objects)
                    {
                        if (Player.Instance.IsFocusable(parent))
                        {
                            nTarget = parent as Spatial;
                        }
                    }
                }
                if (nTarget != null)
                {
                    if (_target != null)
                    {
                        float a = GlobalTransform.origin.DistanceTo(_target.GlobalTransform.origin);
                        float b = GlobalTransform.origin.DistanceTo(nTarget.GlobalTransform.origin);
                        if (b < a)
                        {
                            _target = nTarget;
                        }
                    }
                    else
                    {
                        _target = nTarget;
                    }
                }
                break;
            case IABrain.Player:
                if (body is Player)
                {
                    _target = body as Spatial;
                }
                else
                {
                    if (_target is Player == false)
                    {
                        _target = null;
                    }
                }
                break;
            case IABrain.Build:
                parent = body.GetNode("../..");
                if (parent != null && parent is Objects)
                {
                    if (Player.Instance.IsFocusable(parent))
                    {
                        nTarget = parent as Spatial;
                    }
                    if (nTarget != null)
                    {
                        if (_target != null)
                        {
                            GD.Print(_target);
                            float a = GlobalTransform.origin.DistanceTo(_target.GlobalTransform.origin);
                            float b = GlobalTransform.origin.DistanceTo(nTarget.GlobalTransform.origin);
                            if (b < a)
                            {
                                _target = nTarget;
                            }
                        }
                        else
                        {
                            _target = nTarget;
                        }
                    }
                }
                else
                {
                    if (_target is Objects == false)
                    {
                        _target = null;
                    }
                }
                break;
        }
    }

    public void OnBodyExited(Node body)
    {
        if (body == _target)
        {
            _target = null;
            _currentPath = null;
        }
    }

    public void Hit(float dmg)
    {
        durability -= dmg;

        if (durability <= 0.0f)
        {
            Death();
        }
    }

    public void Death()
    {
        GameData data = GetNode<GameData>("/root/GameData");
        data.SpwanSound(GlobalTransform.origin);

        Spatial scrap = GD.Load<PackedScene>("res://Scenes/Scrap.tscn").Instance<Spatial>();
        scrap.Translation = GlobalTransform.origin;
        GetTree().CurrentScene.AddChild(scrap);

        this.QueueFree();
    }

    float _atkCD = 0.0f;
    public virtual void Attack(float delta)
    {
        _atkCD = _atkCD - delta;
        if (_atkCD <= 0.0f)
        {
            _atkCD += attackCD;
            if (IsInstanceValid(_target) && _target.IsInsideTree())
            {
                if (_target is Objects)
                {
                    ((Objects)_target).Hit(attackDamage + GD.Randf() * attackDamageRand);
                }

                // Animation
                Node weapon = this.GetNodeOrNull<Node>("Weapon");
                if (weapon != null && weapon.GetChildCount() > 0)
                {
                    int childId = (int)(GD.Randi() % weapon.GetChildCount());
                    CPUParticles particle = weapon.GetChild<CPUParticles>(childId);
                    if (particle != null)
                    {
                        GameData data = GetNode<GameData>("/root/GameData");
                        data.SpwanSound(particle.GlobalTransform.origin, "res://Sounds/Effects/bullet.mp3");
                    
                        particle.Restart();
                    }
                }
            }
            if (deathOnAttack)
            {
                Death();
            }
        }
        else
        {
            if (deathOnAttack)
            {
                Spatial render = GetNodeOrNull<Spatial>(NameOfRender);
                if (render != null)
                {
                    render.Scale = Vector3.One * (2.0f - (_atkCD / attackCD));
                }
                else
                {
                    this.Scale = Vector3.One * (2.0f - (_atkCD / attackCD));
                }
            }
        }
    }

    public void Freeze()
    {
        freezeTime = 4.0f;
        pctFreeze = Mathf.Min(pctFreeze + 0.05f, 0.9f);
        FreezeRenderEffect();
    }

    public void FreezeRenderEffect()
    {
        for (int i = 0; i < this.GetChildCount(); i++)
        {
            MeshInstance mesh = this.GetChildOrNull<MeshInstance>(i);
            if (mesh != null)
            {
                ShaderMaterial mat = (ShaderMaterial)mesh.GetActiveMaterial(0);
                if (mat.Shader.HasParam("Emission"))
                {
                    mat.SetShaderParam("Emission", Colors.Black.LinearInterpolate(Colors.Cyan, pctFreeze));
                }

            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        freezeTime -= delta;
        if (freezeTime <= 0.0f && pctFreeze > 0.0f)
        {
            freezeTime = 0.0f;
            pctFreeze = 0.0f;
            FreezeRenderEffect();
        }
        Vector3 goTo = new Vector3(0.0f, 0.0f, 0.0f);
        if (_target != null)
        {
            if (IsInstanceValid(_target) == false || _target.IsInsideTree() == false)
            {
                _target = null;
            }
            else
            {
                goTo = _target.GlobalTransform.origin;
            }
        }
        if (_target == null)
        {
            if (_currentPath == null)
            {
                SearchPath();
            }
            else
            {
                float l = GlobalTransform.origin.DistanceTo(_currentPath.GlobalTransform.origin);
                if (l <= 1.5f && _currentPathIsEnd == false)
                {
                    _currentPath = _currentPath.GetParent<Spatial>();
                    _currentPathIsEnd = _currentPath.Name.Equals("Navigation", StringComparison.OrdinalIgnoreCase);
                }
                goTo = _currentPath.GlobalTransform.origin + _pathFeature;
            }
        }
        float len = GlobalTransform.origin.DistanceTo(goTo);
        if (len <= attackDist && (_target != null || _currentPathIsEnd))
        {
            AudioChange("AudioMove", false);
            AnimationState(false);
            Attack(delta);
        }
        else
        {
            AudioChange("AudioMove", true);
            AnimationState(true);
            Vector3 dir = (goTo - GlobalTransform.origin);
            dir.y = 0.0f;
            dir = dir.Normalized();
            _lastPosTimer -= delta;
            if (_lastPosTimer < 0.0f)
            {
                _lastPosTimer = 1.0f;
                if (_lastPos == GlobalTransform.origin)
                {
                    MoveAndSlide(dir.Cross(Vector3.Up) * speed * (1.0f - pctFreeze), Vector3.Up);
                }

                var space = GetWorld().DirectSpaceState;
                Vector3 p = GlobalTransform.origin + Vector3.Up;
                var collid = space.IntersectRay(p, p + dir * 2.0f, new Godot.Collections.Array() { this.GetRid() }, 4);
                if (collid.Count > 0)
                {
                    MoveAndSlide(dir.Cross(Vector3.Up) * speed * (1.0f - pctFreeze), Vector3.Up);
                }

                _lastPos = GlobalTransform.origin;
            }

            MoveAndSlide(dir * speed * (1.0f - pctFreeze), Vector3.Up);
            if (Transform.origin + dir != Transform.origin)
            {
                this.LookAt(Transform.origin + dir, Vector3.Up);
            }
        }
        MoveAndSlide(Vector3.Down * 5.0f, Vector3.Up);
    }

    public void AnimationState(bool walk)
    {
        AnimationPlayer animation = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animation != null)
        {
            if (walk)
            {
                if (animation.IsPlaying() && animation.CurrentAnimation.Equals("Walk"))
                {
                    return;
                }
                if (animation.HasAnimation("Walk"))
                {
                    animation.Play("Walk");
                }
            }
            else
            {
                if (animation.HasAnimation("Attack"))
                {
                    animation.Play("Attack");
                }
            }
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


    private float _searchLen;
    private Spatial _searchNode;
    private float _searchCalcLen;
    private Spatial _searchCalcNode;
    public void SearchPath()
    {
        _searchNode = GetTree().CurrentScene.GetNode<Spatial>("World/Navigation");
        _searchLen = GlobalTransform.origin.DistanceSquaredTo(_searchNode.GlobalTransform.origin);
        SearchPathInChild(_searchNode);
        _currentPath = _searchNode;
        _currentPathIsEnd = false;
        _pathFeature = new Vector3(
            GD.Randf() * 2.0f - 1.0f,
            0.0f,
            GD.Randf() * 2.0f - 1.0f
            );
    }

    private void SearchPathInChild(Spatial currentNode)
    {
        for (int i = 0; i < currentNode.GetChildCount(); i++)
        {
            _searchCalcNode = currentNode.GetChildOrNull<Spatial>(i);
            if (_searchCalcNode != null)
            {
                _searchCalcLen = GlobalTransform.origin.DistanceSquaredTo(_searchCalcNode.GlobalTransform.origin);
                if (_searchCalcLen < _searchLen)
                {
                    _searchLen = _searchCalcLen;
                    _searchNode = _searchCalcNode;
                }
                SearchPathInChild(_searchCalcNode);
            }
        }
    }
}
