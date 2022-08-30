using UnityEngine;

/*
 * Used to select an occlusion query result value. 
 */

[CreateAssetMenu(menuName = "PBG/Values/OcclusionValue")]
public class OcclusionValue : Value {
    public OcclusionResult value;

    public override float Compute()
    {
        return ((float)value);
    }
}

// Supported values
public enum OcclusionResult {
    Full = 0,
    Partial = 1,
    None = 2
}