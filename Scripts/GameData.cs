using Godot;
using System;
using System.Collections.Generic;

public class GameData : Node
{
    public class LinkData
    {
        public PhysicsBody A;
        public Objects AObject;
        public PhysicsBody B;

        public Objects BObject;

        public Node cable;
    }

    List<LinkData> listLink = new List<LinkData>();

    public override void _Ready()
    {

    }

    public int GetDataCable(PhysicsBody a, PhysicsBody b)
    {
        for (int i = 0; i < listLink.Count; i++)
        {
            var data = listLink[i];
            if (data.A == a && data.B == b)
            {
                return i;
            }
            if (data.A == b && data.B == a)
            {
                return i;
            }
        }
        return -1;
    }

    public List<LinkData> GetLink(PhysicsBody body)
    {
        List<LinkData> links = new List<LinkData>();

        foreach (LinkData link in listLink)
        {
            if (link.A == body || link.B == body)
            {
                links.Add(link);
            }
        }

        return links;
    }

    public List<LinkData> GetLink(Objects body)
    {
        List<LinkData> links = new List<LinkData>();
        foreach (LinkData link in listLink)
        {
            if (link.AObject == body || link.BObject == body)
            {
                links.Add(link);
            }
        }
        return links;
    }

    public LinkData GetDataCable(int index)
    {
        return listLink[index];
    }

    public void RemoveDateCable(LinkData data)
    {
        GetTree().CurrentScene.GetNode("World/Navigation/NavigationMeshInstance").RemoveChild(data.cable);
        data.cable.QueueFree();
        listLink.Remove(data);
    }

    public bool AddDataCable(PhysicsBody start, PhysicsBody end)
    {
        if (start.GetParent() is Objects || end.GetParent() is Objects)
        {
            Objects startObj = start.GetParent() as Objects;
            Objects endObj = end.GetParent() as Objects;
            Vector3 A = start.GlobalTransform.origin;
            Vector3 B = end.GlobalTransform.origin;
            Node cable = GD.Load<PackedScene>("res://Scenes/Cable.tscn").Instance();
            if (cable is Spatial)
            {
                Spatial spaceCable = cable as Spatial;
                spaceCable.Transform = spaceCable.Transform.LookingAt((B - A), Vector3.Up);
                float dist = A.DistanceTo(B);
                spaceCable.Scale = new Vector3(1.0f, 1.0f, dist);
                spaceCable.Translation = B;
            }
            GetTree().CurrentScene.GetNode("World/Navigation/NavigationMeshInstance").AddChild(cable);
            LinkData newData = new LinkData()
            {
                A = start,
                AObject = startObj,
                B = end,
                BObject = endObj,
                cable = cable
            };
            listLink.Add(newData);
            return true;
        }
        return false;
    }
}
