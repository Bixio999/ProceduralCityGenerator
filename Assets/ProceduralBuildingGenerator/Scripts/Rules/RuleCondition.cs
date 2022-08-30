using UnityEngine;

/*
 * Models the condition handler of a rule.
 * 
 * A condition is defined by a rule query, an operator (condition type) and a 
 * value as second operand. 
 */

[CreateAssetMenu(menuName = "PBG/RuleCondition")]
public class RuleCondition : ScriptableObject {

    // Supported operators
    public enum ConditionType {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        True,
        False,
        Empty,
        NotEmpty
    }

    [SerializeField]
    public RuleQuery query; // Condition query

    [SerializeField]
    public ConditionType conditionType; // Condition operator

    [SerializeField]
    public Value value; // Condition operand

    /*
     * Test the condition on the given shape. 
     */
    public bool Evaluate(Shape target)
    {
        return conditionType switch
        {
            ConditionType.Equal => ((float)query.Compute(target) == value.Compute()),

            ConditionType.NotEqual => ((float)query.Compute(target) != value.Compute()),

            ConditionType.GreaterThan => ((float)query.Compute(target) > value.Compute()),

            ConditionType.LessThan => ((float)query.Compute(target) < value.Compute()),

            ConditionType.GreaterThanOrEqual => ((float)query.Compute(target) >= value.Compute()),

            ConditionType.LessThanOrEqual => ((float)query.Compute(target) <= value.Compute()),

            ConditionType.True => ((bool)query.Compute(target) == true),

            ConditionType.False => ((bool)query.Compute(target) == false),

            ConditionType.Empty => (query.Compute(target) == null),

            ConditionType.NotEmpty => (query.Compute(target) != null),

            _ => false,
        };
    }
}

/* ---------------------------- */

/*
 * Rule query superclass, used to edit objects from Unity editor.
 */
public abstract class RuleQuery : ScriptableObject {
    public abstract object Compute(Shape target);
}