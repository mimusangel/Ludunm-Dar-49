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

    Spatial _target = null;

    bool _init = false;

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
        }
    }

    public virtual void Attack()
    {

    }

    public override void _PhysicsProcess(float delta)
    {
        if (_init == false)
        {
            var space = GetWorld().DirectSpaceState;
            var collid = space.IntersectRay(GlobalTransform.origin, GlobalTransform.origin + Vector3.Down * 20.0f);
            if (collid.Count > 0)
            {
                this.Translation = (Vector3)collid["position"];
            }
            _init = true;
        }

        Vector3 goTo = new Vector3(0.0f, 0.0f, 0.0f);
        if (_target != null)
        {
            try
            {
                goTo = _target.GlobalTransform.origin;
            }
            catch (ObjectDisposedException e)
            {
                goTo = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }
        float len = GlobalTransform.origin.DistanceTo(goTo);
        if (len <= attackDist)
        {
            // Attack
            Attack();
        }
        else
        {
            Vector3 dir = (goTo - GlobalTransform.origin);
            dir.y = 0.0f;
            dir = dir.Normalized();
            MoveAndSlide(dir * 5.0f, Vector3.Up);
            this.LookAt(Transform.origin + dir, Vector3.Up);
        }
    }
}
