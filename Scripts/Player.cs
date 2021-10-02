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

    private GadgetType _gadget = GadgetType.Weapon;
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

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public override void _ExitTree()
    {
        if (Instance != this)
        {
            Instance = null;
        }
    }

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
                    string resPath = "res://Scenes/Objects/TowerTest.tscn";
                    if (_gadget == GadgetType.Repeater)
                    {
                        resPath = "res://Scenes/Objects/Repeater.tscn";
                    }
                    else
                    {
                        switch (_gadgetDataID)
                        {
                            case 0:
                                resPath = "res://Scenes/Objects/TowerIce.tscn";
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
                        ray.VisualObject.GetNode<PhysicsBody>("Object/Body").CollisionLayer = 0;
                        GetTree().CurrentScene.GetNode("World/Navigation/NavigationMeshInstance").AddChild(ray.VisualObject);
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
                        GetTree().CurrentScene.GetNode("World/Navigation/NavigationMeshInstance").RemoveChild(ray.VisualObject);
                        ray.VisualObject.QueueFree();
                    }
                    else
                    {
                        ray.VisualObject.GetNode<PhysicsBody>("Object/Body").CollisionLayer = 1;
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
                    startLink = (PhysicsBody)collid["collider"];
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

                        break;
                    case GadgetType.Link:
                        ControlActionLink(button);
                        break;
                    case GadgetType.Repeater:
                    case GadgetType.Tower:
                        ControlActionPlaceTower(button);
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
            _speed = Mathf.Max(_speed, 5.0f);
            _speed += (_speed * 1.5f * Mathf.Abs(forward)) * delta;
            _speed = Mathf.Min(_speed, 10.0f);
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
                var collid = space.IntersectRay(ray.position, ray.position + ray.direction * 20.0f,
                    new Godot.Collections.Array() { this.GetRid(), body.GetRid() }, 2);

                if (collid.Count > 0)
                {
                    obj.Translation = (Vector3)collid["position"];

                    timeTest -= delta;
                    if (timeTest <= 0.0f)
                    {
                        timeTest += 1.0f;

                        var shape = body.GetNode<CollisionShape>("CollisionShape");
                        PhysicsShapeQueryParameters query = new PhysicsShapeQueryParameters();
                        query.SetShape(shape.Shape);
                        query.Exclude = new Godot.Collections.Array() { body.GetRid() };
                        query.Transform = obj.Transform;
                        query.CollisionMask = 1;
                        var result = space.IntersectShape(query);
                        ray.Placeable = result.Count <= 0;
                    }
                }
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        Movement(delta);
        switch (_gadget)
        {
            case GadgetType.Repeater:
            case GadgetType.Tower:
                PlaceTowerOver(delta);
                break;
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

}
