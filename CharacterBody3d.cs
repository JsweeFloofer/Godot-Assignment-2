using Godot;
using System;
using System.Diagnostics;
using System.Transactions;

public partial class CharacterBody3d : CharacterBody3D
{
    [Export]
    public float maxSpeed = 15.0f;
	private float speedCap;
	private float currentSpeed = 0f;
	public const float JumpVelocity = 7.5f;

	private Vector3 direction;
	private Vector3 previousDirection;

    [Export]
	public Marker3D cam;

    [Export]
    public MeshInstance3D mesh;


    public float rotSpeed = 10f;

    [Export]
    public Timer jumpTimer;

    [Export]
    public Timer hoverTimer;
	private bool hovering = true;


    private float airTime = 0f;
    private float coyoteTime = 0.5f;
    private bool jumped = false;
	private bool jumpDelayActive = false;

	private int movementMode = 0;

	private Vector3 velocity;
	private Vector3 respawnPos;

    public override void _Ready()
    {
		speedCap = maxSpeed;
		respawnPos = Position;

		var areas = GetTree().GetNodesInGroup("Triggers");

		foreach (Node node in areas)
		{
			if (node is Area3D area)
			{
				area.BodyEntered += (Node3D body) =>
				{
					OnAreaBodyEntered(body, area);
				};
			}
		}
    }

	private void OnAreaBodyEntered(Node3D body, Area3D area)
	{
		if (body != this || !area.IsInGroup("Triggers"))
		{
			return;
		}
		
		if (area.IsInGroup("Camera"))
		{
			cam.Call("moveCam", area);
		}
		else if (area.IsInGroup("Deadzone"))
        {
			Position = respawnPos;
        }
        if (area.IsInGroup("Checkpoint"))
        {
			respawnPos = area.Position;
        }
    }

