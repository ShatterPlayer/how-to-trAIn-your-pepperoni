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

        [Header("Patience Bar")]
        [SerializeField] private Vector2 patienceBarSize = new Vector2(300f, 20f);
        [SerializeField] private Color patienceBarFullColor = Color.green;
        [SerializeField] private Color patienceBarEmptyColor = Color.red;

        private readonly Dictionary<IngredientType, Sprite> spriteLookup =
            new Dictionary<IngredientType, Sprite>();
        private readonly List<GameObject> markers = new List<GameObject>();
        private PizzaOrder currentOrder;

        private Image patienceFill;
        private RectTransform patienceFillRect;
        private GameObject patienceObj;
        private float patienceBarTotalWidth;

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
            CreatePatienceBar();

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

        private void Update()
        {
            if (patienceFill == null) return;

            if (currentOrder != null && OrderManager.Instance != null)
            {
                patienceObj.SetActive(true);
                var progress = OrderManager.Instance.OrderProgressNormalized;
                var fill = 1f - progress;
                patienceFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, patienceBarTotalWidth * fill);
                patienceFill.color = Color.Lerp(patienceBarEmptyColor, patienceBarFullColor, fill);
            }
            else
            {
                patienceObj.SetActive(false);
            }
        }

        private void CreatePatienceBar()
        {
            patienceObj = new GameObject("PatienceBar", typeof(CanvasRenderer));
            patienceObj.transform.SetParent(panelRoot != null ? panelRoot.transform : transform, false);

            var bg = patienceObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f);
            bg.type = Image.Type.Simple;
            var bgRect = bg.rectTransform;
            bgRect.anchorMin = new Vector2(0.5f, 0f);
            bgRect.anchorMax = new Vector2(0.5f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = patienceBarSize;
            bgRect.anchoredPosition = new Vector2(0f, -30f);
            patienceBarTotalWidth = patienceBarSize.x;

            var fillObj = new GameObject("Fill", typeof(CanvasRenderer));
            fillObj.transform.SetParent(patienceObj.transform, false);
            patienceFill = fillObj.AddComponent<Image>();
            patienceFill.color = patienceBarFullColor;
            patienceFillRect = patienceFill.rectTransform;
            patienceFillRect.anchorMin = new Vector2(0f, 0f);
            patienceFillRect.anchorMax = new Vector2(0f, 1f);
            patienceFillRect.pivot = new Vector2(0f, 0.5f);
            patienceFillRect.sizeDelta = new Vector2(patienceBarTotalWidth, patienceBarSize.y);
            patienceFillRect.anchoredPosition = Vector2.zero;

            patienceObj.SetActive(false);
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
