using Godot;
using System;

public class TowerTestUI : Panel
{
    Label lbl;
    public override void _Ready()
    {
        lbl = GetNode<Label>("Label");
        Objects obj = GetNode<Objects>("../..");
        obj.Connect("UpdateUI", this, "UpdateUI");
        var col = obj.GetNode<CollisionObject>("Object/Body");
        col.Connect("mouse_entered", this, "MouseEnter");
        col.Connect("mouse_exited", this, "MouseExit");
    }

    public void MouseEnter()
    {
        GetNode<Spatial>("../MeshInstance").Scale = new Vector3(2.0f, 2.0f, 2.0f);
    }

    public void MouseExit()
    {
        GetNode<Spatial>("../MeshInstance").Scale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    public void UpdateUI(Objects obj)
    {
        lbl.Text = $"Charges : {obj.GetPctCharge().ToString("0.00")}%";
    }
}
