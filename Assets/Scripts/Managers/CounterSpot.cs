using UnityEngine;

namespace PizzaGame.Managers
{
    public class CounterSpot : MonoBehaviour
    {
        [Header("Points")]
        [SerializeField] private Transform waitPoint;
        [SerializeField] private Transform interactionPoint;

        public Transform WaitPoint => waitPoint;
        public Transform InteractionPoint => interactionPoint;
    }
}
