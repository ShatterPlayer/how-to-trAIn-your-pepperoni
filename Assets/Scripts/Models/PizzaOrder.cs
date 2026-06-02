using System.Collections.Generic;

namespace PizzaGame.Models
{
    [System.Serializable]
    public class PizzaOrder
    {
        public string OrderId;
        public List<IngredientRequirement> Ingredients = new List<IngredientRequirement>();
    }
}
