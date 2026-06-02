using UnityEngine;

namespace PizzaGame.Managers
{
    public class PizzaIngredientPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform pizzaRoot;
        [SerializeField] private Transform placementRoot;

        [Header("Placement")]
        [SerializeField] private LayerMask pizzaLayerMask = ~0;
        [SerializeField] private float rayLength = 50f;
        [SerializeField] private float ingredientScale = 0.1f;

        private GameObject previewInstance;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (pizzaRoot == null)
            {
                pizzaRoot = transform;
            }

            if (placementRoot == null)
            {
                placementRoot = transform;
            }

        }

        private void Update()
        {
            if (HandManager.Instance == null || playerCamera == null)
            {
                Debug.Log("HandManager or playerCamera reference is missing, cannot place ingredient.");
                return;
            }

            var ray = playerCamera.ScreenPointToRay(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                TryStartPlacement(ray);
            }

            if (Input.GetMouseButton(0))
            {
                UpdatePlacement(ray);
            }

            if (Input.GetMouseButtonUp(0))
            {
                FinishPlacement();
            }
        }

        private void TryStartPlacement(Ray ray)
        {
            if (previewInstance != null)
            {
                return;
            }

            if (!HandManager.Instance.TryGetCurrentPrefab(out var prefab))
            {
                Debug.Log("No ingredient currently held, skipping placement.");
                return;
            }
            Debug.Log("Attempting to place ingredient...");

            if (!Physics.Raycast(ray, out var hit, rayLength, pizzaLayerMask))
            {
                Debug.Log("Raycast did not hit a valid pizza surface.");
                return;
            }

            var hitLayer = hit.collider.gameObject.layer;
            if ((pizzaLayerMask.value & (1 << hitLayer)) == 0)
            {
                Debug.Log("Hit object is not on the pizza layer, ignoring.");
                return;
            }

            Debug.Log($"Placing ingredient at {hit.point} with normal {hit.normal}");

            var rotation = Quaternion.identity;
            var position = hit.point;
            previewInstance = Instantiate(prefab, position, rotation);
            previewInstance.transform.SetParent(placementRoot, true);
            previewInstance.transform.position = position;
            previewInstance.transform.localScale = Vector3.one * ingredientScale;
        }

        private void UpdatePlacement(Ray ray)
        {
            if (previewInstance == null)
            {
                return;
            }

            if (!Physics.Raycast(ray, out var hit, rayLength, pizzaLayerMask))
            {
                return;
            }

            previewInstance.transform.position = hit.point;
        }

        private void FinishPlacement()
        {
            if (previewInstance == null)
            {
                return;
            }

            previewInstance = null;
            HandManager.Instance.ClearHand();
        }

    }
}
