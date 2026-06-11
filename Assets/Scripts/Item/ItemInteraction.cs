using UnityEngine;

namespace Game
{
    public class ItemInteraction : MonoBehaviour,IInteractable
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public string InteractMessage => "Nhấn E để tương tác";

        public bool CanInteract => true;

        public void Interact()
        {
            Debug.Log("Đã tương tác");
        }
    }
}
