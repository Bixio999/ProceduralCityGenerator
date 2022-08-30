using System.Collections.Generic;
using UnityEngine;

/*
 * Generate building by using grammar-like rule sets. 
 * 
 * The rulesets can be created directly from Unity editor, as scriptable 
 * objects.
 * 
 * Based on "Procedural Modeling of Buildings" by Muller.
 */
public class ProceduralBuildingGenerator : MonoBehaviour
{
    // Singleton pattern, used to access data without direct references
    private static ProceduralBuildingGenerator _instance;
    public static ProceduralBuildingGenerator Instance 
    { 
        get 
        {
            return _instance; 
        }
    }

    // Generation module queue
    public Queue<Shape> evaluation = new Queue<Shape>();

    // History of generated shapes - used for queries
    [HideInInspector] public List<Shape> shapes = new List<Shape>();

    // Available rulesets
    public RuleSet[] ruleSets;

    // Generated Building object
    [HideInInspector]public GameObject buildingFolder;

    // DEBUG PARAMETERS
    [Header("--- USE ONLY IN CASE OF INDEPENDENT PROJECT ---")]
    public bool debug = false;
    public int RandomSeed = 0;
    public Material buildingMaterial;
    public string currentRuleSet;
    public int maxIterations;

    /* ----------------------------- */

    /*
     * Setup the singleton, and run the generation if debug mode is 
     * active.
     */
    void Awake()
    {
        _instance = this;
        if (debug)
            GenerateFromRuleSet(currentRuleSet);
    }

    /* -------- PUBLIC FUNCTIONS -------- */

    /*
     * Clear the data structures.
     */
    public void Clear()
    {
        evaluation.Clear();
        shapes.Clear();
    }

    /*
     * Generate a new building by using the given ruleset and set up building
     * lot with given sizes.
     */
    public GameObject GenerateFromRuleSet(string ruleSetName, Vector2 lotDimension)
    {
        // Reset the state for the new generation
        ResetState();

        // Find the ruleset and start the generation
        foreach (RuleSet ruleSet in ruleSets)
        {
            if (ruleSet.name == ruleSetName)
            {
                Generate(ruleSet, lotDimension);
                return this.buildingFolder;
            }
        }
        throw new System.ArgumentException("Rule set not found");
    }

    /* -------- PRIVATE FUNCTIONS -------- */

    /*
     * Debug function;
     * 
     * Generate a new building by using given ruleset.
     */
    private void GenerateFromRuleSet(string ruleSetName)
    {
        foreach (RuleSet ruleSet in ruleSets)
        {
            if (ruleSet.name == ruleSetName)
            {
                Generate(ruleSet);
                return;
            }
        }
    }

    /*
     * Clear the data structure and reset the building object.
     */
    private void ResetState()
    {
        Clear();
        this.buildingFolder = null;
    }

    /*
     * Handler for data setup and generation init. 
     * 
     * Run the generation with default ruleset
     * axiom.
     * 
     * Set the random seed if debug mode is active.
     */
    private void Generate(RuleSet ruleSet)
    {
        // If debug mode is active, update the seed
        if (debug)
        {
            if (RandomSeed == 0)
                RandomSeed = (int) System.DateTime.Now.Ticks;
            Random.InitState(RandomSeed);
            Debug.Log(RandomSeed);
        }

        // Create the building object
        this.buildingFolder = new GameObject("Building");

        // Clone the default axiom from ruleset
        Shape axiom = ScriptableObject.CreateInstance<Shape>().Clone(ruleSet.axiom);

        // Run the generation
        Compute(ruleSet, axiom);

        // Destroy non-terminal shapes and combine final meshes to optimize the
        // model
        CleanAndFix(this.buildingFolder);
    }

    /*
     * Handler for data setup and generation init.
     * 
     * Run the generation with the given building lot size.
     */
    private void Generate(RuleSet ruleSet, Vector2 lotDimension)
    {
        // Create the building object
        this.buildingFolder = new GameObject("Building");

        // Clone and update the axiom
        Shape axiom = ScriptableObject.CreateInstance<Shape>().Clone(ruleSet.axiom);
        axiom.scope.size = new Vector3(lotDimension.x, 0, lotDimension.y);

        // Run the generation
        Compute(ruleSet, axiom);

        // Destroy intermediate shapes and combine final meshes to optimize the
        // model
        CleanAndFix(this.buildingFolder);
    }

    /*
     * Run the generation of a new building by given ruleset and axiom shape.
     */
    private void Compute(RuleSet ruleSet, Shape axiom)
    {
        // Add axiom shape to evaluation queue and shape history
        evaluation.Enqueue(axiom);
        shapes.Add(axiom);

        // Evaluate shapes until generation is completed
        Shape shape;
        while (evaluation.Count > 0)
        {
            // Get a shape to evaluate
            shape = evaluation.Dequeue();

            // Update the current scope reference
            Scope.Current = shape.scope;

            // Execute gramma rules for the current shape
            ruleSet.Execute(shape);

            // Disable evaluated shapes
            shape.DisableShape();
        }
    }

    /*
     * Remove from the building object all the generated intermediate shapes.
     * 
     * The inteermediate shapes are set to inactive, and have to be destroyed
     * to save memory and avoid contribution during mesh combination. 
     */
    public void CleanShapes(GameObject building)
    {
        // Remove all unactive shapes
        foreach (Transform shape in building.transform)
        {
            if (!shape.gameObject.activeSelf)
            {
                DestroyImmediate(shape.gameObject);
            }
        }
    }

    /*
     * Combine final shapes of the building into a single mesh in order to save
     * memory and optimize rendering. 
     */
    public void FixMeshes(GameObject building)
    {
        MeshFilter[] meshFilters = building.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            meshFilters[i].transform.parent.gameObject.SetActive(false);

            i++;
        }
        MeshFilter filter = building.AddComponent<MeshFilter>();
        filter.mesh = new Mesh();
        filter.mesh.CombineMeshes(combine);

        building.AddComponent<MeshRenderer>().material = this.buildingMaterial;
        building.AddComponent<BoxCollider>();

        // Delete all shapes
        while (building.transform.childCount > 0)
            DestroyImmediate(building.transform.GetChild(0).gameObject);

        building.isStatic = true;
    }

    /*
     * Handler that performs removal of non-terminal shapes and mesh 
     * combination.
     */
    private void CleanAndFix(GameObject building)
    {
        CleanShapes(building);
        FixMeshes(building);
    }

}
