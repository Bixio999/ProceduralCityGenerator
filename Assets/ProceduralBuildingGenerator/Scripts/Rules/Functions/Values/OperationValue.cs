using UnityEngine;

/*
 * Used to evaluate arithmetic calculations in rules. 
 */

[CreateAssetMenu(menuName = "PBG/Values/OperationValue")]
public class OperationValue : Value {

    public Value firstValue; // First operand

    // Supported operators
    public enum Operation 
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power
    }

    public Operation operation; // Selected operator

    public Value secondValue; // Second operand

    /*
     * Returns the result of the defined operation.
     */
    public override float Compute()
    {
        switch (operation)
        {
            case Operation.Add:
                return firstValue.Compute() + secondValue.Compute();
            case Operation.Subtract:
                return firstValue.Compute() - secondValue.Compute();
            case Operation.Multiply:
                return firstValue.Compute() * secondValue.Compute();
            case Operation.Divide:
                return firstValue.Compute() / secondValue.Compute();
            case Operation.Power:
                return Mathf.Pow(firstValue.Compute(), secondValue.Compute());
            default:
                return 0;
        }
    }
}