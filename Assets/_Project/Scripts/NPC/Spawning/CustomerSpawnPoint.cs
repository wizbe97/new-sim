using UnityEngine;

namespace Project.NPC.Spawning
{
    public sealed class CustomerSpawnPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
    }
}