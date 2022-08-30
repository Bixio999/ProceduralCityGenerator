using UnityEngine;

/*
 * Rule function used to generate a roof shape compatible with the selected
 * roof type.
 * 
 * Note: currently, only hipped roof type is implemented. 
 */

[CreateAssetMenu(menuName = "PBG/Functions/Generics/Roof")]
public class Roof : RuleFunction{

    [SerializeField]
    public RoofType type; // Type of the roof

    [SerializeField]
    public Value angle; // Desired roof shape angle

    public string output; // Symbol assigned to generated shape

    /*
     * Generate a roof shape for the selected roof type.
     */
    public override void Execute(Shape target) {
        Shape roof = null;

        // Get the correct shape
        switch (type) {
            case RoofType.Gambrel:

            case RoofType.Cone:

            case RoofType.Gabled:

            case RoofType.CrossGable:

            case RoofType.Mansard:

            case RoofType.Hipped:
                roof = SetHipped(target);
                break;
        }

        // If the shape was correctly created, assign the symbol and add it to
        // the evaluation queue
        if (roof)
        {
            roof.symbol = this.output;
            roof.RefreshShapeModel();

            ProceduralBuildingGenerator.Instance.evaluation.Enqueue(roof);
            ProceduralBuildingGenerator.Instance.shapes.Add(roof);
        }
        else
            throw new System.NullReferenceException("No shape was created");
    }

    /*
     * Generate a suitable shape for an hipped roof with the selected 
     * properties.
     */
    private Shape SetHipped(Shape target)
    {
        // Copy the target scope
        Scope s = CreateInstance<Scope>().Init(target.scope);

        // Set the height of the scope in order to obtain an hipped roof with the
        // requested angle
        s.size.y = (target.scope.size.z * Mathf.Tan(Mathf.Deg2Rad * angle.Compute())) / 2;

        // Create the shape
        return CreateInstance<Shape>().Init(null, s, target.rotation, target);
    }

    // Supported roof types
    public enum RoofType 
    {
        Gambrel,
        Cone,
        Gabled,
        Hipped,
        CrossGable,
        Mansard
    }
}