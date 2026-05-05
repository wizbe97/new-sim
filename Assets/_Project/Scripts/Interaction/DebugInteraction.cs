using Project.Interfaces;
using UnityEngine;

namespace Project.Interaction
{
    public sealed class DebugInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Interact";

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract => true;

        public void Interact()
        {
            Debug.Log($"Interacted with {name}");
        }
    }
}