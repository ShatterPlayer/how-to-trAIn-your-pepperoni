using UnityEngine;
using UnityEngine.UI;

namespace PizzaGame.Managers
{
    public class PizzaIngredientPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform pizzaRoot;
        [SerializeField] private RectTransform placementRoot;

        [Header("Placement")]
        [SerializeField] private LayerMask pizzaLayerMask = ~0;
        [SerializeField] private float rayLength = 50f;
        [SerializeField] private float ingredientSpriteSize = 0.08f;

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
                placementRoot = GetComponent<RectTransform>();
            }
        }

        private void Update()
        {
            if (HandManager.Instance == null || playerCamera == null)
            {
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

            if (!HandManager.Instance.TryGetCurrentSprite(out var sprite))
            {
                return;
            }

            if (!Physics.Raycast(ray, out var hit, rayLength, pizzaLayerMask))
            {
                return;
            }

            var ingredientGO = new GameObject($"Ingredient_{HandManager.Instance.CurrentIngredient}");
            ingredientGO.transform.SetParent(placementRoot, false);

            var img = ingredientGO.AddComponent<Image>();
            img.sprite = sprite;

            var rt = ingredientGO.GetComponent<RectTransform>();
            var localPos = placementRoot.InverseTransformPoint(hit.point);
            rt.anchoredPosition = new Vector2(localPos.x, localPos.y);
            rt.sizeDelta = new Vector2(ingredientSpriteSize, ingredientSpriteSize);

            previewInstance = ingredientGO;
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

            var localPos = placementRoot.InverseTransformPoint(hit.point);
            var rt = previewInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(localPos.x, localPos.y);
        }

        private void FinishPlacement()
        {
            if (previewInstance == null)
            {
                return;
            }

            var ingredientType = HandManager.Instance.CurrentIngredient;
            var worldPos = previewInstance.transform.position;
            var localPos = pizzaRoot.InverseTransformPoint(worldPos);
            var localPos2D = new Vector2(localPos.x, localPos.z);

            if (PizzaManager.Instance != null)
            {
                PizzaManager.Instance.RegisterPlacement(
                    ingredientType,
                    localPos2D,
                    previewInstance
                );
            }

            previewInstance = null;
            HandManager.Instance.ClearHand();
        }
    }
}
