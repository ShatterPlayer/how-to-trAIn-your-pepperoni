using System;
using System.Collections.Generic;
using PizzaGame.Infrastructure;
using PizzaGame.Models;
using UnityEngine;

namespace PizzaGame.Managers
{
    public class OrderManager : SingletonMono<OrderManager>
    {
        [SerializeField] private List<PizzaOrder> seededOrders = new List<PizzaOrder>();

        [Header("Random Orders")]
        [SerializeField] private int minIngredients = 2;
        [SerializeField] private int maxIngredients = 4;
        [SerializeField] private float minIngredientRadius = 0.1f;
        [SerializeField] private float maxIngredientRadius = 0.45f;
        [SerializeField] private float minIngredientSpacing = 0.3f;

        [Header("Timing")]
        [SerializeField] private float maxOrderDuration = 20f;

        private readonly List<PizzaOrder> orderPool = new List<PizzaOrder>();
        private int orderCounter = 1;
        private float orderTimer;
        private CustomerAgent currentCustomer;

        public PizzaOrder CurrentOrder { get; private set; }
        public CustomerAgent CurrentCustomer { get; private set; }

        public event Action<PizzaOrder> OnOrderStarted;
        public event Action<PizzaOrder> OnOrderCompleted;

        private void Start()
        {
            foreach (var order in seededOrders)
            {
                orderPool.Add(order);
            }
        }

        private void Update()
        {
            if (CurrentOrder == null || maxOrderDuration <= 0f)
            {
                return;
            }

            orderTimer += Time.deltaTime;
            if (orderTimer >= maxOrderDuration)
            {
                TimeoutCurrentOrder();
            }
        }

        public void EnqueueOrder(PizzaOrder order)
        {
            if (order == null)
            {
                return;
            }

            orderPool.Add(order);
        }

        public bool TryStartNextOrder()
        {
            if (CurrentOrder != null)
            {
                return false;
            }

            PizzaOrder order = null;
            if (orderPool.Count > 0)
            {
                order = orderPool[0];
                orderPool.RemoveAt(0);
            }
            else
            {
                order = CreateRandomOrder();
            }

            StartOrder(order);
            return true;
        }

        public bool TryStartNextOrder(CustomerAgent customer)
        {
            currentCustomer = customer;
            if (!TryStartNextOrder())
            {
                currentCustomer = null;
                return false;
            }
            return true;
        }

        public void CompleteCurrentOrder()
        {
            var completed = CurrentOrder;
            CurrentOrder = null;
            CurrentCustomer = null;
            currentCustomer = null;
            orderTimer = 0f;
            if (completed != null)
            {
                OnOrderCompleted?.Invoke(completed);
            }
        }

        private void StartOrder(PizzaOrder order)
        {
            if (order == null)
            {
                return;
            }

            CurrentOrder = order;
            CurrentCustomer = currentCustomer;
            orderTimer = 0f;
            if (string.IsNullOrWhiteSpace(CurrentOrder.OrderId))
            {
                CurrentOrder.OrderId = $"Order_{orderCounter}";
            }

            orderCounter += 1;
            OnOrderStarted?.Invoke(CurrentOrder);
        }

        private void TimeoutCurrentOrder()
        {
            var timedOutCustomer = currentCustomer;
            CompleteCurrentOrder();
            if (timedOutCustomer != null)
            {
                timedOutCustomer.OnOrderTimedOut();
            }
        }

        public PizzaOrder CreateRandomOrder()
        {
            var order = new PizzaOrder();
            var ingredientCount = Mathf.Clamp(
                UnityEngine.Random.Range(minIngredients, maxIngredients + 1),
                1,
                12
            );

            for (var i = 0; i < ingredientCount; i += 1)
            {
                var type = GetRandomIngredientType();
                var position = GenerateNonOverlappingPosition(order);
                order.Ingredients.Add(new IngredientRequirement
                {
                    Type = type,
                    Position = position
                });
            }

            return order;
        }

        private IngredientType GetRandomIngredientType()
        {
            var types = (IngredientType[])System.Enum.GetValues(typeof(IngredientType));
            if (types.Length <= 1)
            {
                return IngredientType.Bacon;
            }

            var pick = UnityEngine.Random.Range(1, types.Length);
            return types[pick];
        }

        private Vector2 GenerateNonOverlappingPosition(PizzaOrder order)
        {
            var maxAttempts = 30;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var pos = UnityEngine.Random.insideUnitCircle
                    * UnityEngine.Random.Range(minIngredientRadius, maxIngredientRadius);
                var valid = true;

                foreach (var existing in order.Ingredients)
                {
                    if (Vector2.Distance(pos, existing.Position) < minIngredientSpacing)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    return pos;
                }
            }

            return UnityEngine.Random.insideUnitCircle * maxIngredientRadius;
        }
    }
}
