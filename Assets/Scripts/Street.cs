using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Street {
    Vector3 pos;
    float dir;
    float roadWidth;
    List<Mesh> mesh;
    static Texture2D populationDensity;
    static List<Mesh> riverSegs;

    public Street(Vector3 pos, float dir, float roadWidth) {
        this.pos = pos;
        this.dir = dir;
        this.roadWidth = roadWidth;
        this.mesh = new List<Mesh>();
    }

    public Street(Vector3 pos, float dir, float roadWidth, Texture2D populationDensity) : this(pos, dir, roadWidth) {
        Street.populationDensity = populationDensity;
    }

    public Street growStreet(StreetQueue streets) {
        Street intersecting = streets.checkForIntersections(this);
        if (intersecting != null) {
            if (buildIntersection(this, intersecting)) streets.remove();
            return null;
        }
        if (bridgeRiver(checkRiverIntersect())) streets.remove();
        if (roadWidth == 4) growHighway(streets);
        else {
            mesh.Add(curvedRoadSegment((UnityEngine.Random.value - 0.5f) * 20));
            if (streets.checkForIntersections(this) != null || checkRiverIntersect() != null) mesh.Add(straightRoadSegment(5));
            else mesh.Add(straightRoadSegment(20 * roadWidth));
        }
        if (roadWidth < 4 && UnityEngine.Random.value < 0.02f) streets.remove();
        Debug.Log(populationDensity.GetPixel((int) pos.x, (int) pos.z).r);
        if (UnityEngine.Random.value < 0.2f / roadWidth) return new Street(new Vector3(pos.x, pos.y, pos.z), dir + (UnityEngine.Random.value > 0.5 ? 90 : -90), roadWidth);
        else if (roadWidth > 1 && (UnityEngine.Random.value < (0.1 * roadWidth + populationDensity.GetPixel((int) pos.x, (int) pos.z).r / 2f))) return new Street(new Vector3(pos.x, pos.y, pos.z), dir + (UnityEngine.Random.value > 0.5 ? 90 : -90), roadWidth / 2);
        else return null;
    }

    void growHighway(StreetQueue streets) {
        float maxDensity = 0;
        float maxDensityAngle = 0;
        for (float angle = -10; angle <= 10; angle += 5f) {
            Vector3 testPoint = extendRay(pos, dir + angle, 200);
            float popDens = populationDensity.GetPixel((int) testPoint.x, (int) testPoint.z).r;
            if (popDens > maxDensity) {
                maxDensity = popDens;
                maxDensityAngle = angle;
            }
        }
        mesh.Add(curvedRoadSegment(maxDensityAngle + (UnityEngine.Random.value - 0.5f) * 30));
        if (streets.checkForIntersections(this) != null || checkRiverIntersect() != null) mesh.Add(straightRoadSegment(5));
        else mesh.Add(straightRoadSegment(20 * roadWidth));
    }

    // returns whether or not the growing street should be deleted
    static bool buildIntersection(Street growing, Street intersected) {
        Mesh intersectedSeg = intersected.getSegment(intersected.checkIntersect(growing));
        float segAngle = calcVectorAngle(intersectedSeg.vertices[2] - intersectedSeg.vertices[0]);
        float angleDiff = Mathf.DeltaAngle(segAngle, growing.getDir());
        Vector3 segCenter = Vector3.Lerp(intersectedSeg.vertices[0], intersectedSeg.vertices[3], 0.5f);
        if ((angleDiff < 45 && angleDiff > -45) || angleDiff > 135 || angleDiff < -135) {
            growing.turnRoad(calcVectorAngle(segCenter - growing.getPos()) - growing.getDir());
            growing.extendRoad((segCenter - growing.getPos()).magnitude);
            return true;
        } else {
            Vector3 targetPoint = extendRay(segCenter, segAngle + (angleDiff > 0 ? -90 : 90), growing.getRoadWidth() * 10);
            float dAngle;
            float dist;
            if (growing.getRoadWidth() == 4) {
                if (UnityEngine.Random.value < 0.3) {
                    growing.turnRoad(calcVectorAngle(segCenter - growing.getPos()) - growing.getDir());
                    growing.extendRoad((segCenter - growing.getPos()).magnitude);
                    return true;
                }
                dAngle = calcVectorAngle(targetPoint - growing.getPos()) - growing.getDir();
                dist = (targetPoint - growing.getPos()).magnitude;
                int iter = 0;
                while (dist > 5 && iter < 10) {
                    growing.turnRoad(dAngle);
                    growing.extendRoad(Mathf.Min(dist, 40));
                    dAngle = calcVectorAngle(targetPoint - growing.getPos()) - growing.getDir();
                    dist = (targetPoint - growing.getPos()).magnitude;
                    iter++;
                }
                growing.turnRoad(calcVectorAngle(segCenter - growing.getPos()) - growing.getDir());
                growing.extendBridge();
            } else {
                if (growing.getRoadWidth() == intersected.getRoadWidth()) {
                    growing.turnRoad(calcVectorAngle(segCenter - growing.getPos()) - growing.getDir());
                    growing.extendRoad(growing.getRoadWidth() * 20);
                } else if (UnityEngine.Random.value < 0.65) {
                    growing.turnRoad(UnityEngine.Random.value < 0.5 ? -90 : 90);
                    growing.extendRoad(growing.getRoadWidth() * 20);
                } else return true;
            }
            return false;
        }
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

    Mesh straightRoadSegment(float length, float elevation) {
        Mesh segMesh = new Mesh();
        Vector3[] roadVerts = new Vector3[4];
        int[] roadTris = new int[6];
        roadVerts[0] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        roadVerts[1] = new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        roadVerts[2] = new Vector3(pos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + elevation, pos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)));
        roadVerts[3] = new Vector3(pos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + elevation, pos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)));
        roadTris[0] = 1;
        roadTris[1] = 2;
        roadTris[2] = 0;
        roadTris[3] = 1;
        roadTris[4] = 3;
        roadTris[5] = 2;
        segMesh.vertices = roadVerts;
        segMesh.triangles = roadTris;
        pos = new Vector3(pos.x + length * Mathf.Sin(Mathf.Deg2Rad * dir), pos.y + elevation, pos.z + length * Mathf.Cos(Mathf.Deg2Rad * dir));
        return segMesh;
    }

    Mesh straightRoadSegment(float length) {
        return straightRoadSegment(length, 0);
    }

    public void extendRoad(float length) {
        mesh.Add(straightRoadSegment(length));
    }

    // always length 80
    public void extendBridge() {
        Mesh bridgeMesh = new Mesh();
        CombineInstance[] bridgeParts = new CombineInstance[13];
        Vector3[] tri1verts = {new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                               new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                               new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)))};
        int[] tri1tris = {0, 1, 2};
        Vector3[] tri2verts = {new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                               new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                               new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)))};
        int[] tri2tris = {0, 2, 1};
        Vector3[] rect1verts = {new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)))};
        int[] rect1tris = {3, 2, 1, 3, 1, 0};
        Vector3[] supp1verts = {new Vector3(pos.x + 28 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + 28 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 28 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z + 28 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 32 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z + 32 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 32 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + 32 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)))};
        int[] supp1tris = {0, 1, 2, 0, 2, 3, 3, 2, 1, 3, 1, 0};
        Vector3[] supp2verts = {new Vector3(pos.x + 28 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + 28 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 28 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z + 28 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 32 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z + 32 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 32 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + 32 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)))};
        int[] supp2tris = {3, 2, 1, 3, 1, 0, 0, 1, 2, 0, 2, 3};
        Vector3[] supp3verts = {new Vector3(pos.x + 48 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + 48 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 48 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z + 48 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 52 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z + 52 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x + 52 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + 52 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)))};
        int[] supp3tris = {0, 1, 2, 0, 2, 3, 3, 2, 1, 3, 1, 0};
        Vector3[] supp4verts = {new Vector3(pos.x + 48 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + 48 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 48 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z + 48 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 52 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z + 52 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x + 52 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + 52 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)))};
        int[] supp4tris = {3, 2, 1, 3, 1, 0, 0, 1, 2, 0, 2, 3};
        bridgeParts[0].mesh = straightRoadSegment(20, 10);
        bridgeParts[1].mesh = straightRoadSegment(40, 0);
        bridgeParts[2].mesh = straightRoadSegment(20, -10);
        Vector3[] tri3verts = {new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                               new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                               new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)))};
        int[] tri3tris = {0, 2, 1};
        Vector3[] tri4verts = {new Vector3(pos.x + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                               new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                               new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90)))};
        int[] tri4tris = {0, 1, 2};
        Vector3[] rect2verts = {new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir - 90)), pos.y + 10, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir - 90))),
                                new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y + 10, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90))),
                                new Vector3(pos.x - 20 * Mathf.Sin(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Sin(Mathf.Deg2Rad * (dir + 90)), pos.y, pos.z - 20 * Mathf.Cos(Mathf.Deg2Rad * dir) + roadWidth * Mathf.Cos(Mathf.Deg2Rad * (dir + 90)))};
        int[] rect2tris = {0, 1, 2, 0, 2, 3};
        bridgeParts[3].mesh = new Mesh();
        bridgeParts[3].mesh.vertices = tri1verts;
        bridgeParts[3].mesh.triangles = tri1tris;
        bridgeParts[4].mesh = new Mesh();
        bridgeParts[4].mesh.vertices = tri2verts;
        bridgeParts[4].mesh.triangles = tri2tris;
        bridgeParts[5].mesh = new Mesh();
        bridgeParts[5].mesh.vertices = tri3verts;
        bridgeParts[5].mesh.triangles = tri3tris;
        bridgeParts[6].mesh = new Mesh();
        bridgeParts[6].mesh.vertices = tri4verts;
        bridgeParts[6].mesh.triangles = tri4tris;
        bridgeParts[7].mesh = new Mesh();
        bridgeParts[7].mesh.vertices = rect1verts;
        bridgeParts[7].mesh.triangles = rect1tris;
        bridgeParts[8].mesh = new Mesh();
        bridgeParts[8].mesh.vertices = rect2verts;
        bridgeParts[8].mesh.triangles = rect2tris;
        bridgeParts[9].mesh = new Mesh();
        bridgeParts[9].mesh.vertices = supp1verts;
        bridgeParts[9].mesh.triangles = supp1tris;
        bridgeParts[10].mesh = new Mesh();
        bridgeParts[10].mesh.vertices = supp2verts;
        bridgeParts[10].mesh.triangles = supp2tris;
        bridgeParts[11].mesh = new Mesh();
        bridgeParts[11].mesh.vertices = supp3verts;
        bridgeParts[11].mesh.triangles = supp3tris;
        bridgeParts[12].mesh = new Mesh();
        bridgeParts[12].mesh.vertices = supp4verts;
        bridgeParts[12].mesh.triangles = supp4tris;
        bridgeMesh.CombineMeshes(bridgeParts, true, false);
        mesh.Add(bridgeMesh);
    }

    public void turnRoad(float angle) {
        mesh.Add(curvedRoadSegment(angle));
    }

    public static Vector3 extendRay(Vector3 initPos, float dir, float dist) {
        return new Vector3(initPos.x + dist * Mathf.Cos(Mathf.Deg2Rad * (90 - dir)), initPos.y, initPos.z + dist * Mathf.Sin(Mathf.Deg2Rad * (90 - dir)));
    }

    public float getRoadWidth() {
        return roadWidth;
    }

    public Mesh getMesh() {
        Mesh fullStreet = new Mesh();
        CombineInstance[] segments = new CombineInstance[mesh.Count];
        for (int i = 0; i < mesh.Count; i++) {
            segments[i].mesh = mesh[i];
        }
        fullStreet.CombineMeshes(segments, true, false);
        return fullStreet;
    }

    public int getMeshSize() {
        return mesh.Count;
    }

    public Mesh getSegment(int ind) {
        if (ind < 0 || ind >= mesh.Count) return mesh[1];
        return mesh[ind];
    } 

    public Vector3 getPos() {
        return pos;
    }

    public float getDir() {
        return dir;
    }

    public string toString() {
        return pos + "/n" + dir;
    }

    public static float calcVectorAngle(Vector3 v) {
        if (v.z > 0) return Mathf.Rad2Deg * Mathf.Atan(v.x / v.z);
        return Mathf.Rad2Deg * -1 * Mathf.Atan(v.z / v.x) + (v.x  > 0 ? 90 : -90);
    }

    public int checkIntersect(Street extending) {
        float minDist = 2 << 20;
        int closestSegInd = -1;
        for (int i = 1; i < mesh.Count - (extending == this ? 1 : 0); i+= 2) {
            Mesh segment = mesh[i];
            float dist = 2 << 20;
            for (float j = 0; j <= 1; j += 0.2f) dist = Mathf.Min(Vector3.Distance(extending.getPos(), Vector3.Lerp(segment.vertices[0], segment.vertices[2], j)), dist);
            Vector3 checkpoint = extendRay(extending.getPos(), extending.getDir(), dist);
            if (((checkpoint.x > segment.vertices[0].x && checkpoint.x < segment.vertices[2].x) || (checkpoint.x < segment.vertices[0].x && checkpoint.x > segment.vertices[2].x)) && (checkpoint.z > segment.vertices[0].z && checkpoint.z < segment.vertices[2].z || checkpoint.z < segment.vertices[0].z && checkpoint.z > segment.vertices[2].z)
                    && dist < minDist) {
                minDist = dist;
                closestSegInd = i;
            }
        }
        return closestSegInd;
    }

    public Mesh checkRiverIntersect() {
        float minDist = 2 << 20;
        int closestSegInd = -1;
        for (int i = 0; i < riverSegs.Count; i+= 2) {
            Mesh segment = riverSegs[i];
            float dist = Vector3.Distance(getPos(), Vector3.Lerp(segment.vertices[0], segment.vertices[3], 0.5f));
            Vector3 checkpoint = extendRay(getPos(), getDir(), dist);
            if (((checkpoint.x > segment.vertices[0].x && checkpoint.x < segment.vertices[2].x) || (checkpoint.x < segment.vertices[0].x && checkpoint.x > segment.vertices[2].x)) && ((checkpoint.z > segment.vertices[0].z && checkpoint.z < segment.vertices[2].z) || (checkpoint.z < segment.vertices[0].z && checkpoint.z > segment.vertices[2].z))
                    && dist < minDist) {
                minDist = dist;
                closestSegInd = i;
            }
        }
        if (closestSegInd < 0 || minDist > 160) return null;
        return riverSegs[closestSegInd];
    }
        
    public bool bridgeRiver(Mesh intersectedSeg) {
        if (intersectedSeg == null) return false;
        float segAngle = calcVectorAngle(intersectedSeg.vertices[2] - intersectedSeg.vertices[0]);
        float angleDiff = Mathf.DeltaAngle(segAngle, getDir());
        if ((angleDiff < 45 && angleDiff > 0) || angleDiff < -135) {
            turnRoad(30);
            extendRoad(roadWidth * 20);
        } else if ((angleDiff > -45 && angleDiff < 0) || angleDiff > 135) {
            turnRoad(-30);
            extendRoad(roadWidth * 20);
        } else {
            Vector3 segCenter = Vector3.Lerp(intersectedSeg.vertices[0], intersectedSeg.vertices[3], 0.5f);
            Vector3 targetPoint = extendRay(segCenter, segAngle + (angleDiff > 0 ? -90 : 90), getRoadWidth() * 10);
            float dAngle;
            float dist;
            if (getRoadWidth() == 4) {
                dAngle = calcVectorAngle(targetPoint - getPos()) - getDir();
                dist = (targetPoint - getPos()).magnitude;
                int iter = 0;
                while (dist > 5 && iter < 10) {
                    turnRoad(dAngle);
                    extendRoad(Mathf.Min(dist, 40));
                    dAngle = calcVectorAngle(targetPoint - getPos()) - getDir();
                    dist = (targetPoint - getPos()).magnitude;
                    iter++;
                }
                turnRoad(calcVectorAngle(segCenter - getPos()) - getDir());
                extendBridge();
            } else if (UnityEngine.Random.value < 0.4) {
                turnRoad(UnityEngine.Random.value < 0.5 ? -90 : 90);
                extendRoad(getRoadWidth() * 20);
            } else return true;
        }
        return false;
    }

    public static float distToMesh(Street extending, Mesh intersecting) {
        float dist = 2 << 20;
        for (float i = 0; i <= 1; i += 0.2f) dist = Mathf.Min(Vector3.Distance(extending.getPos(), Vector3.Lerp(intersecting.vertices[0], intersecting.vertices[3], i)), dist);
        return dist;
    }

    public static void initRiverMesh(List<Mesh> river) {
        riverSegs = river;
    }
}