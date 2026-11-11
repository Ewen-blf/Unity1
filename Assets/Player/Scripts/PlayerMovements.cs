using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour 
{
    [Header("Paramètres de déplacement")]
    public float speed = 6.0f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    
    // On remplace l'ancienne variable par une gâchette (trigger)
    private bool jumpTrigger = false; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<PlayerInput>().enabled = false;
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        // On arme la gâchette UNIQUEMENT au moment où la touche est enfoncée
        if (value.isPressed)
        {
            jumpTrigger = true;
        }
    }

    void Update()
    {
        if (!IsOwner) return; 

        if (controller.isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Déclenchement du saut
            if (jumpTrigger)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpTrigger = false; // On désarme la gâchette immédiatement pour éviter le rebond
            }
        }
        else
        {
            // Si on est en l'air, on désarme aussi la gâchette. 
            // Ça empêche le joueur d'appuyer sur Espace en plein vol 
            // et de sauter automatiquement dès qu'il touche le sol.
            jumpTrigger = false;
        }

        // Mouvements
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}