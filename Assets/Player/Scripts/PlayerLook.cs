using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;

/// <summary>
/// Contrôle la rotation de la caméra à la première personne (axe vertical) 
/// et l'orientation du corps du joueur (axe horizontal) pour le propriétaire réseau.
/// </summary>
public class PlayerLook : NetworkBehaviour
{
    [Header("Paramètres de vue")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    private Vector2 lookInput;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && playerCamera != null)
        {
            var cinemachineCam = playerCamera.GetComponent<CinemachineCamera>();
            if (cinemachineCam != null)
            {
                cinemachineCam.enabled = false;
            }
        }
    }

    void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        if (!IsOwner) return;

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}