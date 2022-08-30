using UnityEngine;
using System.Collections;

/*
 * Models the scope of a shape.
 * 
 * Contains data about shape's position and scale (size). 
 * 
 * Scope class supports the possibility to save and reload a scope status, as
 * implementation of state store & load of l-system's turtle generation. 
 */

[CreateAssetMenu(menuName = "PBG/Scope")]
public class Scope : ScriptableObject{

    public Vector3 position; // Position of the shape

    public Vector3 size = Vector3.zero; // Scale of the shape

    /* ----------- */

    private Stack savedScopes; // Contains the saved scopes

    public static Scope Current; // Reference to the scope of the currently evaluated shape

    /* ------------------------------ */

    /*
     * Create a new scope by copying the given one
     */
    public Scope Init(Scope scope)
    {
        position = scope.position;
        size = scope.size;
        return this;
    }

    /*
     * Create a new scope with default values
     */
    public Scope Init()
    {
        position = new Vector3();
        size = new Vector3();
        return this;
    }

    /* ----- STORE & LOAD FUNCTIONS ----- */

    /*
     * Save the current scope.
     */
    public void SaveScope()
    {
        if (savedScopes == null)
            savedScopes = new Stack();
        Scope s = CreateInstance<Scope>();
        s.Init(this);
        savedScopes.Push(s);
    }

    /*
     * Restore the latest saved scope and set its status to the current
     * scope.
     */
    public void RestoreScope()
    {
        if (savedScopes == null)
            return;
        if (savedScopes.Count == 0)
            return;
        Scope scope = (Scope)savedScopes.Pop();
        position = scope.position;
        size = scope.size;
    }
}