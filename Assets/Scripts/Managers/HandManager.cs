using System;
using System.Collections.Generic;
using PizzaGame.Models;
using UnityEngine;
using UnityEngine.UI;

namespace PizzaGame.Managers
{
    public class HandManager : MonoBehaviour
    {
        public static HandManager Instance { get; private set; }

        [Serializable]
        private struct IngredientSpriteEntry
        {
            public IngredientType Type;
            public Sprite Sprite;
        }

        [Header("UI Hand")]
        [SerializeField] private Image handImage;

        [Header("Sprites")]
        [SerializeField] private List<IngredientSpriteEntry> ingredientSprites =
            new List<IngredientSpriteEntry>();

        public IngredientType CurrentIngredient { get; private set; } = IngredientType.None;

        private readonly Dictionary<IngredientType, Sprite> spriteLookup =
            new Dictionary<IngredientType, Sprite>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildSpriteLookup();
            UpdateHandVisual();
        }

        private void Update()
        {
            HandleNumberInput();
        }

        public void SetIngredient(IngredientType type)
        {
            CurrentIngredient = type;
            UpdateHandVisual();
        }

        public void ClearHand()
        {
            SetIngredient(IngredientType.None);
        }

        public bool TryGetCurrentSprite(out Sprite sprite)
        {
            if (CurrentIngredient == IngredientType.None)
            {
                sprite = null;
                return false;
            }

            return spriteLookup.TryGetValue(CurrentIngredient, out sprite)
                && sprite != null;
        }

        private void HandleNumberInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                ClearHand();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetIngredient(IngredientType.Cheese);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetIngredient(IngredientType.Bacon);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetIngredient(IngredientType.Spinach);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetIngredient(IngredientType.Salami);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetIngredient(IngredientType.Pineapple);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                SetIngredient(IngredientType.Mushroom);
            }
        }

        private void BuildSpriteLookup()
        {
            spriteLookup.Clear();
            foreach (var entry in ingredientSprites)
            {
                if (entry.Type == IngredientType.None || entry.Sprite == null)
                {
                    continue;
                }

                spriteLookup[entry.Type] = entry.Sprite;
            }
        }

        private void UpdateHandVisual()
        {
            if (handImage == null)
            {
                return;
            }

            if (CurrentIngredient == IngredientType.None)
            {
                handImage.gameObject.SetActive(false);
                handImage.sprite = null;
                return;
            }

            if (spriteLookup.TryGetValue(CurrentIngredient, out var sprite))
            {
                handImage.sprite = sprite;
                handImage.gameObject.SetActive(true);
            }
            else
            {
                handImage.gameObject.SetActive(false);
            }
        }
    }
}
