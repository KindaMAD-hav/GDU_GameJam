using UnityEngine;

public class GrapplingScript : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 grapplePoint;
    public LayerMask whatIsGrappleable;
    public Transform gunTip, camera, player;
    private float maxDistance = 100f;
    private SpringJoint joint;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        Debug.Log("GrapplingScript initialized.");
    }

    void Update()
    {
        DrawRope();
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Grapple button pressed.");
            StartGrapple();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("Grapple button released.");
            StopGrapple();
        }
    }

    void StartGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(camera.position, camera.forward, out hit, maxDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            Debug.Log("Grapple hit point: " + grapplePoint);
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);
            Debug.Log("Distance to grapple point: " + distanceFromPoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;
        }
        else
        {
            Debug.Log("No valid grapple point found.");
        }
    }

    void StopGrapple()
    {
        if (joint != null)
        {
            Debug.Log("Grapple stopped.");
            Destroy(joint);
        }
    }

    void DrawRope()
    {
        // Rope drawing logic here
        Debug.Log("Drawing rope...");
    }

    public bool IsGrappling()
    {
        return joint != null;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
