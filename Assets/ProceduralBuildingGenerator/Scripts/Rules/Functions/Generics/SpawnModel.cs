using UnityEngine;

/*
 * Rule function used to spawn models with terminal shapes in the scene. 
 */

[CreateAssetMenu(menuName = "PBG/Functions/Generics/SpawnModel")]
public class SpawnModel : RuleFunction {

    [SerializeField]
    new public string name; // Name to assign to gameobject

    [SerializeField]
    public GameObject model; // Reference to the model

    /*
     * Create a gameobject of the selected model with the given shape 
     * properties.
     */
    public override void Execute(Shape target) {
        if (target == null)
            throw new System.ArgumentNullException("SpawnModel: target is null");

        // Get the shape scope
        Scope scope = target.scope;

        // Create the object and assign properties
        GameObject obj = Instantiate(model, scope.position, target.GetQuaternion());
        obj.transform.localScale = scope.size;
        obj.name = name + "(Model)";

        // Assign model as children of the building gameobject
        obj.transform.parent = ProceduralBuildingGenerator.Instance.buildingFolder.transform;
    }
}