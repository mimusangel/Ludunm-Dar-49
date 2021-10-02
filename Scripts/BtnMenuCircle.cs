using Godot;
using System;

public class BtnMenuCircle : Button
{

    [Export]
    public GadgetType gadget = GadgetType.Weapon;
    [Export]
    public int gadgetDataID = -1;
    [Export]
    public Texture icon;

    public override void _Ready()
    {
        TextureRect img = GetNode<TextureRect>("TextureRect");
        if (img != null)
        {
            img.Texture = icon;
        }
    }
}
