using UnityEngine;

namespace Project.Managers
{
    public sealed class SceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private SceneReferenceProvider sceneReferences;

        private void Awake()
        {
            if (sceneReferences == null)
            {
                sceneReferences = GetComponent<SceneReferenceProvider>();
            }
        }

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError($"{nameof(SceneBootstrapper)} could not find a {nameof(GameManager)} instance.", this);
                return;
            }

            if (sceneReferences == null)
            {
                Debug.LogError($"{nameof(SceneBootstrapper)} is missing SceneReferenceProvider.", this);
                return;
            }

            GameManager.Instance.BindScene(sceneReferences);
        }
    }
}