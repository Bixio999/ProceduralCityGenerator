using UnityEngine;

/*
 * Used to assign a numeric constant in rules.
 */

[CreateAssetMenu(menuName = "PBG/Values/NumericValue")]
public class NumericValue : Value {

    public float value;

    public override float Compute()
    {
        return value;
    }
}

/*
 * Value superclass, used to edit objects from Unity editor.
 */
public abstract class Value : ScriptableObject {
    public bool isRelative;
    public abstract float Compute();
}