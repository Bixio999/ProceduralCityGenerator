using UnityEngine;

/*
 * Models an event executed by a rule action, when fired.
 * 
 * Contains a set of rule functions describing the operations to be executed for 
 * this event.
 * 
 * An item can have an output symbol that is assigned to a new child shape. 
 * An output shape is a copy of its parent shape with the a new symbol. 
 * 
 * In case of rule actions that only copy the target shape, an item can only 
 * have the output symbol defined, without any additional function to execute. 
 * On the other hand, a rule action can also run an item containing functions
 * without direct outputs.
 * 
 * A rule item can also be defined by rule functions when they need to generate a
 * custom shape. In this case, an item is described only with an output symbol
 * and no functions: the target shape is then renamed with the output symbol.
 */

[CreateAssetMenu(menuName = "PBG/RuleActionItem")]
public class RuleActionItem : ScriptableObject {

    [SerializeField]
    public RuleFunction[] functions; // Set of rule functions

    [SerializeField]
    public string output; // Action output

    /*
     * Run the action item by executing all of its functions, 
     */
    public void Run(Shape target) {
        // Check if the rule item was correctly defined
        if (functions == null && output == null)
            throw new System.ArgumentException(this.name + ": empty rule action item");

        // Execute all its functions
        foreach (RuleFunction function in functions) {
            function.Execute(target);
        }

        // Create the output shape
        if (output != null && output != "")
        {
            Shape newShape;
            if (target.symbol != null && target.symbol != "")
            {
                newShape = ScriptableObject.CreateInstance<Shape>();
                newShape.Init(output, target);
            }
            else
            {
                newShape = target;
                newShape.symbol = output;
                newShape.RefreshShapeModel();
            }
            ProceduralBuildingGenerator.Instance.evaluation.Enqueue(newShape);
            ProceduralBuildingGenerator.Instance.shapes.Add(newShape);
        }
        // Destroy temporary target shapes
        else if (target.symbol == null)
            target.DestroyObject();
    }
}

/* ---------------------------- */

/*
 * Rule function superclass, used to edit objects from Unity editor.
 */
public abstract class RuleFunction : ScriptableObject {
    public abstract void Execute(Shape target);
}

// Used to reference axes in functions and conditions
public enum Coords {
    X = 0,
    Y = 1,
    Z = 2
}