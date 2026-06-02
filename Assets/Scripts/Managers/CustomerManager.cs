using System;
using System.Collections;
using PizzaGame.Infrastructure;
using UnityEngine;

namespace PizzaGame.Managers
{
    public class CustomerManager : SingletonMono<CustomerManager>
    {
        [Header("References")]
        [SerializeField] private CounterSpot counterSpot;
        [SerializeField] private CustomerAgent customerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;

        [Header("Spawning")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private float minSpawnInterval = 4f;
        [SerializeField] private float maxSpawnInterval = 8f;
        [SerializeField] private int maxActiveCustomers = 5;

        [Header("Queue")]
        [SerializeField] private float queueSpacing = 0.8f;
        [SerializeField] private Vector3 queueDirection = Vector3.back;

        private readonly System.Collections.Generic.List<CustomerAgent> queue =
            new System.Collections.Generic.List<CustomerAgent>();
        private Coroutine spawnLoop;
        private int activeCustomers;

        public CounterSpot CounterSpot => counterSpot;

        public event Action<CustomerAgent> OnCustomerWaiting;
        public event Action<CustomerAgent> OnCustomerOrdering;
        public event Action<CustomerAgent> OnCustomerDone;

        private void Start()
        {
            if (spawnOnStart)
            {
                spawnLoop = StartCoroutine(SpawnLoop());
            }
        }

        private void OnDisable()
        {
            if (spawnLoop != null)
            {
                StopCoroutine(spawnLoop);
                spawnLoop = null;
            }
        }

        public void RegisterCustomer(CustomerAgent customer)
        {
            if (customer == null || queue.Contains(customer))
            {
                return;
            }

            queue.Add(customer);
        }

        public void RemoveCustomer(CustomerAgent customer)
        {
            if (customer == null)
            {
                return;
            }

            queue.Remove(customer);
        }

        public bool IsFirstInQueue(CustomerAgent customer)
        {
            return queue.Count > 0 && queue[0] == customer;
        }

        public Vector3 GetQueuePosition(CustomerAgent customer)
        {
            if (counterSpot == null || counterSpot.WaitPoint == null)
            {
                return customer != null ? customer.transform.position : Vector3.zero;
            }

            var index = queue.IndexOf(customer);
            if (index < 0)
            {
                index = queue.Count;
            }

            var direction = GetQueueDirection();

            return counterSpot.WaitPoint.position + direction * (queueSpacing * index);
        }

        private Vector3 GetQueueDirection()
        {
            if (spawnPoint != null && counterSpot != null && counterSpot.WaitPoint != null)
            {
                var toSpawn = spawnPoint.position - counterSpot.WaitPoint.position;
                if (toSpawn.sqrMagnitude > 0.001f)
                {
                    return toSpawn.normalized;
                }
            }

            if (queueDirection.sqrMagnitude > 0.001f)
            {
                return queueDirection.normalized;
            }

            return counterSpot != null && counterSpot.WaitPoint != null
                ? -counterSpot.WaitPoint.forward
                : Vector3.back;
        }

        public void SetCounterSpot(CounterSpot counter)
        {
            counterSpot = counter;
        }

        public void StartSpawning()
        {
            if (spawnLoop == null)
            {
                spawnLoop = StartCoroutine(SpawnLoop());
            }
        }

        public void StopSpawning()
        {
            if (spawnLoop != null)
            {
                StopCoroutine(spawnLoop);
                spawnLoop = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                var delay = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
                yield return new WaitForSeconds(delay);
                if (activeCustomers < maxActiveCustomers)
                {
                    SpawnCustomer();
                }
            }
        }

        public void SpawnCustomer()
        {
            if (customerPrefab == null || spawnPoint == null)
            {
                return;
            }

            var instance = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            instance.SetCounterSpot(counterSpot);
            instance.SetExitPoint(exitPoint != null ? exitPoint : spawnPoint);
            instance.OnDespawned += HandleCustomerDespawned;
            activeCustomers += 1;
        }

        private void HandleCustomerDespawned(CustomerAgent customer)
        {
            if (customer != null)
            {
                customer.OnDespawned -= HandleCustomerDespawned;
            }

            activeCustomers = Mathf.Max(0, activeCustomers - 1);
        }

        public void NotifyCustomerWaiting(CustomerAgent customer)
        {
            OnCustomerWaiting?.Invoke(customer);
        }

        public void NotifyCustomerOrdering(CustomerAgent customer)
        {
            OnCustomerOrdering?.Invoke(customer);
        }

        public void NotifyCustomerDone(CustomerAgent customer)
        {
            OnCustomerDone?.Invoke(customer);
        }
    }
}
