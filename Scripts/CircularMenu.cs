using Godot;
using System;

public class CircularMenu : Control
{
    public override void _Ready()
    {
        RecalculateMenuPosition();
    }

    public void RecalculateMenuPosition()
    {
        int totalChild = GetChildCount();
        float step = 360.0f / (float)totalChild;

        for (int i = 0; i < totalChild; i++)
        {
            float ang = step * (float)i;
            float x = Mathf.Sin(Mathf.Deg2Rad(ang));
            float y = Mathf.Cos(Mathf.Deg2Rad(ang));
            Node node = GetChild(i);
            if (node is BtnMenuCircle)
            {
                BtnMenuCircle btn = node as BtnMenuCircle;
                btn.RectPivotOffset = btn.RectSize / 2.0f;
                btn.RectPosition = new Vector2(x, -y) * 100.0f - btn.RectPivotOffset;
                btn.RectRotation = ang;
                btn.Connect("pressed", this, "BtnChangeGadget", new Godot.Collections.Array() { btn });
                TextureRect img = btn.GetNode<TextureRect>("TextureRect");
                if (img != null)
                {
                    img.RectRotation = -ang;
                }
            }
        }
    }

    public void BtnChangeGadget(BtnMenuCircle btn)
    {
        if (btn != null && Player.Instance != null)
        {
            Player.Instance.gadget = btn.gadget;
            Player.Instance.gadgetDataID = btn.gadgetDataID;
            HUD.Instance?.UpdateGadget(btn.gadget, btn.gadgetDataID);
        }
        Control parent = this.GetParent<Control>();
        if (parent != null)
        {
            parent.Visible = false;
        }
    }
}
