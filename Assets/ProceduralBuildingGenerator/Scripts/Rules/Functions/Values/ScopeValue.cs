using UnityEngine;

/*
 * Used to introduce scope values in rules calculations by reading the current
 * scope. 
 */

[CreateAssetMenu(menuName = "PBG/Values/ScopeValue")]
public class ScopeValue : Value {

    public Coords coord; // Coordinate to read
    public ScopeOperation operation; // Property to read

    // Supported operations
    public enum ScopeOperation {
        Position,
        Size
    }

    /*
     * Get the current scope and return the selected property value.
     */
    public override float Compute()
    {
        Scope scope = Scope.Current;

        if (operation == ScopeOperation.Position)
            return GetCoord(scope.position);
        else
            return GetCoord(scope.size);
    }

    /*
     * Returns the value assigned to selected coordinate in the given vector.
     */
    private float GetCoord(Vector3 vector)
    {
        return coord switch
        {
            Coords.X => vector.x,
            Coords.Y => vector.y,
            Coords.Z => vector.z,
            _ => 0,
        };
    }
}