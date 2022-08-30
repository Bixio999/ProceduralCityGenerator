using UnityEngine;

/*
 * Rule query used to check if a shape is in the front facade of the building.
 */

[CreateAssetMenu(menuName = "PBG/Conditions/Visible")]
public class Visible : RuleQuery {

    [SerializeField]
    public string target;
    
    public override object Compute(Shape target)
    {
        return Vector3.Dot((target.GetQuaternion() * Vector3.forward).normalized, Vector3.forward) == 1 && target.scope.position.z == 0 && target.scope.size.z == 0;
    }
}