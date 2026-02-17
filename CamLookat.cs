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

	private float pitch;
	private float yaw;

	private float camDistanceMax = 10f;
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

	public void moveCam(Node3D node)
	{

        GD.Print("Test");
        GD.Print("Old pos is: " + moveTarget.Position);

        moveTarget = node;
		move = true;

        GD.Print("New pos is: " + moveTarget.Position);
    }

	private void OnCamIdleTimeout()
	{
		idle = true;
	}
}
