using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using PlantGeneratorHelper;
using TreeEditor;

public class PlantGenerator : MonoBehaviour
{
    private string tree;
    [SerializeField] private string axiom;
    [SerializeField] private int iterations;
    Stack<TransformInfoHelper> stack = new Stack<TransformInfoHelper>();
    Stack<int> splineIndexStack = new Stack<int>();
    TransformInfoHelper helper;
    [SerializeField] private float length = 1.0f;
    [SerializeField] private float angle = 30.0f;
    [SerializeField] private float angleY = 15.0f;
    private List<List<Vector3>> LinesList = new List<List<Vector3>>();
    [SerializeField] private Material TreeMaterial;

    void Start()
    {
        this.transform.position = new Vector3(Random.Range(-10f, 15f), 0, Random.Range(-5f, 20f));
        tree = axiom;
        ExpandTreeString();
        CreateMesh();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (List<Vector3> line in LinesList)
        {
            if (line.Count == 2)
            {
                Gizmos.DrawLine(line[0], line[1]);
            }
        }
    }

    void ExpandTreeString()
    {
        string expandedTree;
        for (int i = 0; i < iterations; i++)
        {
            expandedTree = "";
            foreach (char j in tree)
            {
                switch (j)
                {
                    case 'F':
                        if (Random.Range(0f, 1f) < 0.5f)
                        {
                            expandedTree += "F";
                        }
                        else
                        {
                            expandedTree += "FF";
                        }
                        break;
                    case 'B':
                        if (Random.Range(0f, 1f) < 0.5f)
                        {
                            expandedTree += "[llFB][rFB]";
                        }
                        else
                        {
                            expandedTree += "[lFB][rrFB]";
                        }
                        break;
                    default:
                        expandedTree += j.ToString();
                        break;
                }
            }
            tree = expandedTree;
        }
    }

    void CreateMesh()
    {
        GameObject treeObject = new GameObject("Tree");
        var meshFilter = treeObject.AddComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        var meshRenderer = treeObject.AddComponent<MeshRenderer>();
        meshRenderer.material = TreeMaterial;

        var container = treeObject.AddComponent<SplineContainer>();
        container.RemoveSplineAt(0);
        var extrude = treeObject.AddComponent<SplineExtrude>();
        extrude.Container = container;

        var currentSpline = container.AddSpline();
        var splineIndex = container.Splines.FindIndex(currentSpline);

        currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);

        foreach (char j in tree)
        {
            switch (j)
            {
                case 'F':
                    transform.Translate(Vector3.up * length);
                    currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);
                    break;
                case 'B':
                    break;
                case '[':
                    stack.Push(new TransformInfoHelper
                    {
                        position = transform.position,
                        rotation = transform.rotation
                    });
                    splineIndexStack.Push(splineIndex);
                    int splineCount = currentSpline.Count;
                    int prevSplineIndex = splineIndex;
                    currentSpline = container.AddSpline();
                    splineIndex = container.Splines.FindIndex(currentSpline);
                    currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);
                    container.LinkKnots(new SplineKnotIndex(prevSplineIndex, splineCount - 1), new SplineKnotIndex(splineIndex, 0));
                    break;
                case ']':
                    TransformInfoHelper info = stack.Pop();
                    transform.position = info.position;
                    transform.rotation = info.rotation;
                    splineIndex = splineIndexStack.Pop();
                    currentSpline = container.Splines[splineIndex];
                    break;
                case 'l':
                    transform.Rotate(Vector3.back, angle);
                    transform.Rotate(Vector3.up, angleY);
                    break;
                case 'r':
                    transform.Rotate(Vector3.forward, angle);
                    transform.Rotate(Vector3.up, angleY);
                    break;
            }
        }
    }
}

public static class TreeGeneratorExtension
    {
        public static int FindIndex(this IReadOnlyList<Spline> splines, Spline spline)
        {
            for (int i = 0; i < splines.Count; i++)
            {
                if (splines[i] == spline)
                {
                    return i;
                }
            }
            return -1;
        }   
    }
