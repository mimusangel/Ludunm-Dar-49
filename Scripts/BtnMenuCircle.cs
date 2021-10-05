using Godot;
using System;

public class BtnMenuCircle : TextureRect
{

    [Export]
    public GadgetType gadget = GadgetType.Weapon;
    [Export]
    public int gadgetDataID = -1;
    [Export]
    public int cost = 0;

}
