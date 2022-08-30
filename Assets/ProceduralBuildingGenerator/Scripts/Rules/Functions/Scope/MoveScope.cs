using UnityEngine;

/*
 * Function used to move the scope's position of a shape. 
 * 
 * If a value of the new position is set to "relative", use it as scaling factor
 * of the current scope value. 
 */

[CreateAssetMenu(menuName = "PBG/Functions/Scope/MoveScope")]
public class MoveScope : RuleFunction {

    [SerializeField]
    public Value[] newPosition = new Value[3]; // Values of the move vector to apply

    /*
     * Modify the position of target's scope with the selected values.
     */
    public override void Execute(Shape target) {
        // Get the target scope
        Scope scope = target.scope;

        // Get the values 
        float[] computedValues = new float[newPosition.Length];
        for (int i = 0; i < newPosition.Length; i++)
        {
            Value value = newPosition[i];
            float v = value.Compute();
            computedValues[i] = v;
        }

        // Calculate the move vector
        Vector3 values = Vector3.zero;

        if (newPosition[0].isRelative)
            values.x = computedValues[0] * scope.position.x;
        else
            values.x = computedValues[0];

        if (newPosition[1].isRelative)
            values.y = computedValues[1] * scope.position.y;
        else
            values.y = computedValues[1];

        if (newPosition[2].isRelative)
            values.z = computedValues[2] * scope.position.z;
        else
            values.z = computedValues[2];

        // Apply scope movement
        scope.position += values;
        return;
    }
    
}