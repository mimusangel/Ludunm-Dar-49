using Godot;
using System;

public class Player : KinematicBody
{
    public class PlayerRay
    {
        public Vector3 position;
        public Vector3 direction;
        public Node VisualObject = null;
        public bool Placeable = false;
    }

    public static Player Instance { get; private set; }

    private bool _forward;
    private bool _backward;
    private bool _right;
    private bool _left;
    private bool _jump;

    private Vector3 _velocity = new Vector3();
    private float _speed = 0.0f;
    private Vector3 _gravityVel = new Vector3();
    private Vector3 _gravity = new Vector3(0.0f, -9.81f, 0.0f);

    private Camera _camera;

    private GadgetType _gadget = GadgetType.Hand;
    public GadgetType gadget
    {
        get
        {
            return _gadget;
        }
        set
        {
            _gadget = value;
        }
    }
    private int _gadgetDataID = -1;
    public int gadgetDataID
    {
        get
        {
            return _gadgetDataID;
        }
        set
        {
            _gadgetDataID = value;
        }
    }

    public int scrap = 0;

    public override void _EnterTree()
    {
        Instance = this;
    }

    Spatial _tmpCable;
    public override void _Ready()
    {
        _camera = GetNode<Camera>("Camera");
    }

    private void ControlPlayer(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            InputEventKey key = @event as InputEventKey;

            switch (key.Scancode)
            {
                case (int)KeyList.W:
                    _forward = key.Pressed;
                    break;
                case (int)KeyList.S:
                    _backward = key.Pressed;
                    break;
                case (int)KeyList.A:
                    _left = key.Pressed;
                    break;
                case (int)KeyList.D:
                    _right = key.Pressed;
                    break;
                case (int)KeyList.Space:
                    _jump = key.Pressed;
                    break;
            }
        }
    }

    PlayerRay ray = null;
    float timeTest = 1.0f;
    private void ControlActionPlaceTower(InputEventMouseButton button)
    {
        if (button.ButtonIndex == (int)ButtonList.Left)
        {
            if (button.Pressed)
            {
                timeTest = 0.0f;
                if (_camera != null)
                {
                    string resPath = "res://Scenes/Objects/Capacitor.tscn";
                    if (_gadget == GadgetType.Repeater)
                    {
                        resPath = "res://Scenes/Objects/Repeater.tscn";
                    }
                    else
                    {
                        switch (_gadgetDataID)
                        {
                            case 0:
                                resPath = "res://Scenes/Objects/Capacitor.tscn";
                                break;
                            case 1:
                                resPath = "res://Scenes/Objects/TowerIce.tscn";
                                break;
                            case 2:
                                resPath = "res://Scenes/Objects/TowerFire.tscn";
                                break;
                        }
                    }
                    ray = new PlayerRay()
                    {
                        position = _camera.ProjectRayOrigin(button.Position),
                        direction = _camera.ProjectRayNormal(button.Position),
                        VisualObject = GD.Load<PackedScene>(resPath).Instance()
                    };
                    if (ray.VisualObject != null)
                    {
                        GetTree().CurrentScene.GetNode("World").AddChild(ray.VisualObject);
                        PlaceMode(true);
                    }
                    PlaceTowerOver(0.0f);
                }
            }
            else
            {
                if (ray != null && ray.VisualObject != null)
                {
                    if (!ray.Placeable)
                    {
                        GetTree().CurrentScene.GetNode("World").RemoveChild(ray.VisualObject);
                        ray.VisualObject.QueueFree();
                    }
                    else
                    {
                        PlaceMode(false);
                    }
                }
                ray = null;
            }
        }
    }

    PhysicsBody startLink = null;
    private void ControlActionLink(InputEventMouseButton button)
    {
        if (button.ButtonIndex == (int)ButtonList.Left)
        {
            var origin = _camera.ProjectRayOrigin(button.Position);
            var direction = _camera.ProjectRayNormal(button.Position);
            var space = GetWorld().DirectSpaceState;
            var collid = space.IntersectRay(origin, origin + direction * 20.0f,  new Godot.Collections.Array() { this.GetRid() }, 4);

            if (button.Pressed)
            {
                if (collid.Count > 0)
                {
                    if (_tmpCable == null)
                    {
                        var tmp = GD.Load<PackedScene>("res://Scenes/Cable.tscn").Instance();
                        GetTree().CurrentScene.AddChild(tmp);
                        _tmpCable = tmp as Spatial;
                    }
                    startLink = (PhysicsBody)collid["collider"];
                    _tmpCable.Visible = true;
                }
            }
            else
            {
                if (collid.Count > 0)
                {
                    PhysicsBody endLink = (PhysicsBody)collid["collider"];
                    if (startLink != null && startLink != endLink)
                    {
                        GameData data = GetNode<GameData>("/root/GameData");
                        int dataId = data.GetDataCable(startLink, endLink);
                        if (dataId >= 0)
                        {
                            var dataCable = data.GetDataCable(dataId);
                            data.RemoveDateCable(dataCable);
                        }
                        else
                        {
                            data.AddDataCable(startLink, endLink);
                        }
                    }
                }
                if (_tmpCable != null)
                {
                    _tmpCable.Visible = false;
                }
                startLink = null;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        ControlPlayer(@event);
        if (HUD.Instance.MenuIsOpen() == false)
        {
            if (@event is InputEventMouseMotion)
            {
                InputEventMouseMotion motion = @event as InputEventMouseMotion;

                if (ray != null)
                {
                    ray.position = _camera.ProjectRayOrigin(motion.Position);
                    ray.direction = _camera.ProjectRayNormal(motion.Position);
                }
            }
            if (@event is InputEventMouseButton)
            {
                InputEventMouseButton button = @event as InputEventMouseButton;
                
                switch (_gadget)
                {
                    case GadgetType.Weapon:
                        if (_lastObjFocus != null)
                        {
                            GameData data = GetNode<GameData>("/root/GameData");
                            data.LinkFree(_lastObjFocus);
                            GetTree().CurrentScene.GetNode("World").RemoveChild(_lastObjFocus);
                            _lastObjFocus.QueueFree();
                            _lastObjFocus.MouseExit();
                            _lastObjFocus = null;
                        }
                        break;
                    case GadgetType.Link:
                        ControlActionLink(button);
                        break;
                    case GadgetType.Repeater:
                    case GadgetType.Tower:
                        ControlActionPlaceTower(button);
                        break;
                    case GadgetType.Hand:
                        if (button.ButtonIndex == (int)ButtonList.Left)
                        {
                            if (_lastObjFocus != null)
                            {
                                if (button.Pressed == false)
                                {
                                    _lastObjFocus.Use();
                                }
                            }
                        }
                        break;
                }
            }
        }
    }

    private void Movement(float delta)
    {
        bool onFloor = this.IsOnFloor();

        if (onFloor)
        {
            float forward = (_backward ? 1.0f : 0.0f) - (_forward ? 1.0f : 0.0f);
            _speed = Mathf.Max(_speed, 10.0f);
            _speed += (_speed * 1.5f * Mathf.Abs(forward)) * delta;
            _speed = Mathf.Min(_speed, 20.0f);
            _velocity = this.Transform.basis.z * forward;
        }
        else
        {
            _speed -= (_speed / 1.25f) * delta;
            _speed = Mathf.Max(_speed, 0.0f);
        }

        this.MoveAndSlide(_velocity * _speed, Vector3.Up);

        float rot = (_left ? 1.0f : 0.0f) - (_right ? 1.0f : 0.0f);
        this.RotateY(5.0f * rot * delta);

        _gravityVel += _gravity * delta;
        if (onFloor)
        {
            _gravityVel = new Vector3();
            if (_jump)
            {
                _gravityVel = -_gravity;
            }
        }
        _gravityVel = this.MoveAndSlide(_gravityVel, Vector3.Up);
    }

    private void PlaceTowerOver(float delta)
    {
        if (ray == null)
        {
            return;
        }
        if (ray.VisualObject != null)
        {
            if (ray.VisualObject is Spatial)
            {
                var obj = ray.VisualObject as Spatial;
                var body = obj.GetNode<PhysicsBody>("Object/Body");
                var space = GetWorld().DirectSpaceState;
                var collid = space.IntersectRay(ray.position, ray.position + ray.direction * 15.0f,
                    new Godot.Collections.Array() { this.GetRid() },
                    3);
                obj.Visible = true;
                if (collid.Count > 0)
                {
                    obj.Translation = (Vector3)collid["position"];
                    if (collid["collider"] is PhysicsBody)
                    {
                        PhysicsBody collidBody = (PhysicsBody)collid["collider"];
                        if ((collidBody.CollisionLayer & 2) == 2)
                        {
                            timeTest -= delta;
                            if (timeTest <= 0.0f)
                            {
                                timeTest += 0.2f;

                                var shape = body.GetNode<CollisionShape>("CollisionShape");
                                PhysicsShapeQueryParameters query = new PhysicsShapeQueryParameters();
                                query.SetShape(shape.Shape);
                                query.Exclude = new Godot.Collections.Array();
                                query.Transform = obj.Transform;
                                query.CollisionMask = 1;
                                var result = space.IntersectShape(query);
                                ray.Placeable = result.Count <= 0;
                            }
                        }
                        else
                        {
                            ray.Placeable = false;
                        }
                    }
                    else
                    {
                        ray.Placeable = false; 
                    }
                    PlaceMode(true);
                }
                else
                {
                    ray.Placeable = false;
                    obj.Visible = false;
                }
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        Movement(delta);
        RayForInterface();
        switch (_gadget)
        {
            case GadgetType.Link:
                UpdateCable();
                break;
            case GadgetType.Repeater:
            case GadgetType.Tower:
                PlaceTowerOver(delta);
                break;
        }
    }

    public void UpdateCable()
    {
        if (startLink == null || _tmpCable == null)
        {
            return;
        }
        var origin = _camera.ProjectRayOrigin(GetViewport().GetMousePosition());
        var direction = _camera.ProjectRayNormal(GetViewport().GetMousePosition());
        var space = GetWorld().DirectSpaceState;
        var collid = space.IntersectRay(origin, origin + direction * 20.0f, new Godot.Collections.Array() { this.GetRid() }, 4);
        Vector3 A = startLink.GlobalTransform.origin;
        Vector3 B = GlobalTransform.origin + Vector3.Up;
        if (collid.Count > 0)
        {
            PhysicsBody body = (PhysicsBody)collid["collider"];
            if (startLink != body)
            {
                Objects startObj = startLink.GetParent() as Objects;
                Objects endObj = body.GetParent() as Objects;
                if (startObj != endObj)
                {
                    B = body.GlobalTransform.origin;
                }
            }
        }
        _tmpCable.Translation = B;
        Vector3 dir = (B - A).Normalized();
        _tmpCable.LookAt(_tmpCable.GlobalTransform.origin + dir, Vector3.Up);
        float dist = A.DistanceTo(B);
        _tmpCable.Scale = new Vector3(1.0f, 1.0f, dist);
        if (_tmpCable is MeshInstance)
        {
            MeshInstance mesh = _tmpCable as MeshInstance;
            ShaderMaterial mat = (ShaderMaterial)mesh.GetActiveMaterial(0);
            mat.SetShaderParam("CableColor", new Color(Colors.DarkGray, 0.8f));
            mat.SetShaderParam("GravityForce", -0.05f * dist);
        }
    }

    private Objects _lastObjFocus = null;
    public void RayForInterface()
    {
        var origin = _camera.ProjectRayOrigin(GetViewport().GetMousePosition());
        var direction = _camera.ProjectRayNormal(GetViewport().GetMousePosition());
        var space = GetWorld().DirectSpaceState;
        var collid = space.IntersectRay(origin, origin + direction * 20.0f, new Godot.Collections.Array() { this.GetRid() });
        if (collid.Count > 0)
        {
            if (_lastObjFocus != null)
            {
                _lastObjFocus.MouseExit();
                _lastObjFocus = null;
            }
            PhysicsBody body = (PhysicsBody)collid["collider"];
            Objects obj = body.GetNodeOrNull<Objects>("../..");
            if (obj != null && obj != _lastObjFocus)
            {
                obj.MouseEnter();
                _lastObjFocus = obj;
            }
        }
        else
        {
            if (_lastObjFocus != null)
            {
                _lastObjFocus.MouseExit();
                _lastObjFocus = null;
            }
        }
    }

    public bool IsFocusable(Node node)
    {
        if (ray != null && ray.VisualObject != null)
        {
            if (ray.VisualObject == node)
            {
                return false;
            }
        }
        return true;
    }

    public void PlaceMode(bool enable)
    {
        if (ray.VisualObject != null)
        {
            Spatial visu = ray.VisualObject.GetNodeOrNull<Spatial>("Interface");
            if (visu != null)
            {
                visu.Visible = !enable;
            }
            for (int i = 0; i < ray.VisualObject.GetChildCount(); i++)
            {
                Node child = ray.VisualObject.GetChildOrNull<Node>(i);
                if (child != null)
                {
                    if (child is MeshInstance)
                    {
                        MeshInstance mesh = child as MeshInstance;
                        for (int j = 0; j < mesh.GetSurfaceMaterialCount(); j++)
                        {
                            ShaderMaterial mat = (ShaderMaterial)mesh.GetActiveMaterial(j);
                            if (mat.Shader.HasParam("Emission"))
                            {
                                mat.SetShaderParam("Emission", ray.Placeable ? Colors.Black : Colors.Red);

                            }
                        }
                        for (int j = 0; j < mesh.GetChildCount(); j++)
                        {
                            PhysicsBody body = mesh.GetChildOrNull<PhysicsBody>(j);
                            if (body != null)
                            {
                                body.CollisionLayer = (uint)(enable ? 0 : 1);
                                body.CollisionMask = (uint)(enable ? 0 : 1);
                            }
                        }
                    }
                }
            }
        }
    }
}
