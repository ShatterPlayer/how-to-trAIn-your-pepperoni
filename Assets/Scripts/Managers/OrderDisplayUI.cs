using System;
using System.Collections.Generic;
using PizzaGame.Models;
using UnityEngine;
using UnityEngine.UI;

namespace PizzaGame.Managers
{
    public class OrderDisplayUI : MonoBehaviour
    {
        [Serializable]
        private struct IngredientSpriteEntry
        {
            public IngredientType Type;
            public Sprite Sprite;
        }

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform pizzaMarkerRoot;
        [SerializeField] private List<IngredientSpriteEntry> ingredientSprites =
            new List<IngredientSpriteEntry>();

        [Header("Display")]
        [SerializeField] private float markerSize = 30f;

        private readonly Dictionary<IngredientType, Sprite> spriteLookup =
            new Dictionary<IngredientType, Sprite>();
        private readonly List<GameObject> markers = new List<GameObject>();
        private PizzaOrder currentOrder;

        private void Awake()
        {
            spriteLookup.Clear();
            foreach (var entry in ingredientSprites)
            {
                if (entry.Type != IngredientType.None && entry.Sprite != null)
                {
                    spriteLookup[entry.Type] = entry.Sprite;
                }
            }
        }

        private void Start()
        {
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnOrderStarted += OnOrderStarted;
                OrderManager.Instance.OnOrderCompleted += OnOrderCompleted;
            }

            if (OrderManager.Instance != null && OrderManager.Instance.CurrentOrder != null)
            {
                OnOrderStarted(OrderManager.Instance.CurrentOrder);
            }
        }

        private void OnDestroy()
        {
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnOrderStarted -= OnOrderStarted;
                OrderManager.Instance.OnOrderCompleted -= OnOrderCompleted;
            }
        }

        private void OnOrderStarted(PizzaOrder order)
        {
            currentOrder = order;
            UpdateDisplay();
        }

        private void OnOrderCompleted(PizzaOrder order)
        {
            if (order == currentOrder)
            {
                currentOrder = null;
                ClearDisplay();
            }
        }

        public void UpdateDisplay()
        {
            ClearMarkers();

            if (currentOrder == null)
            {
                if (panelRoot != null) panelRoot.SetActive(false);
                return;
            }

            if (panelRoot != null) panelRoot.SetActive(true);

            if (pizzaMarkerRoot == null) return;

            foreach (var req in currentOrder.Ingredients)
            {
                CreateMarker(req);
            }
        }

        public void ClearDisplay()
        {
            ClearMarkers();
            if (panelRoot != null) panelRoot.SetActive(false);
            currentOrder = null;
        }

        private void ClearMarkers()
        {
            foreach (var m in markers)
            {
                Destroy(m);
            }
            markers.Clear();
        }

        private void CreateMarker(IngredientRequirement req)
        {
            if (!spriteLookup.TryGetValue(req.Type, out var sprite))
            {
                return;
            }

            var marker = new GameObject($"Marker_{req.Type}");
            marker.transform.SetParent(pizzaMarkerRoot, false);

            var img = marker.AddComponent<Image>();
            img.sprite = sprite;
            var rect = img.rectTransform;
            rect.sizeDelta = new Vector2(markerSize, markerSize);
            rect.anchoredPosition = MapPosition(req.Position);

            markers.Add(marker);
        }

        private Vector2 MapPosition(Vector2 ingredientPos)
        {
            var pivot = pizzaMarkerRoot.pivot;
            var half = pizzaMarkerRoot.rect.size * 0.5f;
            return new Vector2(ingredientPos.x * half.x, ingredientPos.y * half.y);
        }
    }
}
