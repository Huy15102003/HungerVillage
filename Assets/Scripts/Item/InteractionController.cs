using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class InteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private TextMeshProUGUI interactionText;

        [Header("Settings")]
        [SerializeField] private float interactDistance = 3f;

        private IInteractable currentInteractable;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (interactionText != null)
            {
                interactionText.text = "";
            }

        }

        private void Update()
        {
            CheckInteractable();
            CheckInput();
        }

        private void CheckInteractable()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                currentInteractable = hit.collider.GetComponentInParent<IInteractable>();

                if (currentInteractable != null &&
                    currentInteractable.CanInteract)
                {
                    interactionText.text = currentInteractable.InteractMessage;
                    return;
                }
            }

            currentInteractable = null;
            interactionText.text = "";
        }

        private void CheckInput()
        {
            if (currentInteractable == null)
                return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentInteractable.Interact();
            }
        }
    }
}