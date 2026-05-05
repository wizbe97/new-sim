using UnityEngine;

namespace Project.Spawning
{
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = Color.green;
        [SerializeField, Min(0.1f)] private float gizmoRadius = 0.5f;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
        }
    }
}