using UnityEngine;

/*
 * Rule function that split a shape into a set of sub-dimensional shapes. 
 * 
 * Example: a 3D shape can be splitted into 6 2D faces.
 * 
 * A 2D shape is defined by a scope with z scale (size) value to 0.
 */

[CreateAssetMenu(menuName = "PBG/Functions/Splits/Component")]
public class Component : RuleFunction {

    // Supported split operations
    public enum CompType
    {
        faces,
        edges,
        vertices,
        face,
        edge,
        vertex,
        sideFaces,
        sideFace,
        sideEdges,
        topEdges,
        bottomEdges
    }

    [SerializeField]
    public CompType type; // Selected operation type

    [SerializeField]
    public bool hasParameter; // If the operation support custom parameters, apply them

    [SerializeField]
    public Value[] parameters; // Additional parameters

    [SerializeField]
    public string[] symbols; // Set of output symbols for the result shapes

    /* --------------------------- */

    /*
     * Apply the splitting on the target shape with the selected parameters.
     */
    public override void Execute(Shape target) {

        if (symbols.Length == 0)
            throw new System.ArgumentException(this.name + ": there must be at least one symbol");

        // Get the output shapes

        Shape[] outputShapes;

        // Some operations use parameters to assign given output symbols in a certain
        // order and quantity, so the final symbol assignment have to be skipped.
        bool symbolsAssigned = false;

        switch(type)
        {
            case CompType.faces:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split faces on 2D shape");
                outputShapes = SplitFaces(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.sideFaces:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split faces on 2D shape");
                outputShapes = SplitSideFaces(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.sideEdges:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split side edges on 2D shape");
                outputShapes = SplitSideEdges(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.topEdges:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split top edges on 2D shape");
                outputShapes = SplitTopEdges(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.bottomEdges:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split bottom edges on 2D shape");
                outputShapes = SplitBottomEdges(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.face:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split faces on 2D shape");
                if (!hasParameter || parameters.Length != 1)
                    throw new System.ArgumentException("parameter must be positive and flag setted");
                outputShapes = SplitFace(target, Mathf.RoundToInt(parameters[0].Compute()));
                break;
            case CompType.sideFace:
                if (target.scope.size.z == 0)
                    throw new System.ArgumentException(this.name + ": called split faces on 2D shape");
                if (!hasParameter || parameters.Length != 1)
                    throw new System.ArgumentException("parameter must be positive and flag setted");
                outputShapes = SplitSideFace(target, Mathf.RoundToInt(parameters[0].Compute()));
                break;

            case CompType.edges:
                if (target.scope.size.z == 0)
                    outputShapes = SplitEdges2D(target);
                else
                    outputShapes = SplitEdges(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.vertices:
                if (target.scope.size.z == 0)
                    outputShapes = SplitVertices2D(target);
                else
                    outputShapes = SplitVertices(target);
                symbolsAssigned = AssignSymbols(outputShapes);
                break;
            case CompType.edge:
                if (!hasParameter || parameters.Length != 1)
                    throw new System.ArgumentException("parameter must be positive and flag setted");
                outputShapes = SplitEdge(target, Mathf.RoundToInt(parameters[0].Compute()));
                break;
            case CompType.vertex:
                if (!hasParameter || parameters.Length != 1)
                    throw new System.ArgumentException("parameter must be positive and flag setted");
                outputShapes = SplitVertex(target, Mathf.RoundToInt(parameters[0].Compute()));
                break;

            default:
                return;
        }

        // Assign symbols to output shapes

        foreach (Shape shape in outputShapes)
        {
            if (symbolsAssigned)
            {
                shape.parent = target;
                shape.RefreshShapeModel();
                ProceduralBuildingGenerator.Instance.evaluation.Enqueue(shape);
                ProceduralBuildingGenerator.Instance.shapes.Add(shape);
            }
            else
            {
                foreach (string symbol in symbols)
                {
                    Shape newShape = CreateInstance<Shape>().Init(symbol, shape.scope, shape.rotation, target);
                    ProceduralBuildingGenerator.Instance.evaluation.Enqueue(newShape);
                    ProceduralBuildingGenerator.Instance.shapes.Add(newShape);
                }
                shape.DestroyObject();
            }
        }
    }

    /*
     * Assign symbols to output shapes, if custom naming is requested.
     * 
     * Use parameters to set how many shapes should be assigned to each output
     * symbol. 
     * 
     * Example: split raw building shape into facade symbol for the side faces,
     * flooring symbol for the bottom face, and roof symbol for the top face;
     * set as parameters the values {4, 1, 1} and as symbols {"facade", "flooring",
     * "roof"} to obtain 4 shapes assigned to side faces with facade symbol, a 
     * shape assigned to bottom face with flooring symbol and a shape assigned
     * to top face with roof symbol. 
     */
    private bool AssignSymbols(Shape[] outputShapes)
    {
        // Check if parameters are defined
        if (hasParameter && parameters.Length > 0 && parameters.Length == symbols.Length)
        {
            // Compute the parameters values
            int[] values = new int[parameters.Length];

            // Calculate their sum 
            int sum = 0; 
            for (int i = 0; i < parameters.Length; i++)
            {
                values[i] = Mathf.RoundToInt(parameters[i].Compute());
                sum += values[i];
            }

            // Check if the sum of parameters values is equal to the number of
            // output shapes
            if (sum == outputShapes.Length)
            {
                // Proceed with the symbol assignment
                int index = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    for (int j = 0; j < values[i]; j++)
                    {
                        outputShapes[index].symbol = symbols[i];
                        index++;
                    }
                }
                return true;
            }
        } 
        return false;
    }

    /* --------------------------- */

    /*
     * Split a 3D shape to obtain 1D shapes representing the side edges of the
     * shape.
     */
    private Shape[] SplitSideEdges(Shape target)
    {
        // Create structure for output shapes
        Shape[] outputShapes = new Shape[4];

        // Get all edges from target
        Shape[] edges = SplitEdges(target);

        // Save only side edges
        int index = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            if (i % 3 == 1)
            {
                outputShapes[index] = edges[i];
                index++;
            }
            else
                edges[i].DestroyObject();
        }
        
        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain 1D shapes representing the top edges of the
     * shape.
     */
    private Shape[] SplitTopEdges(Shape target)
    {
        // Create structure for output shapes
        Shape[] outputShapes = new Shape[4];

        // Get all edges from target
        Shape[] edges = SplitEdges(target);

        // Save only top edges
        int index = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            if (i % 3 == 2)
            {
                outputShapes[index] = edges[i];
                index++;
            }
            else
                edges[i].DestroyObject();
        }
        
        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain 1D shapes representing the bottom edges of the
     * shape.
     */
    private Shape[] SplitBottomEdges(Shape target)
    {
        // Create structure for output shapes
        Shape[] outputShapes = new Shape[4];

        // Get all edges from target
        Shape[] edges = SplitEdges(target);

        // Save only bottom edges
        int index = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            if (i % 3 == 0)
            {
                outputShapes[index] = edges[i];
                index++;
            }
            else
                edges[i].DestroyObject();
        }
        
        return outputShapes;
    }
 
    /*
     * Split a 3D shape to obtain 2D shapes representing the side faces of the
     * shape.
     */
    private Shape[] SplitSideFaces(Shape target)
    {
        // Create structure for output shapes
        Shape[] outputShapes = new Shape[4];

        // Compute move vectors for shapes position
        Vector3[] moveP = {Vector3.zero, Vector3.right, Vector3.forward + Vector3.right, Vector3.forward};

        // Calculate the shapes
        for(int i = 0; i < 4; i ++)
        {
            // Create a new scope
            Scope s = CreateInstance<Scope>().Init(target.scope);

            // Compute its scope position 
            s.position += Vector3.Scale(moveP[i], target.scope.size);

            // Set the scope size
            Vector3 newSize;

            // Assign the correct size on x-axis
            if (i % 2 == 0)
                newSize = new Vector3(target.scope.size.x, target.scope.size.y, 0);
            else
                newSize = new Vector3(target.scope.size.z, target.scope.size.y, 0);
            s.size = newSize;

            // Calculate rotation for current face
            Vector3 rotation = target.rotation;
            rotation.y += -90 * i;

            // Create the shape
            outputShapes[i] = CreateInstance<Shape>().Init(null, s, rotation, target);
        }
        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain a 2D shape representing one of the side faces
     * of the shape. 
     * 
     * The parameter value represents the index of the side face requested.
     */
    private Shape[] SplitSideFace(Shape target, int parameter)
    {
        // Check if the given parameter index is valid
        if (parameter > 0 && parameter < 5)
        {
            Shape[] outputShapes = null;

            // Get all sidefaces from target
            Shape[] sideFaces = SplitSideFaces(target);

            // Destroy the unused shapes
            for(int i = 0; i < sideFaces.Length; i++)
            {
                if (i == parameter - 1)
                    outputShapes = new Shape[] { sideFaces[i] };
                else
                    sideFaces[i].DestroyObject();
            }

            return outputShapes;
        }
        else
            throw new System.ArgumentException(this.name + ": parameter must be between 1 and 4");
    }

    /*
     * Split a 3D shape to obtain a 2D shape representing one of the faces of 
     * the shape.
     * 
     * The parameter value represents the index of the face requested.
     */
    private Shape[] SplitFace(Shape target, int parameter)
    {
        // Check if the given parameter index is valid
        if (parameter > 0 && parameter < 7)
        {
            Shape[] outputShapes = null;

            // Get all faces from target
            Shape[] faces = SplitFaces(target);

            // Destroy unused shapes
            for (int i = 0; i < faces.Length; i++)
            {
                if (i == parameter - 1)
                    outputShapes = new Shape[] { faces[i] };
                else
                    faces[i].DestroyObject();
            }
            return outputShapes;
        }
        else
            throw new System.ArgumentException(this.name + ": parameter must be between 1 and 6");
    }

    /*
     * Split a 3D shape to obtain 2D shapes representing the faces of the shape.
     */
    private Shape[] SplitFaces(Shape target)
    {
        // Get the sidefaces from target
        Shape[] sideFaces = SplitSideFaces(target);
        Shape[] outputShapes = new Shape[6];

        // Move sidefaces to output array
        for(int i = 0; i < 4; i ++)
            outputShapes[i] = sideFaces[i];

        // Calculate the bottom face
        Scope s = CreateInstance<Scope>().Init(target.scope);
        s.position += Vector3.Scale(Vector3.forward, target.scope.size);
        s.size = new Vector3(target.scope.size.x, target.scope.size.z, 0);
        Vector3 rotation = target.rotation + new Vector3(-90, 0, 0);

        outputShapes[4] = CreateInstance<Shape>().Init(null, s, rotation, target);

        // Calculate the top face
        s = CreateInstance<Scope>().Init(target.scope);
        s.position += Vector3.Scale(Vector3.right + Vector3.forward + Vector3.up, target.scope.size);
        s.size = new Vector3(target.scope.size.x, target.scope.size.z, 0);
        rotation = target.rotation + new Vector3(90, 180, 0);

        outputShapes[5] = CreateInstance<Shape>().Init(null, s, rotation, target);

        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain a 1D shape representing a side edge of
     * the shape.
     * 
     * The parameter value represents the index of the face requested.
     */
    private Shape[] SplitEdge(Shape target, int parameter)
    {
        // Check if the parameter index is valid
        if (parameter > 0 && parameter < 13)
        {
            Shape[] outputShapes = null;

            // Get the edges from targeet
            Shape[] edges = SplitEdges(target);

            // Destroy unused shapes
            for (int i = 0; i < edges.Length; i++)
            {
                if (i == parameter - 1)
                    outputShapes = new Shape[] { edges[i] };
                else
                    edges[i].DestroyObject();
            }
            return outputShapes;
        }
        else
            throw new System.ArgumentException(this.name + ": parameter must be between 1 and 12");
    }

    /*
     * Split a 2D shape to obtain a 1D shape representing an edge of the shape.
     */
    private Shape[] SplitEdges2D(Shape target)
    {
        // Create structure for output shapes
        Shape[] outputShapes = new Shape[4];

        // Compute move vectors for shapes position
        Vector3[] moveP = {Vector3.zero, Vector3.right, Vector3.up + Vector3.right, Vector3.up};

        // Calculate the shapes
        for(int i = 0; i < 4; i ++)
        {
            // Create a new scope
            Scope s = CreateInstance<Scope>().Init(target.scope);

            // Set the shape position
            s.position += target.GetQuaternion() * Vector3.Scale(moveP[i], target.scope.size);

            // Set the size based on the current edge
            if (i % 2 == 0)
                s.size = new Vector3(target.scope.size.x, 0, 0);
            else
                s.size = new Vector3(target.scope.size.y, 0, 0);

            // Set the rotation based on current edge
            Vector3 rotation = target.rotation;
            rotation.y += -90 * i;

            // Create the shape
            outputShapes[i] = CreateInstance<Shape>().Init(null, s, rotation, target);
        }
        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain 1D shapes representing the edge of the shape.
     */
    private Shape[] SplitEdges(Shape target)
    {
        // Create structure for output shapes
        Shape[] outputShapes = new Shape[12];

        // Compute move vectors for shapes position
        Vector3[] moveP = {Vector3.zero, Vector3.right, Vector3.forward + Vector3.right, Vector3.forward};

        // Compute rotation factor for each side face
        int[] planeMasksSideEdges = {1, -1, -1, 1};

        for(int i = 0; i < 4; i ++)
        {
            // Bottom edges
            Scope s = CreateInstance<Scope>().Init(target.scope);
            Vector3 moveDir = target.GetQuaternion() * moveP[i];
            s.position += Vector3.Scale(moveDir, target.scope.size);

            if (i % 2 == 0)
                s.size = new Vector3(target.scope.size.x, 0, 0);
            else
                s.size = new Vector3(target.scope.size.z, 0, 0);

            Vector3 rotation = target.rotation + new Vector3(0, i * -90, 0);
            outputShapes[3 * i] = CreateInstance<Shape>().Init(null, s, rotation, target);

            // Side edges
            s = CreateInstance<Scope>().Init(target.scope);
            s.position += Vector3.Scale(moveDir, target.scope.size);

            s.size = new Vector3(target.scope.size.y, 0, 0);

            if (i % 2 == 0)
                rotation = target.rotation + new Vector3(0, i * -90, planeMasksSideEdges[i] * -90);
            else
                rotation = target.rotation + new Vector3(planeMasksSideEdges[i] * -90, i * 90, 0);

            outputShapes[3 * i + 1] = CreateInstance<Shape>().Init(null, s, rotation, target);

            // Top edges
            s = CreateInstance<Scope>().Init(target.scope);

            moveDir = target.GetQuaternion() * (moveP[i] + Vector3.up);
            s.position += Vector3.Scale(moveDir, target.scope.size);

            if (i % 2 == 0)
                s.size = new Vector3(target.scope.size.x, 0, 0);
            else
                s.size = new Vector3(target.scope.size.z, 0, 0);

            rotation = target.rotation + new Vector3(0, i * -90, 0);
            outputShapes[3 * i + 2] = CreateInstance<Shape>().Init(null, s, rotation, target);
        }

        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain a point representing a vertex of the shape.
     * 
     * The parameter value represents the index of the face requested.
     */
    private Shape[] SplitVertex(Shape target, int parameter)
    {
        if (parameter > 0 && parameter < 9)
        {
            Shape[] outputShapes = null;

            // Get the verteces from targeet
            Shape[] vertices = SplitVertices(target);

            // Destroy unused shapes
            for (int i = 0; i < vertices.Length; i++)
            {
                if (i == parameter - 1)
                    outputShapes = new Shape[] { vertices[i] };
                else
                    vertices[i].DestroyObject();
            }
            return outputShapes;
        }
        else
            throw new System.ArgumentException(this.name + ": parameter must be between 1 and 8");
    }

    /*
     * Split a 2D shape to obtain a set of points representing the verteces of
     * the shape.
     */
    private Shape[] SplitVertices2D(Shape target)
    {
        // Create structure for output shape
        Shape[] outputShapes = new Shape[4];

        // Compute move vectors for shapes position
        Vector3[] moveP = {Vector3.zero, Vector3.right, Vector3.up + Vector3.right, Vector3.up};

        // Calculate the shapes
        for(int i = 0; i < 4; i ++)
        {
            // Create the scope
            Scope s = CreateInstance<Scope>().Init(target.scope);

            // Set the position
            Vector3 moveDir = target.GetQuaternion() * moveP[i];
            s.position += Vector3.Scale(moveDir, target.scope.size);

            // Set size to zero 
            s.size = Vector3.zero;

            // Set the shape rotation
            Vector3 rotation = target.rotation;
            rotation.y += -90 * i;

            // Create the shape
            outputShapes[i] = CreateInstance<Shape>().Init(null, s, rotation, target);
        }
        return outputShapes;
    }

    /*
     * Split a 3D shape to obtain a set of points representing the verteces of 
     * the shape.
     */
    private Shape[] SplitVertices(Shape target)
    {
        // Create structure for output shape
        Shape[] outputShapes = new Shape[8];

        // Compute move vectors for shape position
        Vector3[] moveP = {Vector3.zero, Vector3.right, Vector3.forward + Vector3.right, Vector3.forward};

        // Calculate the shapes
        for (int i = 0; i < 4; i++)
        {
            // Bottom vertices
            Scope s = CreateInstance<Scope>().Init(target.scope);
            s.position += Vector3.Scale(moveP[i], target.scope.size);
            s.size = new Vector3(0, 0, 0);
            Vector3 rotation = target.rotation + new Vector3(0, i * -90, 0);
            outputShapes[2 * i] = CreateInstance<Shape>().Init(null, s, rotation, target);

            // Top vertices
            s = CreateInstance<Scope>().Init(target.scope);
            s.position += Vector3.Scale(moveP[i] + Vector3.up, target.scope.size);
            s.size = new Vector3(0, 0, 0);
            rotation = target.rotation + new Vector3(0, i * -90, 0);
            outputShapes[2 * i + 1] = CreateInstance<Shape>().Init(null, s, rotation, target);
        }
        return outputShapes;
    }
    
}