using System;
using System.Collections; // Needed for IEnumerator
using UnityEngine;
using UnityEngine.UI;     // Needed for UI Image

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

    [Header("Movement Settings")]
    public float sensitivity = 50f;
    public float moveSpeed = 4500f;
    public float walkSpeed = 20f;
    public float runSpeed = 10f;
    public bool grounded;
    public bool onWall;

    [Header("Dash Settings")]
    public float dashForce = 30f;       // Force applied when dashing
    public float dashDuration = 0.2f;   // How long the dash lasts
    public float dashCooldown = 1f;     // Time before dash can be used again

    [Header("Dash UI")]
    public Image dashFillImage;         // Drag a UI Image here (Type = Filled)

    // Dash state
    private bool isDashing = false;
    [HideInInspector] public float dashTimer = 0f; // Tracks cooldown left

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
    private bool surfing;
    private bool cancellingGrounded;
    private bool cancellingWall;
    private bool cancellingSurf;

    // Private Vector3's
    private Vector3 normalVector;
    private Vector3 wallNormalVector;
    private Vector3 lastCheckpointPosition;

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
        // 1) Count down dash timer
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer < 0f) dashTimer = 0f;
        }

        // 2) Update dash UI fill (0 = just dashed, 1 = fully recharged)
        if (dashCooldown > 0f && dashFillImage != null)
        {
            float fill = 1f - (dashTimer / dashCooldown);
            dashFillImage.fillAmount = fill;
        }

        // 3) Check for dash input (Left Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashTimer <= 0f && !isDashing)
        {
            StartCoroutine(Dash());
        }

        // 4) Other player inputs
        MyInput();

        // 5) Mouse look
        Look();
    }

    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.C);

        // Crouch toggle
        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCrouch();
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            StopCrouch();
        }
    }

    private void StartCrouch()
    {
        float slideBoost = 400f;
        transform.localScale = new Vector3(1f, 0.5f, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);

        // Slide boost if moving & grounded
        if (rb.linearVelocity.magnitude > 0.1f && grounded)
        {
            rb.AddForce(orientation.transform.forward * slideBoost);
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

        // Jump
        if (readyToJump && jumping)
        {
            Jump();
        }

        float maxSpeed = walkSpeed;
        if (sprinting) maxSpeed = runSpeed;

        // Crouch slowdown
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000f);
            return;
        }

        // Limit horizontal speed if already at max
        if (x > 0f && num > maxSpeed) x = 0f;
        if (x < 0f && num < -maxSpeed) x = 0f;
        if (y > 0f && num2 > maxSpeed) y = 0f;
        if (y < 0f && num2 < -maxSpeed) y = 0f;

        float multiplier = 1f;
        float multiplierV = 1f;

        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }
        if (grounded && crouching)
        {
            multiplierV = 0f;
        }
        if (wallRunning)
        {
            multiplier = 0.3f;
            multiplierV = 0.3f;
        }
        if (surfing)
        {
            multiplier = 0.7f;
            multiplierV = 0.3f;
        }

        // Apply movement
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Jump()
    {
        if ((grounded || wallRunning || surfing) && readyToJump)
        {
            // Debugging jump
            // MonoBehaviour.print("jumping");

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
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        desiredX = playerCam.transform.localRotation.eulerAngles.y + mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        FindWallRunRotation();
        actualWallRotation = Mathf.SmoothDamp(actualWallRotation, wallRunRotation, ref wallRotationVel, 0.2f);

        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, actualWallRotation);
        orientation.transform.localRotation = Quaternion.Euler(0f, desiredX, 0f);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        float counterForce = 0.16f;
        float threshold = 0.01f;

        // Sliding slowdown
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideSlowdown);
            return;
        }

        // Counter movement on X
        if ((Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f) ||
            (mag.x < -threshold && x > 0f) ||
            (mag.x > threshold && x < 0f))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * (-mag.x) * counterForce);
        }
        // Counter movement on Y
        if ((Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f) ||
            (mag.y < -threshold && y > 0f) ||
            (mag.y > threshold && y < 0f))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * (-mag.y) * counterForce);
        }

        // Limit diagonal running
        float currentSpeed = Mathf.Sqrt(rb.linearVelocity.x * rb.linearVelocity.x + rb.linearVelocity.z * rb.linearVelocity.z);
        if (currentSpeed > walkSpeed)
        {
            float fallSpeed = rb.linearVelocity.y;
            Vector3 limitedVel = rb.linearVelocity.normalized * walkSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, fallSpeed, limitedVel.z);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(lookAngle, moveAngle);
        float xMag = rb.linearVelocity.magnitude * Mathf.Cos((90f - delta) * Mathf.Deg2Rad);
        float yMag = rb.linearVelocity.magnitude * Mathf.Cos(delta * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void FindWallRunRotation()
    {
        if (!wallRunning)
        {
            wallRunRotation = 0f;
            return;
        }

        float angleToWall = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), wallNormalVector, Vector3.up);
        float currentCamAngle = playerCam.transform.rotation.eulerAngles.y;
        float delta = Mathf.DeltaAngle(currentCamAngle, angleToWall);

        wallRunRotation = (-delta / 90f) * 15f;
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
        float angle = Vector3.Angle(Vector3.up, v);
        if (angle < 89f)
        {
            return angle > maxSlopeAngle;
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
            float wallBoost = 20f;

            if (!wallRunning)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * wallBoost, ForceMode.Impulse);
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

        float invokeDelay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke("StopGrounded", Time.deltaTime * invokeDelay);
        }
        if (!cancellingWall)
        {
            cancellingWall = true;
            Invoke("StopWall", Time.deltaTime * invokeDelay);
        }
        if (!cancellingSurf)
        {
            cancellingSurf = true;
            Invoke("StopSurf", Time.deltaTime * invokeDelay);
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

    // --------------------------------
    // DASH COROUTINE
    // --------------------------------
    private IEnumerator Dash()
    {
        isDashing = true;

        // Determine dash direction from WASD input
        Vector3 dashDirection = orientation.forward * y + orientation.right * x;
        if (dashDirection.sqrMagnitude < 0.1f)
        {
            // If no input, dash forward
            dashDirection = orientation.forward;
        }
        dashDirection.Normalize();

        // Apply an impulse in that direction
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);

        // Dash lasts for dashDuration
        yield return new WaitForSeconds(dashDuration);

        // End dash
        isDashing = false;

        // Start cooldown
        dashTimer = dashCooldown;
    }
}