    public override void _PhysicsProcess(double delta)
	{
		velocity = Velocity;
 

		// Add the gravity.
		if (!IsOnFloor())
		{
			if (velocity.Y < 0 && !hovering)
			{
				hoverTimer.Start();
				hovering = true;
			}

            if (velocity.Y < 0 && !hoverTimer.IsStopped() && hovering)
            {
                velocity += GetGravity() * (float)delta * 0.05f;
				if (!Input.IsActionPressed("deny"))
				{
					hoverTimer.Stop();
				}
            }
			else
			{
				velocity += GetGravity() * (float)delta;

            }

			airTime += (float)delta;
            //GD.Print(velocity.Y);
        }
		else if (jumpTimer.IsStopped() && !jumpDelayActive)
		{
			airTime = 0;
			jumped = false;
			hovering = false;
		}
			
			// Handle Jump.
		if (Input.IsActionJustPressed("deny") && (IsOnFloor() || (airTime < coyoteTime && !jumped)))
		{
            jumpDelayActive = true;
            jumped = true;
			jumpTimer.Start();
        }
		if (jumpTimer.IsStopped() && jumpDelayActive)
		{
            velocity.Y = JumpVelocity;
            jumpDelayActive = false;
			
        }
		else if (jumpDelayActive && mesh.Position.Y > 0.5f)
		{
            mesh.Scale = new Vector3(1, mesh.Scale.Y - 0.05f, 1);
            mesh.Position = new Vector3(0, mesh.Position.Y - 0.05f, 0);
        }
		else if (velocity.Y > 0 && mesh.Position.Y < 1)
		{
            mesh.Scale = new Vector3(1, mesh.Scale.Y + 0.05f, 1);
            mesh.Position = new Vector3(0, mesh.Position.Y + 0.05f, 0);
        }
		else if (!Input.IsActionPressed("crouch"))
		{
            mesh.Scale = new Vector3(1, 1, 1);
            mesh.Position = new Vector3(0, 1, 0);
        }

		if (Input.IsActionPressed("crouch"))
		{
			if (speedCap > maxSpeed/2)
			{
                speedCap -= maxSpeed / 10;
				Scale = new Vector3(1, 1 / maxSpeed * speedCap, 1);
			}
			else if (speedCap < maxSpeed/2)
			{
                speedCap = maxSpeed/2;
				Scale = new Vector3(1, 0.5f, 1);
			}
		}
		else
		{
			if (speedCap < maxSpeed)
			{
                speedCap += maxSpeed / 10;
				Scale = new Vector3(1, 1 / maxSpeed * speedCap, 1);
				Position = new Vector3(Position.X, Position.Y, Position.Z);
			}
			else if (speedCap > maxSpeed)
			{
                speedCap = maxSpeed;
				Scale = new Vector3(1, 1, 1);
			}

		}


			// Get the input direction and handle the movement/deceleration
			// As good practice, you should replace UI actions with custom gameplay actions.
			Vector2 inputDir = Input.GetVector("left", "right", "forward", "back");

		if (cam != null)
		{
			Vector3 camForward = cam.GlobalTransform.Basis.Z;
			Vector3 camRight = cam.GlobalTransform.Basis.X;

			camForward.Y = 0;
			camRight.Y = 0;

			direction = (camRight * inputDir.X + camForward * inputDir.Y).Normalized();
		}
		else
		{
            direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        }

		// MOVEMEEENT MOOODES

		if (Input.IsKeyPressed(Key.Key1))
		{
			movementMode = 0;
		}
		else if (Input.IsKeyPressed(Key.Key2))
        {
            movementMode = 1;
        }
        else if (Input.IsKeyPressed(Key.Key3))

        {
            movementMode = 2;
        }



        if (direction != Vector3.Zero)
		{
			switch (movementMode)
			{
				case 0: // Instant
					currentSpeed = speedCap;
					break;

				case 1: // Slow constant
					if (currentSpeed < speedCap)
					{
						currentSpeed += speedCap / 50;
					}
					else
					{
						currentSpeed = speedCap;
					}

					break;

				case 2: // Ease
					if (currentSpeed <= 0)
					{
                        currentSpeed += speedCap / 50;
                    }
                    if (currentSpeed < speedCap)
                    {
						currentSpeed = currentSpeed * 1.05f;
                    }
                    else
                    {
                        currentSpeed = speedCap;
                    }

                    break;
			}

            //GD.Print("Moving");
            //GD.Print(currentSpeed);

			previousDirection = direction.Normalized();



            velocity.X = direction.X * currentSpeed;
            velocity.Z = direction.Z * currentSpeed;

            float targetRot = Mathf.Atan2(-direction.X, -direction.Z);


			//GD.Print(targetRot);

			Vector3 rotation = mesh.Rotation;
			rotation.Y = Mathf.LerpAngle(rotation.Y, targetRot, (float)delta * rotSpeed);

			mesh.Rotation = rotation;
		}
		else
		{
			//GD.Print("Slowing Down");

            switch (movementMode)
            {
                case 0: // Instant
                    currentSpeed = 0;

                    velocity.X = currentSpeed;
                    velocity.Z = currentSpeed;

                    break;


                case 1: // Fast constant
                    if (currentSpeed > 0)
                    {
                        currentSpeed -= speedCap / 25;
                    }
					else 
					{
						currentSpeed = 0;
					}

                    velocity.X = previousDirection.X * currentSpeed;
                    velocity.Z = previousDirection.Z * currentSpeed;

                    break;

                case 2: // Constant
                    if (currentSpeed > 0)
                    {
                        currentSpeed -= speedCap / 100;
                    }
                    else
                    {
                        currentSpeed = 0;
                    }

                    velocity.X = previousDirection.X * currentSpeed;
                    velocity.Z = previousDirection.Z * currentSpeed;

                    break;
            }

            //GD.Print(currentSpeed);


            
		}
        //GD.Print("direction X = " + direction.X);
        //GD.Print("direction Z = " + direction.Z);
		//GD.Print("direction X = " + direction.X);
        //GD.Print("direction Z = " + direction.Z);

        Velocity = velocity;
		MoveAndSlide();
	}




}
