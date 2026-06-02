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
        private struct IngredientPrefabEntry
        {
            public IngredientType Type;
            public GameObject Prefab;
        }

        [Header("Hand Object")]
        [SerializeField] private Transform handAnchor;
        [SerializeField] private bool hideWhenEmpty = true;

        [Header("Prefabs")]
        [SerializeField] private List<IngredientPrefabEntry> ingredientPrefabs =
            new List<IngredientPrefabEntry>();

        public IngredientType CurrentIngredient { get; private set; } = IngredientType.None;

        private readonly Dictionary<IngredientType, GameObject> prefabLookup =
            new Dictionary<IngredientType, GameObject>();
        private GameObject currentInstance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildPrefabLookup();
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

            return prefabLookup.TryGetValue(CurrentIngredient, out prefab)
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

        private void BuildPrefabLookup()
        {
            prefabLookup.Clear();
            foreach (var entry in ingredientPrefabs)
            {
                if (entry.Type == IngredientType.None || entry.Prefab == null)
                {
                    continue;
                }

                prefabLookup[entry.Type] = entry.Prefab;
            }
        }

        private void UpdateHandVisual()
        {
            if (handAnchor == null)
            {
                return;
            }

            if (CurrentIngredient == IngredientType.None)
            {
                if (hideWhenEmpty)
                {
                    ClearInstance();
                }

                return;
            }

            if (prefabLookup.TryGetValue(CurrentIngredient, out var prefab))
            {
                ReplaceInstance(prefab);
            }
        }

        private void ReplaceInstance(GameObject prefab)
        {
            if (currentInstance != null && currentInstance.name.StartsWith(prefab.name, StringComparison.Ordinal))
            {
                return;
            }

            ClearInstance();
            currentInstance = Instantiate(prefab, handAnchor);
            currentInstance.transform.localPosition = Vector3.zero;
            currentInstance.transform.localRotation = Quaternion.identity;
        }

        private void ClearInstance()
        {
            if (currentInstance == null)
            {
                return;
            }

            Destroy(currentInstance);
            currentInstance = null;
        }
    }
}
