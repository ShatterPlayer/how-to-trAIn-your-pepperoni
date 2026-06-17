using UnityEngine;

namespace PizzaGame.Managers
{
    public class Bell : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactionKey = KeyCode.F;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask bellLayerMask = -1;

        private Transform playerCamera;

        private void Awake()
        {
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.radius = 0.3f;
            }

            if (GetComponent<MeshRenderer>() == null)
            {
                CreateDefaultVisual();
            }
        }

        private void Start()
        {
            playerCamera = Camera.main?.transform;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryRingByRaycast();
            }

            if (Input.GetKeyDown(interactionKey))
            {
                TryRingByProximity();
            }
        }

        private void TryRingByRaycast()
        {
            if (playerCamera == null) return;

            var ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out var hit, interactionRange, bellLayerMask))
            {
                if (hit.collider.gameObject == gameObject ||
                    hit.collider.transform.IsChildOf(transform))
                {
                    Ring();
                }
            }
        }

        private void TryRingByProximity()
        {
            if (playerCamera == null) return;

            var dist = Vector3.Distance(transform.position, playerCamera.position);
            if (dist <= interactionRange)
            {
                Ring();
            }
        }

        public void Ring()
        {
            if (PointManager.Instance != null)
            {
                PointManager.Instance.EvaluateAndScore();
            }
        }

        private void CreateDefaultVisual()
        {
            var bellBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bellBase.transform.SetParent(transform, false);
            bellBase.transform.localPosition = new Vector3(0, 0.3f, 0);
            bellBase.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
            bellBase.GetComponent<Collider>().enabled = false;

            var bellTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bellTop.transform.SetParent(transform, false);
            bellTop.transform.localPosition = new Vector3(0, 0.6f, 0);
            bellTop.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            bellTop.GetComponent<Collider>().enabled = false;

            var bellHammer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bellHammer.transform.SetParent(transform, false);
            bellHammer.transform.localPosition = new Vector3(0.3f, 0.35f, 0);
            bellHammer.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            bellHammer.GetComponent<Collider>().enabled = false;
        }
    }
}
