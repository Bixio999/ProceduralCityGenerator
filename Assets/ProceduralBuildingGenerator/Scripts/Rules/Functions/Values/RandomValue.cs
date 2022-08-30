using UnityEngine;

/*
 * Used to create a random value in rules. 
 */

[CreateAssetMenu(menuName = "PBG/Values/RandomValue")]
public class RandomValue : Value {

    // Values range
    public Value minValue;
    public Value maxValue;

    public override float Compute()
    {
        return Random.Range(minValue.Compute(), maxValue.Compute());
    }
}