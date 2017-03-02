using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;

// TODO: Add scene generation in editor outside of runtime
// TODO: Take nodes Length and Radius properties into account
// TODO: Overall naming needs some fixing

public class XMLParser : MonoBehaviour {

	[SerializeField] TextAsset rawXML;

    XmlSerializer serializer;
    Node root;
    GameObject rootGO;

    public class Node
    {
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("Geometry", IsNullable = true)]
        public string Geometry { get; set; }
        [XmlElement("Length", IsNullable = true)]
        public string Length { get; set; }
        [XmlElement("Radius", IsNullable = true)]
        public string Radius { get; set; }
        [XmlElement("Transformation")]
        public string Transformation { get; set; }
        [XmlElement("Node")]
        public List<Node> Children { get; set; }
    }

    // Use this for initialization
    void Start ()
    {
        TextReader reader = new StringReader(rawXML.text);
        serializer = new XmlSerializer(typeof(Node));
        root = (Node)serializer.Deserialize(reader);
    }

    public void createScene()
    {
    	buildSceneTree(root, null);
    	// addBoundingBox(rootGO);
    }

    void buildSceneTree(Node node, GameObject parent)
    { 	
    	GameObject newGO;
    	var matrix = StringToMatrix(node.Transformation);

    	// Create new GameObject based on the Node geometry
    	newGO = node.Geometry != null
    		? GeometryToGameObject(node.Geometry)
    		: new GameObject();

    	// Set new GameObject's parent if it isn't the root GameObject
    	if (parent != null)
	    	newGO.transform.SetParent(parent.transform);
	    else
	    	rootGO = newGO;

	    // Apply the transformation from the matrix
    	setTransformFromMatrix(ref newGO, ref matrix);

    	newGO.name = node.Name;

    	// Recursively go through all nodes in the tree
    	foreach (Node child in node.Children)
    	{
    		buildSceneTree(child, newGO);
    	}
    }

    public void calculateBoundingBox()
    {
    	addBoundingBox(rootGO);
    }

    // Add a bounding box that contains all the objects in the tree
    void addBoundingBox(GameObject go)
    {
    	BoxCollider boundingBox = go.GetComponent<BoxCollider>();
    	if (!boundingBox)
    		boundingBox = go.AddComponent<BoxCollider>();

    	Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

    	Bounds bounds = new Bounds(go.transform.position, Vector3.zero);

    	foreach (Renderer renderer in renderers)
    	{
    		// This function is awesome
	    	bounds.Encapsulate(renderer.bounds);
    	}
    	boundingBox.center = bounds.center - go.transform.position;
    	boundingBox.size = bounds.size;
    }

    // Apply a 4x4 matrix to a GameObject's transform
    void setTransformFromMatrix(ref GameObject go, ref Matrix4x4 matrix)
    {
    	go.transform.localPosition = MatrixToTranslation(ref matrix);
    	go.transform.localRotation = MatrixToRotation(ref matrix);
    	go.transform.localScale = MatrixToScale(ref matrix);
    }

    //
    // Helpers functions - Names should be explicit enough
    //
    // Matrix conversion functions come from this thread:
    // https://forum.unity3d.com/threads/how-to-assign-matrix4x4-to-transform.121966/
    // Pretty interresting.
    //

    Vector3 MatrixToTranslation(ref Matrix4x4 matrix)
    {
	    Vector3 translate;
	    translate.x = matrix.m03;
	    translate.y = matrix.m13;
	    translate.z = matrix.m23;

	    return translate;
    }

    Quaternion MatrixToRotation(ref Matrix4x4 matrix)
    {
	    Vector3 forward;
	    forward.x = matrix.m02;
	    forward.y = matrix.m12;
	    forward.z = matrix.m22;
	 
	    Vector3 upwards;
	    upwards.x = matrix.m01;
	    upwards.y = matrix.m11;
	    upwards.z = matrix.m21;
	 
	    return Quaternion.LookRotation(forward, upwards);
    }

    Vector3 MatrixToScale(ref Matrix4x4 matrix)
    {
	    Vector3 scale;
	    scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
	    scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
	    scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;

	    return scale;
    }

    GameObject GeometryToGameObject(String geometry)
    {
    	switch (geometry) 
    	{
    		case "Sphere":
    			return GameObject.CreatePrimitive(PrimitiveType.Sphere);
    		case "Cube":
    			return GameObject.CreatePrimitive(PrimitiveType.Cube);
    		default:
    			throw new Exception("Geometry type <" + geometry + "> not supported");
    	}
    }

    Matrix4x4 StringToMatrix(String transformation)
    {
    	var matrix = new Matrix4x4();

    	float[] floatArray = StringToFloatArray(transformation);
    	if (floatArray.Length != 16)
    		throw new Exception("Bad matrix formatting");
    	for (int i = 0; i < 15; i += 4)
    	{
    		matrix.SetRow(i / 4, new Vector4(
    			floatArray[i],
    			floatArray[i + 1],
    			floatArray[i + 2],
    			floatArray[i + 3]));
    	}
    	return matrix;
    }

    // Returns an array of floats extracted from a string where values
    // are separated with the space character.
    // Also makes up for bad formatting (eg. '.0' or '0.')
    float[] StringToFloatArray(String transformation)
    {
    	return transformation.Split(' ')
    		.Where(str => !String.IsNullOrEmpty(str))
    		.Select(floatStr => {
	    		if (floatStr.StartsWith("."))
	    			floatStr.Insert(0, "0");
	    		else if (floatStr.EndsWith("."))
	    			floatStr.Insert(floatStr.Length - 1, "0");
	    		return float.Parse(floatStr);
    		}
    	).ToArray();
    }
}
