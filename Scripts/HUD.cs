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
        GetNode<Control>("ObjectMenu").Visible = false;
    }

    public override void _EnterTree()
    {
        Instance = this;
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

    public void FocusObject(Objects obj)
    {
        if (obj == null || IsInstanceValid(obj) == false || obj.IsInsideTree() == false)
        {
            GetNode<Control>("ObjectMenu").Visible = false;
            return;
        }
        obj.Connect("UpdateUI", this, "UpdateUI");
        GetNode<Control>("ObjectMenu").Visible = true;
    }
    public void UnfocusObject(Objects obj)
    {
        if (obj == null || IsInstanceValid(obj) == false || obj.IsInsideTree() == false)
        {
            GetNode<Control>("ObjectMenu").Visible = false;
            return;
        }
        obj.Disconnect("UpdateUI", this, "UpdateUI");
        GetNode<Control>("ObjectMenu").Visible = false;
    }

    public void UpdateUI(Objects obj)
    {
        var menu = GetNode<Control>("ObjectMenu");
        menu.GetNode<Label>("Box/Title").Text = obj.title;
        menu.GetNode<Label>("Box/Consume").Visible = false;
        menu.GetNode<Label>("Box/Other").Visible = false;
        float pct = 0.0f;
        switch (obj.type)
        {
            case Objects.ObjectsType.Generator:
                pct = obj.GetProductPct() * 100.0f;
                menu.GetNode<Label>("Box/Capacity").Text = $"Produce : {obj.GetProduct().ToString("0.0")}e";
                pct = (obj.GetInstability() - 1.0f) * 100.0f;
                menu.GetNode<Label>("Box/Other").Text = $"Instability : {pct.ToString("0.0")}%";
                menu.GetNode<Label>("Box/Other").Visible = true;
                break;
            case Objects.ObjectsType.Repeater:
                menu.GetNode<Label>("Box/Capacity").Text = "";
                break;
            case Objects.ObjectsType.Storage:
                pct = obj.GetPctCharge() * 100.0f;
                menu.GetNode<Label>("Box/Capacity").Text = $"Storage : {pct.ToString("0.0")}%";
                menu.GetNode<Label>("Box/Consume").Text = $"Consume IDLE : {obj.consumeIDLE.ToString("0.0")}e";
                menu.GetNode<Label>("Box/Consume").Visible = true;
                break;
            case Objects.ObjectsType.Tower:
                pct = obj.GetPctCharge() * 100.0f;
                menu.GetNode<Label>("Box/Capacity").Text = $"Storage : {pct.ToString("0.0")}%";
                menu.GetNode<Label>("Box/Consume").Text = $"Consume : {obj.consumeIDLE.ToString("0.0")}e / {obj.consume.ToString("0.0")}e";
                menu.GetNode<Label>("Box/Consume").Visible = true;
                break;
        }
    }
}
