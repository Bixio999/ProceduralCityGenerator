using UnityEngine;

/*
 * Function used to apply a rotation to a shape by changing its scope 
 * property.
 */

[CreateAssetMenu(menuName = "PBG/Functions/Scope/RotateShape")]
public class RotateShape : RuleFunction {

    [SerializeField]
    public Value[] rotate = new Value[3]; // New rotation

    /*
     * Modify the rotation of target's scope with the selected values.
     */
    public override void Execute(Shape target) {
        if (target == null) 
            throw new System.ArgumentNullException("RotateShape: target is null.");

        // Calculate the new rotation vector
        Vector3 rotation = Vector3.zero;

        if (rotate[0] != null)
            rotation.x = rotate[0].Compute();
        if (rotate[1] != null)
            rotation.y = rotate[1].Compute();
        if (rotate[2] != null)
            rotation.z = rotate[2].Compute();

        // Update the rotation
        target.rotation = rotation;
    }
    
}