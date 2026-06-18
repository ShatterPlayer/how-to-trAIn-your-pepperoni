using System;
using UnityEngine;
using UnityEngine.AI;

namespace PizzaGame.Managers
{
    public class CustomerAgent : MonoBehaviour
    {
        public event Action<CustomerAgent> OnDespawned;
        public enum CustomerState
        {
            Idle = 0,
            Approaching = 1,
            WaitingInQueue = 2,
            Ordering = 3,
            WaitingForPizza = 4,
            Leaving = 5,
            Done = 6
        }

        [Header("References")]
        [SerializeField] private CounterSpot counterSpot;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private NavMeshAgent navAgent;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float cashierStopDistance = 0.2f;
        [SerializeField] private float exitStopDistance = 0.2f;

        [Header("Avoidance")]
        [SerializeField] private ObstacleAvoidanceType queueAvoidance = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        [SerializeField] private ObstacleAvoidanceType leavingAvoidance = ObstacleAvoidanceType.NoObstacleAvoidance;
        [SerializeField] private int queuePriority = 80;
        [SerializeField] private int leavingPriority = 10;

        public CustomerState CurrentState { get; private set; } = CustomerState.Idle;
        private void Awake()
        {
            if (navAgent == null)
            {
                navAgent = GetComponent<NavMeshAgent>();
            }

            if (navAgent != null)
            {
                navAgent.speed = moveSpeed;
            }
        }

        private void Start()
        {
            if (counterSpot == null && CustomerManager.Instance != null)
            {
                counterSpot = CustomerManager.Instance.CounterSpot;
            }

            if (counterSpot != null)
            {
                if (CustomerManager.Instance != null)
                {
                    CustomerManager.Instance.RegisterCustomer(this);
                }
                SetState(CustomerState.Approaching);
            }
        }

        private void Update()
        {
            if (counterSpot == null)
            {
                return;
            }

            switch (CurrentState)
            {
                case CustomerState.Approaching:
                    UpdateQueueDestination();
                    if (HasReachedDestination(cashierStopDistance))
                    {
                        SetState(CustomerState.WaitingInQueue);
                    }
                    break;
                case CustomerState.WaitingInQueue:
                    UpdateQueueDestination();
                    TryStartOrdering();
                    break;
                case CustomerState.Ordering:
                    SetState(CustomerState.WaitingForPizza);
                    break;
                case CustomerState.WaitingForPizza:
                    break;
                case CustomerState.Leaving:
                    UpdateExitDestination();
                    if (HasReachedDestination(exitStopDistance))
                    {
                        SetState(CustomerState.Done);
                    }
                    break;
            }
        }

        public void SetCounterSpot(CounterSpot counter)
        {
            counterSpot = counter;
        }

        public void SetExitPoint(Transform exit)
        {
            exitPoint = exit;
        }

        private void UpdateQueueDestination()
        {
            if (navAgent == null)
            {
                return;
            }

            ApplyQueueAvoidance();
            navAgent.stoppingDistance = cashierStopDistance;
            var target = GetQueueTarget();
            navAgent.isStopped = false;
            navAgent.SetDestination(target);
        }

        private void TryStartOrdering()
        {
            if (CustomerManager.Instance != null
                && !CustomerManager.Instance.IsFirstInQueue(this))
            {
                return;
            }

            if (OrderManager.Instance == null || OrderManager.Instance.CurrentOrder != null)
            {
                return;
            }

            if (OrderManager.Instance.TryStartNextOrder(this))
            {
                SetState(CustomerState.Ordering);
            }
        }

        private Vector3 GetQueueTarget()
        {
            if (CustomerManager.Instance != null)
            {
                return CustomerManager.Instance.GetQueuePosition(this);
            }

            return counterSpot.WaitPoint != null
                ? counterSpot.WaitPoint.position
                : counterSpot.transform.position;
        }

        private void UpdateExitDestination()
        {
            if (navAgent == null)
            {
                return;
            }

            ApplyLeavingAvoidance();
            navAgent.stoppingDistance = exitStopDistance;
            var target = exitPoint != null ? exitPoint : counterSpot.transform;
            navAgent.isStopped = false;
            navAgent.SetDestination(target.position);
        }

        private bool HasReachedDestination(float stopDistance)
        {
            if (navAgent == null || navAgent.pathPending)
            {
                return false;
            }

            return navAgent.remainingDistance <= stopDistance + 0.05f;
        }

        private void ApplyQueueAvoidance()
        {
            if (navAgent == null)
            {
                return;
            }

            navAgent.obstacleAvoidanceType = queueAvoidance;
            navAgent.avoidancePriority = Mathf.Clamp(queuePriority, 0, 99);
        }

        private void ApplyLeavingAvoidance()
        {
            if (navAgent == null)
            {
                return;
            }

            navAgent.obstacleAvoidanceType = leavingAvoidance;
            navAgent.avoidancePriority = Mathf.Clamp(leavingPriority, 0, 99);
        }

        private void SetState(CustomerState newState)
        {
            if (newState == CurrentState)
            {
                return;
            }

            CurrentState = newState;

            if (CustomerManager.Instance != null)
            {
                switch (CurrentState)
                {
                    case CustomerState.WaitingInQueue:
                        CustomerManager.Instance.NotifyCustomerWaiting(this);
                        break;
                    case CustomerState.Ordering:
                        CustomerManager.Instance.NotifyCustomerOrdering(this);
                        break;
                    case CustomerState.Leaving:
                        CustomerManager.Instance.RemoveCustomer(this);
                        break;
                    case CustomerState.Done:
                        CustomerManager.Instance.NotifyCustomerDone(this);
                        break;
                }
            }

            if (CurrentState == CustomerState.Done)
            {
                if (navAgent != null)
                {
                    navAgent.isStopped = true;
                }
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.RemoveCustomer(this);
            }
            OnDespawned?.Invoke(this);
        }

        public void CompleteOrderAndLeave()
        {
            if (CurrentState != CustomerState.WaitingForPizza
                && CurrentState != CustomerState.Ordering)
            {
                return;
            }

            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.CompleteCurrentOrder();
            }

            if (PizzaGame.Infrastructure.AudioManager.Instance != null)
            {
                PizzaGame.Infrastructure.AudioManager.Instance.PlayYay();
            }

            SetState(CustomerState.Leaving);
        }

        public void OnOrderTimedOut()
        {
            if (CurrentState != CustomerState.WaitingForPizza
                && CurrentState != CustomerState.Ordering)
            {
                return;
            }

            if (PizzaGame.Infrastructure.AudioManager.Instance != null)
            {
                PizzaGame.Infrastructure.AudioManager.Instance.PlayFail();
            }

            SetState(CustomerState.Leaving);
        }
    }
}
