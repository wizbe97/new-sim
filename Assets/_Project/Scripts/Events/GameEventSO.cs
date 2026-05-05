using System;
using UnityEngine;

namespace Project.Events
{
    [CreateAssetMenu(
        fileName = "GameEvent",
        menuName = "Project/Events/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        public event Action Raised;

        public void Raise()
        {
            Raised?.Invoke();
        }
    }
}