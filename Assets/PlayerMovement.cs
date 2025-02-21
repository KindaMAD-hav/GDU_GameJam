using System;
using UnityEngine;
using System.Collections; // Needed for IEnumerator

public class PlayerMovement : MonoBehaviour
{
    [Header("Assignables")]
    public Transform playerCam;
    public Transform orientation;
    private Collider playerCollider;
    public Rigidbody rb;

    [Space(10)]
    public LayerMask whatIsGround;
    public LayerMask whatIsWallrunnable;

    [Header("MovementSettings")]
    public float sensitivity = 50f;
    public float moveSpeed = 4500f;
    public float walkSpeed = 20f;
    public float runSpeed = 10f;
    public bool grounded;
    public bool onWall;

    // -- Dash Settings --
    [Header("Dash Settings")]
    public float dashForce = 30f;       // Force applied when dashing
    public float dashDuration = 0.2f;   // How long the dash lasts
    public float dashCooldown = 1f;     // Time before dash can be used again

    // Private floats
    private float wallRunGravity = 1f;
    private float maxSlopeAngle = 35f;
    private float wallRunRotation;
    private float slideSlowdown = 0.2f;
    private float actualWallRotation;
    private float wallRotationVel;
    private float desiredX;
    private float xRotation;
    private float sensMultiplier = 1f;
    private float jumpCooldown = 0.25f;
    private float jumpForce = 550f;
    private float x;
    private float y;

    // Private bools
    private bool readyToJump;
    private bool jumping;
    private bool sprinting;
    private bool crouching;
    private bool wallRunning;
    private bool cancelling;
    private bool readyToWallrun = true;
    private bool airborne;
    private bool onGround;
    private bool surfing;
    private bool cancellingGrounded;
    private bool cancellingWall;
    private bool cancellingSurf;

    // -- Dash State --
    private bool isDashing = false;
    private bool canDash = true;

    // Private Vector3's
    private Vector3 normalVector;
    private Vector3 wallNormalVector;
    private Vector3 wallRunPos;
    private Vector3 previousLookdir;
    private Vector3 lastCheckpointPosition;

    // Private int
    private int nw;

