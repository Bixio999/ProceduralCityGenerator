using UnityEngine;
using UnityEditor;

/*
 * Models a grammar symbol with related properties to describe a shape.
 * 
 * Each shape si defined by a scope that describes its position and size, and 
 * a rotation. Also contains a reference to its parent shape, and a flag to 
 * define whenever is an already evaluated shape or not.
 * 
 * For debug purposes, a shape possesses a gameobject that represents its form.
 */

[CreateAssetMenu(menuName = "PBG/Shape")]
public class Shape : ScriptableObject {

    [SerializeField]
    public Scope scope; // Scope of the shape

    [SerializeField]
    public Vector3 rotation; // Current rotation of the shape

    [SerializeField]
    public string symbol; // Assigned symbol of the shape

    [HideInInspector] public bool isActive = true; // Define if is already evaluated

    [HideInInspector] public Shape parent; // Reference to its parent's shape - used for occlusion

    /* --- DEBUG PARAMETERS --- */

    private GameObject obj; // Model used to see the building form of the shape

    static public GameObject prefab; // Model's prefab to use for shape's object

    /* ------------------------------ */

    /*
     * Set the new created shape as active, and first load prefab model if not
     * assigned.
     */
    private void OnEnable()
    {
        isActive = true;
        if (!prefab)
            prefab = AssetDatabase.LoadAssetAtPath("Assets/ProceduralBuildingGenerator/Prefabs/Cube.prefab", typeof(GameObject)) as GameObject;
    }

    /*
     * Backup function to ensure the shape is set to active.
     */
    private void OnValidate()
    {
        isActive = true;
    }

    /*
     * On building generation final phase, all the intermediate shapes are 
     * destroyed, so if present the shape's model have to be destroyed to not
     * interfere with meshes combination.
     */
    public void DestroyObject()
    {
        if (obj)
            DestroyImmediate(obj);
    }

    /*
     * Create the shape's model.
     * 
     * Available only on debug mode.
     */
    private GameObject CreateShapeModel()
    {
        if (ProceduralBuildingGenerator.Instance.debug)
            return null;

        obj = GameObject.Instantiate(prefab);
        obj.transform.parent = ProceduralBuildingGenerator.Instance.buildingFolder.transform;
        RefreshShapeModel();
        return obj;
    }

    /*
     * Disable the shape after evaluation.
     */
    public void DisableShape()
    {
        isActive = false;

        if (obj)
            obj.SetActive(false);
    }

    /*
     * Update shape's model with its current properties.
     */
    public void RefreshShapeModel()
    {
        if (!obj) return;
 
        obj.transform.SetPositionAndRotation(scope.position, GetQuaternion());
        obj.transform.localScale = scope.size;
        obj.name = symbol;
    }

    /*
     * Crate a new shape by cloning the given one.
     */
    public Shape Clone(Shape other)
    {
        scope = CreateInstance<Scope>().Init(other.scope);
        rotation = other.rotation;
        isActive = other.isActive;
        parent = other.parent;
        symbol = other.symbol;
        CreateShapeModel();
        return this;
    }

    /*
     * Create a new shape with default values.
     */
    public Shape Init() {
        scope = ScriptableObject.CreateInstance<Scope>().Init();
        rotation = new Vector3();
        isActive = true;
        CreateShapeModel();
        return this;
    }

    /*
     * Create a new shape by assigning the given symbol and the given shape as
     * parent. Also copy the parent's rotation.
     */
    public Shape Init(string symbol, Shape parent)
    {
        this.symbol = symbol;
        scope = CreateInstance<Scope>().Init(parent.scope);
        rotation = parent.rotation;
        isActive = true;
        this.parent = parent;
        CreateShapeModel();
        return this;
    }

    /*
     * Create a new shape with the given scope and assigning the given shape as
     * parent. Also copy the parent's rotation.
     */
    public Shape Init(Scope scope, Shape parent)
    {
        this.scope = CreateInstance<Scope>().Init(scope);
        this.symbol = parent.symbol;
        rotation = parent.rotation;
        isActive = true;
        this.parent = parent;
        CreateShapeModel();
        return this;
    }

    /*
     * Create a new shape with the given symbo, scope, rotation and assigning
     * the given shape as parent.
     */
    public Shape Init(string symbol, Scope scope, Vector3 rotation, Shape parent)
    {
        this.symbol = symbol;
        this.scope = CreateInstance<Scope>().Init(scope);
        this.rotation = rotation;
        isActive = true;
        this.parent = parent;
        CreateShapeModel();
        return this;
    }

    /*
     *  Returns the shape's rotation as quaternion. 
     */
    public Quaternion GetQuaternion()
    {
        return Quaternion.Euler(rotation.x, rotation.y, rotation.z);
    }

    /*
     * Test if the current shape intersects the given one.
     */
    public OcclusionResult Intersects(Shape other)
    {
        // Flag for partial intersection
        bool isPartial = false;

        // Test along the x axis

        float a1 = scope.position.x;
        float a2 = scope.position.x + scope.size.x;

        float b1 = other.scope.position.x;
        float b2 = other.scope.position.x + other.scope.size.x;

        if (a1 >= b2 || b1 >= a2)
            return OcclusionResult.None;
        else if (a1 < b1 && a2 > b1 && a2 < b2)
            isPartial = true;
        else if (b1 < a1 && b2 > a2 && b2 < a2)
            isPartial = true;

        // Test along the y axis

        a1 = scope.position.y;
        a2 = scope.position.y + scope.size.y;

        b1 = other.scope.position.y;
        b2 = other.scope.position.y + other.scope.size.y;

        if (a1 >= b2 || b1 >= a2)
            return OcclusionResult.None;
        else if (a1 < b1 && a2 > b1 && a2 < b2)
            isPartial = true;
        else if (b1 < a1 && b2 > a2 && b2 < a2)
            isPartial = true;

        // Test along the z axis

        a1 = scope.position.z;
        a2 = scope.position.z + scope.size.z;

        b1 = other.scope.position.z;
        b2 = other.scope.position.z + other.scope.size.z;

        if (a1 >= b2 || b1 >= a2)
            return OcclusionResult.None;
        else if (a1 < b1 && a2 > b1 && a2 < b2)
            isPartial = true;
        else if (b1 < a1 && b2 > a2 && b2 < a2)
            isPartial = true;
        
        return isPartial ? OcclusionResult.Partial : OcclusionResult.Full;
    }

    /*
     * Returns the shape's symbol as description.
     */
    public override string ToString()
    {
        return symbol;
    }
}