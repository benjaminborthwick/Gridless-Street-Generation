using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetGenerator : MonoBehaviour
{
    [SerializeField]
    private int seed = 1;

    [SerializeField]
    private float dirModifier = 0.75f;
    private float dir = 0;
    private float segLength = 0;
    
    [SerializeField]
    private int riverSegs = 15;
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(seed);

        // generate river that passes through (0, 0)
        Mesh riverMesh = new Mesh();
        Vector3[] riverVerts = new Vector3[riverSegs * 2];
        int[] riverTris = new int[riverSegs * 6];
        riverVerts[0] = new Vector3(0, 0, -2);
        riverVerts[1] = new Vector3(0, 0, 2);
        riverTris[0] = 1;
        riverTris[1] = 0;
        riverTris[2] = 2;
        riverTris[3] = 1;
        riverTris[4] = 2;
        riverTris[5] = 3;
        for (int i = 2; i < riverSegs * 2 + 2; i++) {
            if (i % 2 == 0) {
                dir += randDirMod();
                segLength = 5 + UnityEngine.Random.value * 5;
                if (i < riverSegs * 2) {
                    riverTris[i * 3] = i + 1;
                    riverTris[i * 3 + 1] = i;
                    riverTris[i * 3 + 2] = i + 2;
                    riverTris[i * 3 + 3] = i + 1;
                    riverTris[i * 3 + 4] = i + 2;
                    riverTris[i * 3 + 5] = i + 3;
                }
            } 
            riverVerts[i] = nextCurveVert(riverVerts[i - 2]);
            print(riverVerts[i]);
        }
        riverMesh.vertices = riverVerts;
        riverMesh.triangles = riverTris;
        riverMesh.RecalculateNormals();

        GameObject river = new GameObject("River");
        river.AddComponent<MeshFilter>();
        river.AddComponent<MeshRenderer>();
        river.GetComponent<MeshFilter>().mesh = riverMesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float bias(float r, float b) {
        return r /((1 / b - 2) * (1 - r) + 1);
    }

    // returns a random angle between -90 and 90, biased by the dirModifier constant.
    float randDirMod() {
        return 180 * ((dirModifier < 0.5 ? bias(2 * UnityEngine.Random.value, 1 - dirModifier) / 2 : 1 - bias(2 - 2 * UnityEngine.Random.value, 1 - dirModifier)) - 0.5f);
    }

    Vector3 nextCurveVert(Vector3 prev) {
        return new Vector3(prev.x + segLength * Mathf.Sin(Mathf.Deg2Rad * dir), prev.y, prev.z + segLength * Mathf.Cos(Mathf.Deg2Rad * dir));
    }
}
