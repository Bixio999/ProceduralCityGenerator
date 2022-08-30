using UnityEngine;

/*
 * Models an action for a rule. 
 * 
 * An action has a certain probability to be executed, based on the number of
 * actions defined on its rule: if a rule only has an action assigned, the 
 * probability msut be 1; otherwise it must be a value in (0,1) range.
 * 
 * Each action has a set of items representing the events to run.
 */

[CreateAssetMenu(menuName = "PBG/RuleAction")]
public class RuleAction : ScriptableObject {

    [SerializeField]
    public RuleActionItem[] items; // List of items

    [SerializeField] [Range(0,1)]
    public float probability = 1f; // Probability to be fired

    /*
     * Fire the action by executing its items.
     */
    public void Fire(Shape target) {
        foreach (RuleActionItem item in items) {
            item.Run(target);
        }
    }
}