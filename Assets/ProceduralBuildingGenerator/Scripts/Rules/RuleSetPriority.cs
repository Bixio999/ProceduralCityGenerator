using UnityEngine;

/*
 * Used to group grammar rules by priority. 
 * 
 * Priority is useful to group rules by generation phases, and to define an 
 * execution order for those rules that share the same symbol.
 */

[CreateAssetMenu(menuName = "PBG/RuleSetPriority")]
public class RuleSetPriority : ScriptableObject
{
    [SerializeField]
    public Rule[] rules; // Set of rules 
}
