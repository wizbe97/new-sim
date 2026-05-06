using System.Collections.Generic;
using UnityEngine;

namespace Project.Hands
{
    public sealed class CarryablePhysics : MonoBehaviour
    {
        [Header("Drop Physics")]
        [SerializeField] private bool forceGravityOnDrop = true;
        [SerializeField] private bool forceNonKinematicOnDrop = true;
        [SerializeField] private bool forceCollisionsOnDrop = true;
        [SerializeField] private CollisionDetectionMode droppedCollisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        [SerializeField] private RigidbodyInterpolation droppedInterpolation = RigidbodyInterpolation.Interpolate;

        private readonly List<RigidbodyState> rigidbodyStates = new();
        private readonly List<ColliderState> colliderStates = new();

        private bool hasCachedState;

        public void DisableForCarry()
        {
            CacheState();

            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb == null)
                {
                    continue;
                }

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.detectCollisions = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.interpolation = RigidbodyInterpolation.None;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            foreach (Collider objectCollider in colliders)
            {
                if (objectCollider != null)
                {
                    objectCollider.enabled = false;
                }
            }
        }

        public void RestoreAfterCarry()
        {
            if (!hasCachedState)
            {
                EnableDroppedPhysics();
                return;
            }

            foreach (ColliderState state in colliderStates)
            {
                state.Restore();
            }

            foreach (RigidbodyState state in rigidbodyStates)
            {
                state.Restore();
            }

            ClearCachedState();
        }

        public void EnableDroppedPhysics()
        {
            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb == null)
                {
                    continue;
                }

                if (forceGravityOnDrop)
                {
                    rb.useGravity = true;
                }

                if (forceNonKinematicOnDrop)
                {
                    rb.isKinematic = false;
                }

                if (forceCollisionsOnDrop)
                {
                    rb.detectCollisions = true;
                }

                rb.collisionDetectionMode = droppedCollisionDetectionMode;
                rb.interpolation = droppedInterpolation;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            foreach (Collider objectCollider in colliders)
            {
                if (objectCollider != null)
                {
                    objectCollider.enabled = true;
                }
            }
        }

        private void CacheState()
        {
            rigidbodyStates.Clear();
            colliderStates.Clear();

            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null)
                {
                    rigidbodyStates.Add(new RigidbodyState(rb));
                }
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            foreach (Collider objectCollider in colliders)
            {
                if (objectCollider != null)
                {
                    colliderStates.Add(new ColliderState(objectCollider));
                }
            }

            hasCachedState = true;
        }

        private void ClearCachedState()
        {
            rigidbodyStates.Clear();
            colliderStates.Clear();
            hasCachedState = false;
        }

        private readonly struct RigidbodyState
        {
            private readonly Rigidbody rigidbody;
            private readonly bool useGravity;
            private readonly bool isKinematic;
            private readonly bool detectCollisions;
            private readonly CollisionDetectionMode collisionDetectionMode;
            private readonly RigidbodyInterpolation interpolation;

            public RigidbodyState(Rigidbody rigidbody)
            {
                this.rigidbody = rigidbody;
                useGravity = rigidbody.useGravity;
                isKinematic = rigidbody.isKinematic;
                detectCollisions = rigidbody.detectCollisions;
                collisionDetectionMode = rigidbody.collisionDetectionMode;
                interpolation = rigidbody.interpolation;
            }

            public void Restore()
            {
                if (rigidbody == null)
                {
                    return;
                }

                rigidbody.useGravity = useGravity;
                rigidbody.isKinematic = isKinematic;
                rigidbody.detectCollisions = detectCollisions;
                rigidbody.collisionDetectionMode = collisionDetectionMode;
                rigidbody.interpolation = interpolation;
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private readonly struct ColliderState
        {
            private readonly Collider collider;
            private readonly bool enabled;

            public ColliderState(Collider collider)
            {
                this.collider = collider;
                enabled = collider.enabled;
            }

            public void Restore()
            {
                if (collider == null)
                {
                    return;
                }

                collider.enabled = enabled;
            }
        }
    }
}