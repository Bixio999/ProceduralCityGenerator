using UnityEngine;

/*
 * Function used to change the scope's scale of a shape.
 * 
 * If a value of the new position is set to "relative", use it as scaling factor
 * of the current scope value. 
 */

[CreateAssetMenu(menuName = "PBG/Functions/Scope/ScaleScope")]
public class ScaleScope : RuleFunction {

    [SerializeField]
    public Value[] newSize = new Value[3]; // Values of the new scale vector

    /*
     * Modify the scale of target's scope with the selected values.
     */
    public override void Execute(Shape target) {
        // Get the target scope
        Scope scope = target.scope;

        // Get the values 
        float[] computedValues = new float[newSize.Length];
        for (int i = 0; i < newSize.Length; i++)
        {
            Value value = newSize[i];
            float v = value.Compute();
            computedValues[i] = v;
        }

        // Calculate the scale vector
        Vector3 values = Vector3.zero;

        if (newSize[0].isRelative)
            values.x = computedValues[0] * scope.size.x;
        else
            values.x = computedValues[0];

        if (newSize[1].isRelative)
            values.y = computedValues[1] * scope.size.y;
        else
            values.y = computedValues[1];

        if (newSize[2].isRelative)
            values.z = computedValues[2] * scope.size.z;
        else
            values.z = computedValues[2];

        // Update the scope size
        scope.size = values;
    }
    
}