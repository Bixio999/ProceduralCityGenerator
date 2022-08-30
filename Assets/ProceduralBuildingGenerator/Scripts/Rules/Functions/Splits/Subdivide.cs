using UnityEngine;

/*
 * Rule function used to define new shapes along defined axes by specific 
 * given sizes.
 * 
 * This function supports nested rules, so the output symbols are actually 
 * action items that may contains other functions or only assign the symbol 
 * to the new shape.
 */

[CreateAssetMenu(menuName = "PBG/Functions/Splits/Subdivide")]
public class Subdivide : RuleFunction {

    [SerializeField]
    public Coords coord; // Axes to consider

    [SerializeField]
    public Value[] values; // Spacing to consider for each shape

    [SerializeField]
    public RuleActionItem[] symbols; // Action items 

    /*
     * Generate new shapes by positioning at given spacing along selected axes.
     */
    public override void Execute(Shape target) {
        // Check if parameters are correctly defined
        if (values.Length != symbols.Length)
            throw new System.ArgumentException(this.name + ": values and symbols must have the same length");

        // Get the target scope
        Scope scope = target.scope;

        // Get the values and setup for absolute and relative calculation
        float absSum = 0, relSum = 0;
        float[] computedValues = new float[values.Length];
        for(int i = 0; i < values.Length; i++)
        {
            Value value = values[i];
            float v = value.Compute();
            computedValues[i] = v;

            if (value.isRelative)
                relSum += v;
            else
                absSum += v;
        }

        // Calculate the direction to place shapes and the current scope size
        Vector3 dir = Vector3.zero;
        float scopeValue = absSum;
        switch (coord)
        {
            case Coords.X:
                dir = Vector3.right;
                scopeValue = scope.size.x;
                break;
            case Coords.Y:
                dir = Vector3.up;
                scopeValue = scope.size.y;
                break;
            case Coords.Z:
                dir = Vector3.forward;
                scopeValue = scope.size.z;
                break;
        }
        // Adjust direction with shape rotation
        dir = target.GetQuaternion() * dir;

        // Calculate the shapes position
        float sum = 0, current;
        for (int i = 0; i < values.Length; i++)
        {
            // Calculate the current value
            if (values[i].isRelative)
                current = computedValues[i] * (scopeValue - absSum) / relSum;
            else
                current = computedValues[i];

            // Check if an output symbol for the current slot exists
            if (symbols[i])
            {
                // Create a new scope
                Scope newScope = CreateInstance<Scope>().Init(scope);

                // Update the scope position
                newScope.position = scope.position + dir * sum;

                // Update the correct scope size
                switch(coord)
                {
                    case Coords.X:
                        newScope.size = new Vector3(current, scope.size.y, scope.size.z);
                        break;
                    case Coords.Y:
                        newScope.size = new Vector3(scope.size.x, current, scope.size.z);
                        break;
                    case Coords.Z:
                        newScope.size = new Vector3(scope.size.x, scope.size.y, current);
                        break;
                }

                // Create the new shape
                Shape newShape = CreateInstance<Shape>().Init(newScope, target.symbol == null? target.parent : target);
                newShape.symbol = null;

                // Run the symbol action item to complete creation
                symbols[i].Run(newShape);
            }

            // Move to next slot
            sum += current;
        }
        return;
    }
    
}