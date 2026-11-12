using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère la manipulation physique 3D des objets sur l'établi à la souris via un plan mathématique
/// et l'application de vélocité dans la boucle physique (FixedUpdate). Actif uniquement sur l'établi.
/// </summary>
public class WorkbenchObjectDragger : MonoBehaviour
{
    [Header("Réglages Drag 3D")]
    public Camera mainCam; 
    public LayerMask draggableLayer = ~0;
    public float dragHeightOffset = 0.15f;
    public float followSpeed = 15f;
    public float raycastDistance = 50f;

    private Rigidbody grabbedRb;
    private Plane dragPlane;
    private PlayerWorkbench playerWorkbench;

    private float originalLinearDamping;
    private float originalAngularDamping;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        playerWorkbench = GetComponentInParent<PlayerWorkbench>();
    }

    void Update()
    {
        if (playerWorkbench != null && (!playerWorkbench.IsOwner || !playerWorkbench.IsAtWorkbench))
        {
            if (grabbedRb != null) ReleaseObject();
            return;
        }

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryGrabObject();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && grabbedRb != null)
        {
            ReleaseObject();
        }
    }

    void FixedUpdate()
    {
        if (grabbedRb != null && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            DragObjectPhysics();
        }
    }

    void TryGrabObject()
    {
        if (mainCam == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, draggableLayer, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
            
            if (rb != null)
            {
                grabbedRb = rb;
                
                originalLinearDamping = grabbedRb.linearDamping;
                originalAngularDamping = grabbedRb.angularDamping;

                grabbedRb.useGravity = false;
                grabbedRb.linearDamping = 10f; 
                grabbedRb.angularDamping = 10f;
                
                dragPlane = new Plane(Vector3.up, grabbedRb.position + Vector3.up * dragHeightOffset);
            }
        }
    }

    void DragObjectPhysics()
    {
        if (mainCam == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPoint = ray.GetPoint(enter);
            Vector3 direction = targetPoint - grabbedRb.position;
            grabbedRb.linearVelocity = direction * followSpeed;
        }
    }

    public void ReleaseObject()
    {
        if (grabbedRb == null) return;

        grabbedRb.useGravity = true;
        grabbedRb.linearDamping = originalLinearDamping;
        grabbedRb.angularDamping = originalAngularDamping;
        grabbedRb = null;
    }

    void OnDisable()
    {
        if (grabbedRb != null) ReleaseObject();
    }
}