    // Instance
    public static PlayerMovement Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        playerCollider = GetComponent<Collider>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        readyToJump = true;
        wallNormalVector = Vector3.up;
        lastCheckpointPosition = transform.position;
        Debug.Log("[PlayerMovement] Starting position set to: " + lastCheckpointPosition);
    }

    public void SetCheckpoint(Vector3 checkpoint)
    {
        lastCheckpointPosition = checkpoint;
        Debug.Log("[PlayerMovement] Checkpoint set to: " + lastCheckpointPosition);
    }

    public void Respawn()
    {
        Debug.Log("[PlayerMovement] Respawning to: " + lastCheckpointPosition);
        transform.position = lastCheckpointPosition;
        rb.linearVelocity = Vector3.zero; // Reset velocity
    }

    private void LateUpdate()
    {
        // Wallrunning logic
        WallRunning();
    }

    private void FixedUpdate()
    {
        // Normal movement logic
        Movement();
    }

    private void Update()
    {
        // Handle input
        MyInput();

        // Mouse look
        Look();
    }

    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.C);

        // Crouch
        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCrouch();
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            StopCrouch();
        }

        // Dash (Left Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartCoroutine(Dash());
        }
    }

    private void StartCrouch()
    {
        float num = 400f;
        transform.localScale = new Vector3(1f, 0.5f, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);

        // Slide boost
        if (rb.linearVelocity.magnitude > 0.1f && grounded)
        {
            rb.AddForce(orientation.transform.forward * num);
        }
    }

    private void StopCrouch()
    {
        transform.localScale = new Vector3(1f, 1.5f, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        // If we're currently dashing, skip normal movement
        if (isDashing) return;

        // Gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10f);

        Vector2 mag = FindVelRelativeToLook();
        float num = mag.x;
        float num2 = mag.y;

        CounterMovement(x, y, mag);

        if (readyToJump && jumping)
        {
            Jump();
        }

        float num3 = walkSpeed;
        if (sprinting)
        {
            num3 = runSpeed;
        }

        // Crouch movement
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000f);
            return;
        }

        // Limit movement if already at max speeds
        if (x > 0f && num > num3) x = 0f;
        if (x < 0f && num < -num3) x = 0f;
        if (y > 0f && num2 > num3) y = 0f;
        if (y < 0f && num2 < -num3) y = 0f;

        float num4 = 1f;
        float num5 = 1f;
        if (!grounded)
        {
            num4 = 0.5f;
            num5 = 0.5f;
        }
        if (grounded && crouching)
        {
            num5 = 0f;
        }
        if (wallRunning)
        {
            num5 = 0.3f;
            num4 = 0.3f;
        }
        if (surfing)
        {
            num4 = 0.7f;
            num5 = 0.3f;
        }

        // Apply movement forces
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * num4 * num5);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * num4);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Jump()
    {
        if ((grounded || wallRunning || surfing) && readyToJump)
        {
            Vector3 velocity = rb.linearVelocity;
            readyToJump = false;

            // Upward force
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            // Reset vertical velocity if needed
            if (rb.linearVelocity.y < 0.5f)
            {
                rb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            }
            else if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector3(velocity.x, velocity.y / 2f, velocity.z);
            }

            // Extra force if wallrunning
            if (wallRunning)
            {
                rb.AddForce(wallNormalVector * jumpForce * 3f);
                wallRunning = false;
            }

            Invoke("ResetJump", jumpCooldown);
        }
    }

    private void Look()
    {
        float num = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float num2 = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        desiredX = playerCam.transform.localRotation.eulerAngles.y + num;
        xRotation -= num2;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        FindWallRunRotation();
        actualWallRotation = Mathf.SmoothDamp(actualWallRotation, wallRunRotation, ref wallRotationVel, 0.2f);

        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, actualWallRotation);
        orientation.transform.localRotation = Quaternion.Euler(0f, desiredX, 0f);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        float num = 0.16f;
        float num2 = 0.01f;

        // Sliding slowdown
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideSlowdown);
            return;
        }

        // Counter movement on X axis
        if ((Math.Abs(mag.x) > num2 && Math.Abs(x) < 0.05f) || (mag.x < -num2 && x > 0f) || (mag.x > num2 && x < 0f))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * (-mag.x) * num);
        }
        // Counter movement on Y axis
        if ((Math.Abs(mag.y) > num2 && Math.Abs(y) < 0.05f) || (mag.y < -num2 && y > 0f) || (mag.y > num2 && y < 0f))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * (-mag.y) * num);
        }

        // Limit diagonal running
        if (Mathf.Sqrt(Mathf.Pow(rb.linearVelocity.x, 2f) + Mathf.Pow(rb.linearVelocity.z, 2f)) > walkSpeed)
        {
            float num3 = rb.linearVelocity.y;
            Vector3 vector = rb.linearVelocity.normalized * walkSpeed;
            rb.linearVelocity = new Vector3(vector.x, num3, vector.z);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float current = orientation.transform.eulerAngles.y;
        float target = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;
        float num = Mathf.DeltaAngle(current, target);
        float num2 = 90f - num;
        float magnitude = rb.linearVelocity.magnitude;

        float xMag = magnitude * Mathf.Cos(num2 * Mathf.Deg2Rad);
        float yMag = magnitude * Mathf.Cos(num * Mathf.Deg2Rad);
        return new Vector2(xMag, yMag);
    }

    private void FindWallRunRotation()
    {
        if (!wallRunning)
        {
            wallRunRotation = 0f;
            return;
        }

        float num = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), wallNormalVector, Vector3.up);
        float current = playerCam.transform.rotation.eulerAngles.y;
        float num2 = Mathf.DeltaAngle(current, num);

        wallRunRotation = (-num2 / 90f) * 15f;

        if (!readyToWallrun) return;

        if ((Mathf.Abs(wallRunRotation) < 4f && y > 0f && Math.Abs(x) < 0.1f) ||
            (Mathf.Abs(wallRunRotation) > 22f && y < 0f && Math.Abs(x) < 0.1f))
        {
            if (!cancelling)
            {
                cancelling = true;
                CancelInvoke("CancelWallrun");
                Invoke("CancelWallrun", 0.2f);
            }
        }
        else
        {
            cancelling = false;
            CancelInvoke("CancelWallrun");
        }
    }

    private void CancelWallrun()
    {
        Invoke("GetReadyToWallrun", 0.1f);
        rb.AddForce(wallNormalVector * 600f);
        readyToWallrun = false;
    }

    private void GetReadyToWallrun()
    {
        readyToWallrun = true;
    }

    private void WallRunning()
    {
        if (wallRunning)
        {
            rb.AddForce(-wallNormalVector * Time.deltaTime * moveSpeed);
            rb.AddForce(Vector3.up * Time.deltaTime * rb.mass * 100f * wallRunGravity);
        }
    }

    private bool IsFloor(Vector3 v)
    {
        return Vector3.Angle(Vector3.up, v) < maxSlopeAngle;
    }

    private bool IsSurf(Vector3 v)
    {
        float num = Vector3.Angle(Vector3.up, v);
        if (num < 89f)
        {
            return num > maxSlopeAngle;
        }
        return false;
    }

    private bool IsWall(Vector3 v)
    {
        return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
    }

    private bool IsRoof(Vector3 v)
    {
        return v.y == -1f;
    }

    private void StartWallRun(Vector3 normal)
    {
        if (!grounded && readyToWallrun)
        {
            wallNormalVector = normal;
            float num = 20f;
            if (!wallRunning)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * num, ForceMode.Impulse);
            }
            wallRunning = true;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;

        // Check if this collision is with ground
        if ((int)whatIsGround != ((int)whatIsGround | (1 << layer)))
        {
            return;
        }

        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;

            if (IsFloor(normal))
            {
                if (wallRunning) wallRunning = false;
                grounded = true;
                normalVector = normal;
                cancellingGrounded = false;
                CancelInvoke("StopGrounded");
            }

            if (IsWall(normal) && layer == LayerMask.NameToLayer("Ground"))
            {
                StartWallRun(normal);
                onWall = true;
                cancellingWall = false;
                CancelInvoke("StopWall");
            }

            if (IsSurf(normal))
            {
                surfing = true;
                cancellingSurf = false;
                CancelInvoke("StopSurf");
            }
            IsRoof(normal);
        }

        float num2 = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke("StopGrounded", Time.deltaTime * num2);
        }
        if (!cancellingWall)
        {
            cancellingWall = true;
            Invoke("StopWall", Time.deltaTime * num2);
        }
        if (!cancellingSurf)
        {
            cancellingSurf = true;
            Invoke("StopSurf", Time.deltaTime * num2);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

    private void StopWall()
    {
        onWall = false;
        wallRunning = false;
    }

    private void StopSurf()
    {
        surfing = false;
    }

    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }

    public float GetFallSpeed()
    {
        return rb.linearVelocity.y;
    }

    public Collider GetPlayerCollider()
    {
        return playerCollider;
    }

    public Transform GetPlayerCamTransform()
    {
        return playerCam.transform;
    }

    public bool IsCrouching()
    {
        return crouching;
    }

    public Rigidbody GetRb()
    {
        return rb;
    }

    // ---------------------------
    // DASH COROUTINE
    // ---------------------------
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        // 1) Determine dash direction from input
        //    (This example assumes you store horizontal/vertical input in x/y).
        Vector3 dashDirection = orientation.forward * y + orientation.right * x;

        // 2) If no input is pressed, fall back to facing forward
        if (dashDirection.sqrMagnitude < 0.1f)
        {
            dashDirection = orientation.forward;
        }

        dashDirection.Normalize();

        // 3) Apply an impulse in that direction
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);

        // 4) Dash lasts for dashDuration
        yield return new WaitForSeconds(dashDuration);

        // End dash
        isDashing = false;

        // 5) Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

}
