using System.Collections.Generic;
using PizzaGame.Models;
using UnityEngine;

namespace PizzaGame.Managers
{
    public class PlacedIngredientData
    {
        public IngredientType Type;
        public Vector2 Position;
        public GameObject VisualObject;
    }

    public class PizzaManager : MonoBehaviour
    {
        public static PizzaManager Instance { get; private set; }

        private readonly List<PlacedIngredientData> placedIngredients =
            new List<PlacedIngredientData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterPlacement(IngredientType type, Vector2 position, GameObject visualObject)
        {
            placedIngredients.Add(new PlacedIngredientData
            {
                Type = type,
                Position = position,
                VisualObject = visualObject
            });
        }

        public IReadOnlyList<PlacedIngredientData> GetPlacements()
        {
            return placedIngredients;
        }

        public void ClearAll()
        {
            foreach (var p in placedIngredients)
            {
                if (p.VisualObject != null)
                {
                    Destroy(p.VisualObject);
                }
            }
            placedIngredients.Clear();
        }
    }
}
