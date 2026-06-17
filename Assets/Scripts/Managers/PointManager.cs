using System;
using System.Collections;
using System.Collections.Generic;
using PizzaGame.Infrastructure;
using PizzaGame.Models;
using TMPro;
using UnityEngine;

namespace PizzaGame.Managers
{
    public class ScoringResult
    {
        public int totalRequirements;
        public int totalPlaced;
        public int correctPlacements;
        public int missingIngredients;
        public int wrongPlacements;
        public int pointsEarned;
        public bool perfectBonus;
    }

    public class PointManager : SingletonMono<PointManager>
    {
        [Header("Scoring")]
        [SerializeField] private float maxScoringDistance = 0.5f;
        [SerializeField] private int maxPointsPerIngredient = 100;
        [SerializeField] private float perfectBonusMultiplier = 1.5f;
        [SerializeField] private int missingIngredientPenalty = 30;
        [SerializeField] private int wrongIngredientPenalty = 20;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private float resultDisplayDuration = 3f;

        private int totalScore;
        private Coroutine resultCoroutine;

        public int TotalScore => totalScore;

        public event Action<int> OnScoreChanged;
        public event Action<ScoringResult> OnScoringCompleted;

        private void Start()
        {
            if (scoreText == null || resultText == null)
            {
                CreateDefaultUI();
            }
            UpdateScoreUI();
        }

        public void EvaluateAndScore()
        {
            if (OrderManager.Instance == null || OrderManager.Instance.CurrentOrder == null)
            {
                return;
            }

            var order = OrderManager.Instance.CurrentOrder;
            var placed = PizzaManager.Instance != null
                ? PizzaManager.Instance.GetPlacements()
                : new List<PlacedIngredientData>();

            var result = EvaluatePlacement(order, placed);
            ApplyScore(result);

            if (PizzaManager.Instance != null)
            {
                PizzaManager.Instance.ClearAll();
            }

            var customer = OrderManager.Instance.CurrentCustomer;
            OrderManager.Instance.CompleteCurrentOrder();
            if (customer != null)
            {
                customer.CompleteOrderAndLeave();
            }

            OnScoringCompleted?.Invoke(result);

            if (resultText != null)
            {
                ShowResult(result);
            }
        }

        private ScoringResult EvaluatePlacement(PizzaOrder order, IReadOnlyList<PlacedIngredientData> placed)
        {
            var result = new ScoringResult();
            var usedIngredients = new HashSet<int>();

            for (int i = 0; i < order.Ingredients.Count; i++)
            {
                var req = order.Ingredients[i];
                int bestIdx = -1;
                float bestDist = float.MaxValue;

                for (int j = 0; j < placed.Count; j++)
                {
                    if (usedIngredients.Contains(j)) continue;
                    if (placed[j].Type != req.Type) continue;

                    var dist = Vector2.Distance(req.Position, placed[j].Position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = j;
                    }
                }

                if (bestIdx >= 0)
                {
                    usedIngredients.Add(bestIdx);
                    result.correctPlacements++;
                }
            }

            result.missingIngredients = order.Ingredients.Count - result.correctPlacements;
            result.wrongPlacements = placed.Count - usedIngredients.Count;
            result.totalRequirements = order.Ingredients.Count;
            result.totalPlaced = placed.Count;

            return result;
        }

        private void ApplyScore(ScoringResult result)
        {
            float earned = 0;

            var order = OrderManager.Instance.CurrentOrder;
            var placed = PizzaManager.Instance != null
                ? PizzaManager.Instance.GetPlacements()
                : new List<PlacedIngredientData>();

            var usedIngredients = new HashSet<int>();

            for (int i = 0; i < order.Ingredients.Count; i++)
            {
                var req = order.Ingredients[i];
                int bestIdx = -1;
                float bestDist = float.MaxValue;

                for (int j = 0; j < placed.Count; j++)
                {
                    if (usedIngredients.Contains(j)) continue;
                    if (placed[j].Type != req.Type) continue;

                    var dist = Vector2.Distance(req.Position, placed[j].Position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = j;
                    }
                }

                if (bestIdx >= 0)
                {
                    usedIngredients.Add(bestIdx);
                    var factor = 1f - Mathf.Clamp01(bestDist / maxScoringDistance);
                    earned += maxPointsPerIngredient * factor;
                }
                else
                {
                    earned -= missingIngredientPenalty;
                }
            }

            earned -= result.wrongPlacements * wrongIngredientPenalty;

            if (result.correctPlacements == result.totalRequirements && result.totalRequirements > 0)
            {
                earned *= perfectBonusMultiplier;
                result.perfectBonus = true;
            }

            result.pointsEarned = Mathf.Max(0, Mathf.RoundToInt(earned));
            totalScore += result.pointsEarned;
            OnScoreChanged?.Invoke(totalScore);
            UpdateScoreUI();
        }

        private void ShowResult(ScoringResult result)
        {
            if (resultCoroutine != null)
            {
                StopCoroutine(resultCoroutine);
            }
            resultCoroutine = StartCoroutine(DisplayResult(result));
        }

        private IEnumerator DisplayResult(ScoringResult result)
        {
            string text = $"Punkty: +{result.pointsEarned}";
            if (result.perfectBonus)
            {
                text += "\n<b>PERFEKCYJNIE! Bonus x1.5!</b>";
            }
            text += $"\nPoprawne: {result.correctPlacements} | Brakujące: {result.missingIngredients} | Złe: {result.wrongPlacements}";
            resultText.text = text;
            resultText.gameObject.SetActive(true);

            yield return new WaitForSeconds(resultDisplayDuration);
            resultText.gameObject.SetActive(false);
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Wynik: {totalScore}";
            }
        }

        private void CreateDefaultUI()
        {
            var canvasGO = new GameObject("ScoreCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var scoreGO = new GameObject("ScoreText");
            scoreGO.transform.SetParent(canvasGO.transform, false);
            var scoreTmp = scoreGO.AddComponent<TextMeshProUGUI>();
            scoreTmp.fontSize = 36;
            scoreTmp.alignment = TextAlignmentOptions.TopLeft;
            scoreTmp.color = Color.white;
            scoreTmp.text = "Wynik: 0";
            var scoreRect = scoreTmp.rectTransform;
            scoreRect.anchorMin = new Vector2(0, 1);
            scoreRect.anchorMax = new Vector2(0, 1);
            scoreRect.pivot = new Vector2(0, 1);
            scoreRect.anchoredPosition = new Vector2(20, -20);

            var resultGO = new GameObject("ResultText");
            resultGO.transform.SetParent(canvasGO.transform, false);
            var resultTmp = resultGO.AddComponent<TextMeshProUGUI>();
            resultTmp.fontSize = 48;
            resultTmp.alignment = TextAlignmentOptions.Center;
            resultTmp.color = Color.yellow;
            resultTmp.text = "";
            var resultRect = resultTmp.rectTransform;
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = Vector2.zero;
            resultGO.SetActive(false);

            scoreText = scoreTmp;
            resultText = resultTmp;
        }
    }
}
