using UnityEngine;
using UnityEngine.InputSystem;

public class WorkbenchObjectDragger : MonoBehaviour
{
    [Header("Réglages Drag 3D")]
    public Camera mainCam; 
    public LayerMask draggableLayer = ~0; // Par défaut : TOUT (Everything)
    public float dragHeightOffset = 0.15f;
    public float followSpeed = 15f;
    public float raycastDistance = 50f; // ⚠️ Augmenté à 50 mètres

    private Rigidbody grabbedRb;
    private Plane dragPlane;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) TryGrabObject();
        else if (Mouse.current.leftButton.isPressed && grabbedRb != null) DragObject();
        else if (Mouse.current.leftButton.wasReleasedThisFrame && grabbedRb != null) ReleaseObject();
    }

    void TryGrabObject()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        // 1. DESSINE UN LASER ROUGE DANS LA VUE SCENE DE UNITY (Visible 3 secondes)
        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 3f);
        Debug.Log($"🖱️ Clic détecté ! Rayon lancé...");

        // 2. LANCE LE RAYON PHYSIQUE
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, draggableLayer))
        {
            Debug.Log($"🎯 Rayon a touché : {hit.collider.gameObject.name}");

            // 3. ⚠️ CORRECTION : Cherche le Rigidbody même s'il est sur le parent !
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
            
            if (rb != null)
            {
                Debug.Log($"✅ Rigidbody trouvé sur {rb.gameObject.name} ! Objet attrapé.");
                grabbedRb = rb;
                grabbedRb.useGravity = false;
                grabbedRb.linearDamping = 10f; 
                grabbedRb.angularDamping = 10f;
                dragPlane = new Plane(Vector3.up, grabbedRb.position + Vector3.up * dragHeightOffset);
            }
            else
            {
                Debug.LogError($"❌ L'objet touché ({hit.collider.gameObject.name}) n'a pas de Rigidbody !");
            }
        }
        else
        {
            Debug.LogWarning("👻 Le rayon n'a RIEN touché. (Vérifie le laser rouge dans la vue Scene !)");
        }
    }

    void DragObject()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPoint = ray.GetPoint(enter);
            Vector3 direction = targetPoint - grabbedRb.position;
            grabbedRb.linearVelocity = direction * followSpeed;
        }
    }

    void ReleaseObject()
    {
        grabbedRb.useGravity = true;
        grabbedRb.linearDamping = 0f;
        grabbedRb.angularDamping = 0.05f;
        grabbedRb = null;
    }
}