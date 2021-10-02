using Godot;
using System;

public class HUD : Control
{
    public static HUD Instance { get; private set; } = null;

    Control _menu;
    public override void _Ready()
    {
        _menu = GetNode<Control>("MenuBG");
        _menu.Visible = false;
    }

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            InputEventMouseButton btn = @event as InputEventMouseButton;
            if (btn.ButtonIndex == (int)ButtonList.Right)
            {
                if (!btn.Pressed)
                {
                    _menu.Visible = !_menu.Visible;
                    _menu.RectPosition = btn.Position - _menu.RectSize / 2.0f;
                }
            }
        }
    }

    public  void UpdateGadget(GadgetType value, int dataID)
    {
        GD.Print($"Change Gadget to {value.ToString()} with data id {dataID}");
    }

    public bool MenuIsOpen()
    {
        return _menu.Visible;
    }
}
