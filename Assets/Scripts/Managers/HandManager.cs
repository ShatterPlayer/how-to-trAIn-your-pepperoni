using System;
using System.Collections.Generic;
using PizzaGame.Models;
using UnityEngine;

namespace PizzaGame.Managers
{
    public class HandManager : MonoBehaviour
    {
        public static HandManager Instance { get; private set; }

        [Serializable]
        private struct IngredientModelEntry
        {
            public IngredientType Type;
            public GameObject ModelPrefab;
        }

        [Header("3D Hand")]
        [SerializeField] private Transform handRoot;
        [SerializeField] private Transform attachmentPoint;

        [Header("Models")]
        [SerializeField] private List<IngredientModelEntry> ingredientModels =
            new List<IngredientModelEntry>();

        public IngredientType CurrentIngredient { get; private set; } = IngredientType.None;

        private readonly Dictionary<IngredientType, GameObject> modelLookup =
            new Dictionary<IngredientType, GameObject>();

        private GameObject currentHeldModel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildModelLookup();
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

        public bool TryGetCurrentPrefab(out GameObject prefab)
        {
            if (CurrentIngredient == IngredientType.None)
            {
                prefab = null;
                return false;
            }

            return modelLookup.TryGetValue(CurrentIngredient, out prefab)
                && prefab != null;
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
                SetIngredient(IngredientType.Bacon);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetIngredient(IngredientType.Spinach);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetIngredient(IngredientType.Salami);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetIngredient(IngredientType.Pineapple);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetIngredient(IngredientType.Mushroom);
            }
        }

        private void BuildModelLookup()
        {
            modelLookup.Clear();
            foreach (var entry in ingredientModels)
            {
                if (entry.Type == IngredientType.None || entry.ModelPrefab == null)
                {
                    continue;
                }

                modelLookup[entry.Type] = entry.ModelPrefab;
            }
        }

        private void UpdateHandVisual()
        {
            if (currentHeldModel != null)
            {
                Destroy(currentHeldModel);
                currentHeldModel = null;
            }

            if (handRoot != null)
            {
                handRoot.gameObject.SetActive(CurrentIngredient != IngredientType.None);
            }

            if (CurrentIngredient == IngredientType.None)
            {
                return;
            }

            if (modelLookup.TryGetValue(CurrentIngredient, out var prefab) && prefab != null)
            {
                var parent = attachmentPoint != null ? attachmentPoint : handRoot;
                if (parent == null) return;

                currentHeldModel = Instantiate(prefab, parent);
                currentHeldModel.transform.localPosition = Vector3.zero;
            }
        }
    }
}
