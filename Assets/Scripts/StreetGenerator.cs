using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetGenerator : MonoBehaviour
{
    [SerializeField]
    private int seed = 1;

    [SerializeField]
    private float dirModifier = 0.75f;
    private float currDir = 0;
    private Vector3 currRiverPos;
    private float riverWidth = 2;
    [SerializeField]
    private int numSegs = 100;
    [SerializeField]
    private int iterationDepth = 100;

    public int texture_width = 496;
	public int texture_height = 496;
	public float scale = 5;

    public static Texture2D populationDensity;
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(seed);
        // generate river that passes through (0, 0)
        // No current bounds control for river, so is capable of going off of population map depending on seed.
        Mesh riverMesh = new Mesh();
        CombineInstance[] riverSegs = new CombineInstance[numSegs * 2];
        currRiverPos = new Vector3(0, 0, 0);
        float segLength = 0;
        float dir = 0;
        for (int i = 0; i < numSegs; i++) {
            segLength = 5 + UnityEngine.Random.value * 20;
            dir = UnityEngine.Random.value * 60 - 30;
            riverSegs[i * 2].mesh = straightRiverSegment(segLength, currRiverPos, currDir);
            riverSegs[i * 2 + 1].mesh = curvedRiverSegment(dir);
        }
        riverMesh.CombineMeshes(riverSegs, true, false);

        GameObject river = new GameObject("River");
        river.AddComponent<MeshFilter>();
        river.AddComponent<MeshRenderer>();
        river.GetComponent<MeshFilter>().mesh = riverMesh;

        river.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.8f, 1.0f);

        // generating an initial highway, beginning on a different side of the pop map
        GameObject streets = new GameObject("Streets");
        streets.AddComponent<MeshFilter>();
        streets.AddComponent<MeshRenderer>();
        populationDensity = make_a_texture();
        print(populationDensity);
        Mesh streetMesh = new Mesh();
        CombineInstance[] streetSegs = new CombineInstance[numSegs * 2];
        StreetQueue streetBuds = new StreetQueue();
        streetBuds.add(new Street(new Vector3(-496, 0, 100 + UnityEngine.Random.value * 700), 90, 2, populationDensity));
        int iter = 0;
        while (iter < iterationDepth && streetBuds.getLiveStreets() > 0) {
            streetBuds.add(streetBuds.current().growStreet());
            if (checkOutOFBounds(streetBuds.current().getPos())) streetBuds.remove();
            iter++;
        }
        streets.GetComponent<MeshFilter>().mesh = streetBuds.CombineStreets();
        streets.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 0.4f, 1.0f);
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
        return new Vector3(0,0,0);//prev.x + segLength * Mathf.Sin(Mathf.Deg2Rad * dir), prev.y, prev.z + segLength * Mathf.Cos(Mathf.Deg2Rad * dir));
    }

    Mesh curvedRiverSegment(float angle) {
        Mesh cornerMesh = new Mesh();
        Vector3[] riverVerts = new Vector3[3];
        int[] riverTris = new int[3];
        riverVerts[0] = new Vector3(currRiverPos.x + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (currDir + 90)), currRiverPos.y, currRiverPos.z + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (currDir + 90)));
        riverVerts[1] = new Vector3(currRiverPos.x + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (currDir - 90)), currRiverPos.y, currRiverPos.z + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (currDir - 90)));
        float extSegLength = Mathf.Sqrt(8 * riverWidth * riverWidth * (1 - Mathf.Cos(Mathf.Deg2Rad * angle)));
        if (angle < 0) riverVerts[2] = new Vector3(currRiverPos.x + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (currDir + 90)) + extSegLength * Mathf.Cos(Mathf.Deg2Rad * ((180 - angle) / 2 - currDir)), currRiverPos.y, currRiverPos.z + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (currDir + 90)) + extSegLength * Mathf.Sin(Mathf.Deg2Rad * ((180 - angle) / 2 - currDir)));
        else riverVerts[2] = new Vector3(currRiverPos.x + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (currDir - 90)) + extSegLength * Mathf.Cos(Mathf.Deg2Rad * ((180 - angle) / 2 - currDir)), currRiverPos.y, currRiverPos.z + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (currDir - 90)) + extSegLength * Mathf.Sin(Mathf.Deg2Rad * ((180 - angle) / 2 - currDir)));
        riverTris[0] = 0;
        riverTris[1] = 1;
        riverTris[2] = 2;
        cornerMesh.vertices = riverVerts;
        cornerMesh.triangles = riverTris;
        if (angle < 0) currRiverPos = new Vector3((riverVerts[1].x + riverVerts[2].x) / 2, currRiverPos.y, (riverVerts[1].z + riverVerts[2].z) / 2);
        else currRiverPos = new Vector3((riverVerts[0].x + riverVerts[2].x) / 2, currRiverPos.y, (riverVerts[0].z + riverVerts[2].z) / 2);
        currDir += angle;
        return cornerMesh;
    }

    Mesh straightRiverSegment(float length, Vector3 startPos, float dir) {
        Mesh segMesh = new Mesh();
        Vector3[] riverVerts = new Vector3[4];
        int[] riverTris = new int[6];
        riverVerts[0] = new Vector3(startPos.x + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), startPos.y, startPos.z + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        riverVerts[1] = new Vector3(startPos.x + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), startPos.y, startPos.z + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        riverVerts[2] = new Vector3(startPos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir) + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), 0, startPos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir) + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        riverVerts[3] = new Vector3(startPos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir) + riverWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), 0, startPos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir) + riverWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        riverTris[0] = 1;
        riverTris[1] = 2;
        riverTris[2] = 0;
        riverTris[3] = 1;
        riverTris[4] = 3;
        riverTris[5] = 2;
        segMesh.vertices = riverVerts;
        segMesh.triangles = riverTris;
        segMesh.RecalculateNormals();
        currRiverPos = new Vector3(startPos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir), startPos.y, startPos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir));
        return segMesh;
    }

    bool checkOutOFBounds(Vector3 pos) {
        return pos.x < -1 * texture_width || pos.x > texture_width || pos.z < 0 || pos.z > texture_height * 2;
    }

    Texture2D make_a_texture() {

		// create the texture and an array of colors that will be copied into the texture
		Texture2D texture = new Texture2D (texture_width, texture_height);
		Color[] colors = new Color[texture_width * texture_height];
        float offsetX = UnityEngine.Random.value * 1000;
        float offsetY = UnityEngine.Random.value * 1000;
		// create the Perlin noise pattern in "colors"
		for (int i = 0; i < texture_width; i++)
			for (int j = 0; j < texture_height; j++) {
				float x = scale * i / (float) texture_width;
				float y = scale * j / (float) texture_height;
				float t = Mathf.PerlinNoise (x + offsetX, y + offsetY);                          // Perlin noise!
				colors [j * texture_width + i] = new Color (t, t, t, 1.0f);  // gray scale values (r = g = b)
			}

		// copy the colors into the texture
		texture.SetPixels(colors);

		// do texture specific stuff, probably including making the mipmap levels
		texture.Apply();
		print(texture);
		// return the texture
		return (texture);
	}
}
