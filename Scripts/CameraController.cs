using Godot;
using System;

public class CameraController : Spatial
{
    Spatial _target;

    bool _controlRotate = false;
    Vector2 mouseDelta = new Vector2();

    public override void _Ready()
    {
        _target = GetNode<Spatial>("../Player");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            var btn = @event as InputEventMouseButton;
            if (btn.ButtonIndex == (int)ButtonList.Middle)
            {
                _controlRotate = btn.Pressed;
                if (!_controlRotate)
                {
                    _target.RotationDegrees = this.RotationDegrees;
                }
            }
        }
        if (@event is InputEventMouseMotion)
        {
            var motion = @event as InputEventMouseMotion;
            mouseDelta = motion.Relative;
        }
    }

    public override void _Process(float delta)
    {
        this.Translation = _target.GlobalTransform.origin;
        if (!_controlRotate)
        {
            this.RotationDegrees = _target.RotationDegrees;
        }
        else
        {
            this.RotateY(-mouseDelta.x * delta);
        }
    }

}
