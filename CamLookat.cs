using Godot;
using System;

public partial class CamLookat : Marker3D
{
	[Export]
	public Node3D lookTarget;
    [Export]
    public Node3D moveTarget;
	public Node3D secondTarget;

	[Export]
	public Camera3D cam;
    [Export]
    public RayCast3D rayCenter;
    [Export]
    public RayCast3D rayTop;
    [Export]
    public RayCast3D rayBottom;
    [Export]
    public RayCast3D rayLeft;
    [Export]
    public RayCast3D rayRight;


    private float pitch;
	private float yaw;

	private float camDistanceMax = 6f;
	private float camDistance;
	private float camRotSpeed = 2.5f;

	[Export]
	public Timer camTimer;

	private bool idle;
    private bool move;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (moveTarget != null)
		{
			Position = moveTarget.Position;
		}

		move = false;
		idle = false;

		camTimer.Timeout += OnCamIdleTimeout;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (lookTarget != null)
		{
            LookAt(lookTarget.GlobalPosition, Vector3.Up);
        }
	}

    public override void _PhysicsProcess(double delta)
    {
        rayCenter.TargetPosition = cam.Position;
        rayTop.TargetPosition = cam.Position + new Vector3(0, 0.1f, 0.5f);
        rayBottom.TargetPosition = cam.Position + new Vector3(0, -0.1f, 0.5f);
        rayLeft.TargetPosition = cam.Position + new Vector3(0.1f, 0, 0.5f);
        rayRight.TargetPosition = cam.Position + new Vector3(-0.1f, 0, 0.5f);

        if (rayCenter.IsColliding())
		{
			GD.Print(cam.Position.DistanceTo(rayCenter.GetCollisionPoint()));
			cam.Position = cam.Position.Slerp(new Vector3(0, 0, cam.Position.Z - cam.Position.DistanceTo(rayCenter.GetCollisionPoint())), (float)delta * (cam.Position.DistanceTo(rayCenter.GetCollisionPoint())));
		}
		else if (!rayCenter.IsColliding() && !rayTop.IsColliding() && !rayBottom.IsColliding() && !rayLeft.IsColliding() && !rayRight.IsColliding())
		{
            cam.Position = cam.Position.Slerp(new Vector3(0, 0, camDistanceMax), (float)delta);
        }

        if (rayTop.IsColliding())
        {
            pitch += 0.5f * camRotSpeed / 5f * (float)delta;
        }
        if (rayBottom.IsColliding())
        {
            pitch -= 0.5f * camRotSpeed / 5f * (float)delta;
        }
        if (rayLeft.IsColliding())
        {
            yaw -= 0.5f * camRotSpeed / 5f * (float)delta;
        }
        if (rayRight.IsColliding())
		{
            yaw += 0.5f * camRotSpeed / 5f * (float)delta;
        }


		Position = Position.Slerp(moveTarget.Position + new Vector3(0, 1, 0), (float)delta * 4);

        Vector2 camInputDir = Input.GetVector("viewLeft", "viewRight", "viewDown", "viewUp");


		if (camInputDir != Vector2.Zero)
		{
			idle = false;
			camTimer.Stop();
		}
		else 
		{
			if (camTimer.IsStopped() && !idle)
			{
				camTimer.Start();
			}
		}

        Vector2 plrInputDir = Input.GetVector("left", "right", "back", "forward");

        Vector3 targetRotation = new Vector3 (Rotation.X + camInputDir.Y, Rotation.Y + camInputDir.X, 0);
        //GD.Print(targetRotation);

		pitch += camInputDir.Y * camRotSpeed * (float)delta;
        yaw += camInputDir.X * camRotSpeed * (float)delta;

		pitch = Mathf.Clamp(pitch, -Mathf.Pi / 2f, Mathf.Pi / 2f);

		if (idle && plrInputDir != Vector2.Zero)
		{
			if (pitch != -0.5f)
			{
				pitch = (float)Mathf.Lerp(pitch, -0.5f, delta * camRotSpeed / 5f);
			}
		}

        if (idle && plrInputDir.X != 0 && plrInputDir.Y >= 0)
        {
            yaw -= plrInputDir.X * camRotSpeed / 5f * (float)delta;
        }
		else if (idle && plrInputDir.X != 0 && plrInputDir.Y < 0)
		{
            yaw -= plrInputDir.X * camRotSpeed / 2.5f * (float)delta;
        }


            Rotation = new Vector3(pitch, yaw, 0);

        /*if (move)
		{
			
			if (Position.DistanceTo(moveTarget.Position) < 0.1f)
			{
				move = false;
			}
		}*/

        base._PhysicsProcess(delta);
    }

	private void OnCamIdleTimeout()
	{
		idle = true;
	}
}
