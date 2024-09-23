using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Street {
    Vector3 pos;
    float dir;
    float roadWidth;
    float length;
    Mesh mesh;
    static Texture2D populationDensity;

    public Street(Vector3 pos, float dir, float roadWidth) {
        this.pos = pos;
        this.dir = dir;
        this.roadWidth = roadWidth;
        this.length = 20 * roadWidth;
        this.mesh = straightRoadSegment();
    }

    public Street(Vector3 pos, float dir, float roadWidth, Texture2D populationDensity) : this(pos, dir, roadWidth) {
        Street.populationDensity = populationDensity;
    }

    public Street growStreet() {
        if (roadWidth == 2) growHighway();
        else {

        }

        if (UnityEngine.Random.value < 0.2f / (roadWidth * roadWidth)) {
            return new Street(new Vector3(pos.x, pos.y, pos.z), dir + UnityEngine.Random.value > 0.5 ? 90 : -90, roadWidth);
        }
        else return null;
    }

    Mesh growHighway() {
        float maxDensity = 0;
        float maxDensityAngle = 0;
        for (float angle = -30; angle <= 30; angle += 7.5f) {
            Vector3 testPoint = extendRay(pos, dir + angle, 100);
            float popDens = populationDensity.GetPixel((int) testPoint.x, (int) testPoint.z).r;
            if (popDens > maxDensity) {
                maxDensity = popDens;
                maxDensityAngle = angle;
            }
        }
        Mesh road = new Mesh();
        CombineInstance[] roadSegs = new CombineInstance[3];
        roadSegs[0].mesh = curvedRoadSegment(maxDensityAngle + (UnityEngine.Random.value - 0.5f) * 20);
        roadSegs[1].mesh = straightRoadSegment();
        roadSegs[2].mesh = mesh;
        road.CombineMeshes(roadSegs, true, false);
        mesh = road;
        return road;
    }

    Mesh curvedRoadSegment(float angle) {
        Mesh cornerMesh = new Mesh();
        Vector3[] roadVerts = new Vector3[3];
        int[] roadTris = new int[3];
        roadVerts[0] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        roadVerts[1] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        float extSegLength = Mathf.Sqrt(8 * roadWidth * roadWidth * (1 - Mathf.Cos(Mathf.Deg2Rad * angle)));
        if (angle < 0) roadVerts[2] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)) + extSegLength * Mathf.Cos(Mathf.Deg2Rad * ((180 - angle) / 2 - dir)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)) + extSegLength * Mathf.Sin(Mathf.Deg2Rad * ((180 - angle) / 2 - dir)));
        else roadVerts[2] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)) + extSegLength * Mathf.Cos(Mathf.Deg2Rad * ((180 - angle) / 2 - dir)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)) + extSegLength * Mathf.Sin(Mathf.Deg2Rad * ((180 - angle) / 2 - dir)));
        roadTris[0] = 0;
        roadTris[1] = 1;
        roadTris[2] = 2;
        cornerMesh.vertices = roadVerts;
        cornerMesh.triangles = roadTris;
        if (angle < 0) pos = new Vector3((roadVerts[1].x + roadVerts[2].x) / 2, pos.y, (roadVerts[1].z + roadVerts[2].z) / 2);
        else pos = new Vector3((roadVerts[0].x + roadVerts[2].x) / 2, pos.y, (roadVerts[0].z + roadVerts[2].z) / 2);
        dir += angle;
        return cornerMesh;
    }

    Mesh straightRoadSegment() {
        Mesh segMesh = new Mesh();
        Vector3[] roadVerts = new Vector3[4];
        int[] roadTris = new int[6];
        roadVerts[0] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        roadVerts[1] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        roadVerts[2] = new Vector3(pos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), 0, pos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        roadVerts[3] = new Vector3(pos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), 0, pos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        roadTris[0] = 1;
        roadTris[1] = 2;
        roadTris[2] = 0;
        roadTris[3] = 1;
        roadTris[4] = 3;
        roadTris[5] = 2;
        segMesh.vertices = roadVerts;
        segMesh.triangles = roadTris;
        segMesh.RecalculateNormals();
        pos = new Vector3(pos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir), pos.y, pos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir));
        return segMesh;
    }

    Vector3 extendRay(Vector3 initPos, float dir, float dist) {
        return new Vector3(initPos.x + dist * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)), initPos.y, initPos.z + dist * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)));
    }

    public float getRoadWidth() {
        return roadWidth;
    }

    public Mesh getMesh() {
        return mesh;
    }

    public Vector3 getPos() {
        return pos;
    }

    public string toString() {
        return pos + "/n" + dir;
    }

}