using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/*
 * Rule query used to test if a shape overlap with another.
 * 
 * Based on situations, filters for the shapes to test with target can be applied.
 */

[CreateAssetMenu(menuName = "PBG/Conditions/Occlusion")]
public class Occlusion : RuleQuery {

    [SerializeField]
    public string query; // Filter
    
    public override object Compute(Shape target)
    {
        // Compute the query and get the result
        OcclusionResult res = query switch
        {
            "all" => OcclusionAll(target),
            "noparent" => OcclusionNoParent(target),
            "active" => OcclusionActive(target),
            _ => OcclusionSymbol(target, query),
        };

        // Convert the result to float, to be used by RuleCondition
        return Convert.ToSingle(res);
    }

    /*
     * Compute overlap between target and any other shape generated until now
     */
    private OcclusionResult OcclusionAll(Shape target)
    {
        return OcclusionTest(target, ProceduralBuildingGenerator.Instance.shapes);
    }

    /*
     * Compute overlap between target and any other shape but its relatives.
     */
    private OcclusionResult OcclusionNoParent(Shape target)
    {
        // Find all relative shapes of target
        List<Shape> parents = new List<Shape>();

        Shape current = target;
        while(current != null)
        {
            parents.Add(current);
            current = current.parent;
        }

        // Calculate the list of shapes to test
        List<Shape> shapesToTest = ProceduralBuildingGenerator.Instance.shapes.Except<Shape>(parents).ToList();

        return OcclusionTest(target, shapesToTest);
    }

    /*
     * Compute overlap between target and any other active shape.
     */
    private OcclusionResult OcclusionActive(Shape target)
    {
        List<Shape> shapesToTest = ProceduralBuildingGenerator.Instance.shapes.Where<Shape>(s => s.isActive).ToList();
        return OcclusionTest(target, shapesToTest);
    }

    /*
     * Compute overlap between target and any shape with the given symbol.
     */
    private OcclusionResult OcclusionSymbol(Shape target, string symbol)
    {
        List<Shape> shapesToTest = ProceduralBuildingGenerator.Instance.shapes.Where<Shape>(s => s.symbol == symbol).ToList();
        return OcclusionTest(target, shapesToTest);
    }

    /*
     * Calculate intersection between target shape and all the shapes in the 
     * given list.
     */
    private OcclusionResult OcclusionTest(Shape target, List<Shape> shapesToTest)
    {
       foreach (Shape s in shapesToTest)
       {
            OcclusionResult res = s.Intersects(target);
            if (res != OcclusionResult.None)
            {
                return res;
            }
       }
       return OcclusionResult.None;
    }
}