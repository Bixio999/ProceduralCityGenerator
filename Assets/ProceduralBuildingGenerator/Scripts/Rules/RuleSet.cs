using UnityEngine;

/*
 * Models the rule set. 
 * 
 * Each ruleset is defined by a name, a set of rule priorities and the grammar
 * axiom. 
 * 
 * The rules are grouped by priorities, which allow to easily organize them and 
 * manage the evaluation order. The higher is the priority, the sooner these
 * rules are executed. The priority order also allows to set an execution order 
 * for rules with the same symbol.
 */

[CreateAssetMenu(menuName = "PBG/RuleSet")]
public class RuleSet : ScriptableObject
{
    [SerializeField]
    public Shape axiom; // Ruleset axiom; initial shape

    [SerializeField]
    public RuleSetPriority[] ruleSet; // set of rule priorities

    new public string name; // Ruleset name

    /*
     * Evaluate the given shape by executing the rule with the same symbol and 
     * satisfying its conditions.
     */
    public void Execute(Shape target)
    {
        foreach (RuleSetPriority priority in ruleSet)
        {
            foreach (Rule rule in priority.rules)
            {
                // Find a rule with same symbol
                if (target.symbol == rule.symbol)
                {
                    // If the shape mets its conditions, execute it
                    if (rule.CheckConditions(target))
                    {
                        rule.Execute(target);
                        return;
                    }
                }
            }
        }
    }
}

