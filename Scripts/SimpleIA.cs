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
    Vector3 _lastPos = new Vector3();
    Spatial _currentPath = null;
    bool _currentPathIsEnd = false;

    float pctFreeze;
    float freezeTime = 0.0f;

    [Export]
    public float durability = 10.0f;

    public override void _Ready()
    {
        var area = GetNode<Area>("Area");
        area.Scale = Vector3.One * targetDist;
        area.Connect("body_entered", this, "OnBodyEntered");
        area.Connect("body_exited", this, "OnBodyExited");
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
            GameData data = GetNode<GameData>("/root/GameData");
            data.SpwanSound(GlobalTransform.origin);
            this.QueueFree();
        }
    }

    float _atkCD = 0.0f;
    public virtual void Attack(float delta)
    {
        _atkCD = _atkCD - delta;
        if (_atkCD <= 0.0f)
        {
            _atkCD += 0.25f;
            if (IsInstanceValid(_target) && _target.IsInsideTree())
            {
                if (_target is Objects)
                {
                    ((Objects)_target).Hit(0.01f + GD.Randf() * 0.02f);
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
                if (l < 1.0f && _currentPathIsEnd == false)
                {
                    _lastPos = _currentPath.GlobalTransform.origin;
                    _currentPath = _currentPath.GetParent<Spatial>();
                    _currentPathIsEnd = _currentPath.Name.Equals("Navigation", StringComparison.OrdinalIgnoreCase);
                }
                goTo = _currentPath.GlobalTransform.origin;
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
            MoveAndSlide(dir * speed * (1.0f - pctFreeze), Vector3.Up);
            this.LookAt(Transform.origin + dir, Vector3.Up);
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
                animation.Play("Walk");
            }
            else
            {
                animation.Play("Attack");
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
        _lastPos = GlobalTransform.origin;
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
