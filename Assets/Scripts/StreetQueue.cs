using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetQueue {
    LinkedList<Street> list;
    LinkedList<Street> deadList;
    int highways, deadHighways;

    public StreetQueue() {
        list = new LinkedList<Street>();
        deadList = new LinkedList<Street>();
        highways = 0;
        deadHighways = 0;
    }
    
    public void add(Street s) {
        if (s == null) return;
        if (s.getRoadWidth() == 4) highways++;
        if (list.Count == 0) {
            list.AddFirst(s);
            return;
        }
        LinkedListNode<Street> curr = list.First;
        int numRoadSize = 0;
        for (int i = 0; i < list.Count; i++) {
            if (curr.Value.getRoadWidth() == s.getRoadWidth()) numRoadSize++;
            if (curr.Value.getRoadWidth() < s.getRoadWidth()) {
                for (int j = 0; j < (int) UnityEngine.Random.value * numRoadSize; j++) curr = curr.Previous;
                list.AddBefore(curr, s);
                return;
            }
            curr = curr.Next;
        }
        list.AddLast(s);
    }

    public void remove() {
        deadList.AddFirst(list.First.Value);
        if (deadList.First.Value.getRoadWidth() == 4) deadHighways++;
        list.RemoveFirst();
    }

    public Street getDead(int index) {
        LinkedListNode<Street> curr = deadList.Last;
        if (index >= deadList.Count) return null;
        for (int i = 0; i < index; i++) curr = curr.Previous;
        return curr.Value;
    }

    public Street current() {
        return list.First.Value;
    }

    public Mesh CombineStreets() {
        Mesh streetMesh = new Mesh();
        Debug.Log(list.Count + deadList.Count);
        Debug.Log(highways);
        CombineInstance[] streets = new CombineInstance[list.Count + deadList.Count - highways];
        LinkedListNode<Street> curr = list.First;
        int count = 0;
        for (int i = 0; i < list.Count + deadList.Count; i++) {
            if (i == list.Count) curr = deadList.First;
            if (curr.Value.getRoadWidth() < 4) streets[count++].mesh = curr.Value.getMesh();
            curr = curr.Next;
        }
        streetMesh.CombineMeshes(streets, true, false);
        return streetMesh;
    }

    public Mesh CombineHighways() {
        Mesh streetMesh = new Mesh();
        CombineInstance[] streets = new CombineInstance[highways];
        LinkedListNode<Street> curr = list.First;
        int count = 0;
        for (int i = 0; i < list.Count + deadList.Count; i++) {
            if (i == list.Count) curr = deadList.First;
            if (curr.Value.getRoadWidth() == 4) streets[count++].mesh = curr.Value.getMesh();
            curr = curr.Next;
        }
        streetMesh.CombineMeshes(streets, true, false);
        return streetMesh;
    }

    public int getLiveStreets() {
        return list.Count;
    }

    public int getDeadStreets() {
        return deadList.Count;
    }

    public int getDeadHighways() {
        return deadHighways;
    }

    public int getTotalStreets() {
        return list.Count + deadList.Count;
    }

    public Street checkForIntersections(Street s) {
        float closestDist = 2 << 24;
        int closestMeshInd = -1;
        Street closestStreet = null;
        int meshInd;
        foreach (Street street in deadList) {
            meshInd = street.checkIntersect(s);
            if (meshInd > 0 && Street.distToMesh(s, street.getSegment(meshInd)) < closestDist) {
                closestDist = Street.distToMesh(s, street.getSegment(meshInd));
                closestMeshInd = meshInd;
                closestStreet = street;
            }
        }
        meshInd = s.checkIntersect(s);
            if (meshInd > 0 && Street.distToMesh(s, s.getSegment(meshInd)) < closestDist) {
                closestDist = Street.distToMesh(s, s.getSegment(meshInd));
                closestStreet = s;
            }
        if (closestDist < 200) return closestStreet;
        else return null;
    }

    public bool checkBuildingSpawnable(Vector3 pos, float dir, Mesh river) {
        float size = 20;
        float leftX = pos.x - 30 + Mathf.Min(Mathf.Min(Mathf.Min(size * Mathf.Cos(Mathf.Deg2Rad * (90 + dir + 60)), size * Mathf.Cos(Mathf.Deg2Rad * (90 + dir - 60))), size * Mathf.Cos(Mathf.Deg2Rad * (dir - 90 + 60))), size * Mathf.Cos(Mathf.Deg2Rad * (dir - 90 - 60)));
        float rightX = pos.x + 30 + Mathf.Max(Mathf.Max(Mathf.Max(size * Mathf.Cos(Mathf.Deg2Rad * (90 + dir + 60)), size * Mathf.Cos(Mathf.Deg2Rad * (90 + dir - 60))), size * Mathf.Cos(Mathf.Deg2Rad * (dir - 90 + 60))), size * Mathf.Cos(Mathf.Deg2Rad * (dir - 90 - 60)));
        float bottomZ = pos.z - 30 + Mathf.Min(Mathf.Min(Mathf.Min(size * Mathf.Sin(Mathf.Deg2Rad * (90 + dir + 60)), size * Mathf.Sin(Mathf.Deg2Rad * (90 + dir - 60))), size * Mathf.Sin(Mathf.Deg2Rad * (dir - 90 + 60))), size * Mathf.Sin(Mathf.Deg2Rad * (dir - 90 - 60)));
        float topZ = pos.z + 30 + Mathf.Max(Mathf.Max(Mathf.Max(size * Mathf.Sin(Mathf.Deg2Rad * (90 + dir + 60)), size * Mathf.Sin(Mathf.Deg2Rad * (90 + dir - 60))), size * Mathf.Sin(Mathf.Deg2Rad * (dir - 90 + 60))), size * Mathf.Sin(Mathf.Deg2Rad * (dir - 90 - 60)));
        foreach (Street street in deadList) {
            foreach (Vector3 point in street.getMesh().vertices) {
                if (point.x > leftX && point.x < rightX && point.z < topZ && point.z > bottomZ) return false;
            }
        }
        foreach (Vector3 point in river.vertices) {
            if (point.x > leftX && point.x < rightX && point.z < topZ && point.z > bottomZ) return false;
        }
        return true;
    }
}