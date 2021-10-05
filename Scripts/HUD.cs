using Godot;
using System;

public class HUD : Control
{
    public static HUD Instance { get; private set; } = null;

    public BtnMenuCircle selected = null;

    public override void _Ready()
    {
        GetNode<Control>("ObjectMenu").Visible = false;
        GetNode<Control>("MenuBG").Visible = false;
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
                Control menu = GetNode<Control>("MenuBG");
                menu.Visible = btn.Pressed;
                if (btn.Pressed)
                {
                    menu.RectPosition = btn.Position - menu.RectSize / 2.0f;
                    SetupMenu(btn.Position);
                }
                else
                {
                    SetupMenu(btn.Position);
                    if (selected != null)
                    {
                        Player.Instance.gadget = selected.gadget;
                        Player.Instance.gadgetDataID = selected.gadgetDataID;
                        HUD.Instance?.UpdateGadget(selected.gadget, selected.gadgetDataID);
                    }
                }
            }
        }
        if (@event is InputEventMouseMotion)
        {
            InputEventMouseMotion mouse = @event as InputEventMouseMotion;
            Control menu = GetNode<Control>("MenuBG");
            if (menu.Visible)
            {
                SetupMenu(mouse.Position);
            }
        }
    }

    public void SetupMenu(Vector2 mousePos)
    {
        var _menu = GetNode<Control>("MenuBG/Menu");
        int totalChild = _menu.GetChildCount();
        float step = 360.0f / (float)totalChild;
        float halfStep = step / 2.0f;

        Vector2 mouseDir = (mousePos - _menu.RectGlobalPosition).Normalized();

        var selector = GetNode<Control>("MenuBG/Selector");

        var label = GetNode<Label>("MenuBG/LabelCost");
        label.Text = "";

        var labelCost = GetNode<Label>("MenuBG/Label");
        labelCost.Text = "";
        labelCost.Visible = false;

        float actualDot = 0.0f;
        selected = null;
        for (int i = 0; i < totalChild; i++)
        {
            float ang = step * (float)i;
            float x = Mathf.Sin(Mathf.Deg2Rad(ang));
            float y = Mathf.Cos(Mathf.Deg2Rad(ang));
            BtnMenuCircle node = _menu.GetChildOrNull<BtnMenuCircle>(i);
            if (node != null)
            {
                node.RectPosition = new Vector2(x, -y) * 100.0f - node.RectSize / 2.0f;
                Vector2 dir = node.RectPosition.Normalized();

                float dot = mouseDir.Dot(dir);
                if (dot > actualDot)
                {
                    selected = node;
                    actualDot = dot;
                    selector.RectGlobalPosition = _menu.RectGlobalPosition + new Vector2(x, -y) * 100.0f - selector.RectSize / 2.0f;
                    label.Text = $"{selected.Name}";
                    labelCost.Text = $"{selected.cost} Scrap";
                    labelCost.Visible = selected.gadget == GadgetType.Tower || selected.gadget == GadgetType.Repeater;
                }
            }
        }
        if (selected != null)
        {
            GetNode<Label>("Gadget/Label").Text = $"{selected.Name}";
            GetNode<Label>("Gadget/LabelCost").Text = $"{selected.cost} Scrap";
            GetNode<Label>("Gadget/LabelCost").Visible = selected.gadget == GadgetType.Tower || selected.gadget == GadgetType.Repeater;
            GetNode<TextureRect>("Gadget/TextureRect").Texture = selected.Texture;
        }
    }

    public  void UpdateGadget(GadgetType value, int dataID)
    {
        GD.Print($"Change Gadget to {value.ToString()} with data id {dataID}");
    }

    public bool MenuIsOpen()
    {
        return GetNode<Control>("MenuBG").Visible;
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

    public void UpdateScrap(int amount)
    {
        Label label = GetNodeOrNull<Label>("Scrap/Label");
        if (label != null)
        {
            label.Text = amount.ToString("00000");
        }
    }

    public void UpdateWave(string txt)
    {
        Label label = GetNodeOrNull<Label>("LabelWave");
        if (label != null)
        {
            label.Text = txt;
        }
    }
}
