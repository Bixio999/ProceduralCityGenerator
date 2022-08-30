using UnityEngine;

/*
 * Models a rule of the ruleset grammar. 
 * 
 * Each rule is describe by a symbol, a set of conditions and a set of actions.
 * 
 * A rule must receive as input a shape with a symbol equal to its own. In 
 * addition, all conditions of the rule must be met for the rule to be executed. 
 * If it is valid, it fires (executes) one of the actions described and generates 
 * the child shapes.
 * 
 * A rule action contains a probability, and it must be set in case of multiple
 * actions in a single rule. This is used for stochastic rulesets. 
 * 
 * The action to fire is chosen randomly by following their probabilities.
 */

[CreateAssetMenu(menuName = "PBG/Rule")]
public class Rule : ScriptableObject
{
    [SerializeField]
    public string symbol; // Rule symbol

    [SerializeField]
    public RuleCondition[] conditions; // List of conditions

    [SerializeField]
    public RuleAction[] actions; // List of actions

    /*
     * Evaluate the rule for the input shape. 
     * 
     * Randomly choose an action to execute from the rule actions list.
     */
    public void Execute(Shape target)
    {
        // Check for empty rule
        if (conditions.Length + actions.Length == 0)
            return;

        // Check if the action probabilities are correctly defined
        float probCount = 0;
        foreach (RuleAction action in actions)
        {
            probCount += action.probability;
        }
        if (probCount == 0 || probCount > 1)
            throw new System.ArgumentException(this.name + ": probability sum of actions must be in the interval (0,1]");

        // Choose the action
        float probMin = 0, probMax = float.Epsilon;
        float probability = Random.value;
        foreach (RuleAction action in actions)
        {
            probMax += action.probability;
            if (probMin <= probability && probability < probMax)
            {
                action.Fire(target); // Fire the choosen action
                return;
            }
            probMin = probMax;
        }
    }

    /*
     * Evaluate the rule conditions.
     */
    public bool CheckConditions(Shape target)
    {
        foreach (RuleCondition condition in conditions)
        {
            if (!condition.Evaluate(target))
                return false;
        }
        return true;
    }
}