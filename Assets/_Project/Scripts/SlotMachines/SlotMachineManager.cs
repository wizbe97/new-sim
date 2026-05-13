using System.Collections.Generic;
using UnityEngine;

namespace Project.SlotMachines
{
    public sealed class SlotMachineManager : MonoBehaviour
    {
        [Header("Global Slot Settings")]
        [SerializeField, Min(0.1f)] private float spinDurationSeconds = 0.35f;

        private readonly List<SlotMachine> registeredMachines = new();

        public float SpinDurationSeconds => spinDurationSeconds;

        public void Register(SlotMachine slotMachine)
        {
            if (slotMachine == null || registeredMachines.Contains(slotMachine))
            {
                return;
            }

            registeredMachines.Add(slotMachine);
        }

        public void Unregister(SlotMachine slotMachine)
        {
            if (slotMachine == null)
            {
                return;
            }

            registeredMachines.Remove(slotMachine);
        }

        public bool TryGetAvailableMachine(out SlotMachine slotMachine)
        {
            slotMachine = null;

            float bestScore = float.NegativeInfinity;

            foreach (SlotMachine machine in registeredMachines)
            {
                if (machine == null || !machine.IsAvailable)
                {
                    continue;
                }

                float score = machine.AttractionScore + Random.Range(0f, 0.15f);

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                slotMachine = machine;
            }

            return slotMachine != null;
        }

        public void SetSpinDuration(float seconds)
        {
            spinDurationSeconds = Mathf.Max(0.1f, seconds);
        }
    }
}