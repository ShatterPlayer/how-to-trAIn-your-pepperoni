using UnityEngine;
using UnityEngine.SceneManagement;
using PizzaGame.Managers;

namespace PizzaGame.UI
{
    public class EndGameTrigger : MonoBehaviour
    {
        [Header("Game End Settings")]
        [SerializeField] private int maxOrders = 5; 
        private int ordersCount = 0;

        public static int FinalScore { get; private set; }

        private void Start()
        {
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnOrderCompleted += CheckOrderCount;
            }
        }

        private void OnDestroy()
        {
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnOrderCompleted -= CheckOrderCount;
            }
        }

        private void CheckOrderCount(PizzaGame.Models.PizzaOrder order)
        {
            ordersCount++;

            if (ordersCount >= maxOrders)
            {
                if (PointManager.Instance != null)
                {
                    FinalScore = PointManager.Instance.TotalScore;
                }

                SceneManager.LoadScene("EndMenu");
            }
        }
    }
}