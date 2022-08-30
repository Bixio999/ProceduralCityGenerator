using UnityEngine;

/*
 * Rule function used to repeat a series of output shapes with a given spacing 
 * until there is no more space in the chosen extension axes.
 */

[CreateAssetMenu(menuName = "PBG/Functions/Splits/Repeat")]
public class Repeat : RuleFunction {

    [SerializeField]
    public bool[] axes = new bool[3]; // Axes to consider

    [SerializeField]
    public Value[] values; // Spacing values for each axis

    [SerializeField]
    public string[] symbols; // Output symbols to repeat

    /* --------------------------- */

    /*
     * Repeat as long as possible the output shapes by dividing the target
     * scope along the selected axes into tiles of selected spacing.
     */
    public override void Execute(Shape target) {

        // Check if the function is correctly defined

        if (target == null)
            throw new System.ArgumentNullException(this.name + ": target is null");

        if (values.Length != symbols.Length)
            throw new System.ArgumentException(this.name + ": values and symbols must have the same length");

        if (axes.Length != 3)
            throw new System.ArgumentException(this.name +": axes must have length 3");

        // Check parameters

        bool axesCheck = false;
        int count = 0;
        foreach (bool b in axes)
        {
            if (b)
            {
                axesCheck = true;
                count++;
            }
        }
        if (!axesCheck)
            throw new System.ArgumentException("axes must have at least one true value");
        if (count != values.Length)
            throw new System.ArgumentException("there must be the same number of values as axes to consider");

        // Get the target scope
        Scope scope = target.scope;

        // Compute the parameters values and count absolute and relative values
        float absSum = 0, relSum = 0;
        float[] computedValues = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            Value value = values[i];
            float v = value.Compute();
            computedValues[i] = v;

            if (value.isRelative)
                relSum += v;
            else
                absSum += v;
        }

        // Calculate the repetitions
        int[] repetitions = {1,1,1};
        int valuesIndex = 0;
        for (int i = 0; i < axes.Length; i++)
        {   
            if (axes[i])
            {
                float scopeS = absSum;
                switch (i)
                {
                    case 0:
                        scopeS = scope.size.x;
                        break;
                    case 1:
                        scopeS = scope.size.y;
                        break;
                    case 2:
                        scopeS = scope.size.z;
                        break;
                }

                if (values[valuesIndex].isRelative)
                    repetitions[i] = Mathf.Max(1, Mathf.CeilToInt(scopeS / (computedValues[valuesIndex] * (scopeS - absSum) / relSum)));
                else
                    repetitions[i] = Mathf.Max(1, Mathf.CeilToInt(scopeS / computedValues[valuesIndex]));
                valuesIndex++;
            }
        }

        // Calculate the size for each tile
        float[] tileSize = new float[repetitions.Length];
        tileSize[0] = scope.size.x / repetitions[0];
        tileSize[1] = scope.size.y / repetitions[1];
        tileSize[2] = scope.size.z / repetitions[2];

        // Calculate the movement of the scope for each tile
        Vector3 moveP = Vector3.zero;
        for(int i = 0; i < repetitions[((int)Coords.Y)]; i++)
        {
            if (i > 0)
                moveP += target.GetQuaternion() * Vector3.up * tileSize[(int)Coords.Y];

            for (int j = 0; j < repetitions[(int)Coords.X]; j++)
            {
                if (j > 0)
                    moveP += target.GetQuaternion() * Vector3.right * tileSize[(int)Coords.X];

                for (int k = 0; k < repetitions[(int)Coords.Z]; k++)
                {
                    if (k > 0)
                        moveP += target.GetQuaternion() * Vector3.forward * tileSize[(int)Coords.Z];

                    // Create and compute the scope for the current tile
                    Scope newScope = CreateInstance<Scope>().Init(scope);
                    newScope.position += moveP;
                    newScope.size = new Vector3(tileSize[(int)Coords.X], tileSize[(int)Coords.Y], tileSize[(int)Coords.Z]);

                    // Create a new shape for each output symbol
                    foreach (string symbol in symbols)
                    {
                        Shape newShape = CreateInstance<Shape>().Init(symbol, newScope, target.rotation, target);
                        ProceduralBuildingGenerator.Instance.evaluation.Enqueue(newShape);
                        ProceduralBuildingGenerator.Instance.shapes.Add(newShape);
                    }
                }
            }
        }

        return;
    }
    
}