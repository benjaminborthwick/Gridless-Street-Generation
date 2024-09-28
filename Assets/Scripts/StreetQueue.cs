using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetQueue {
    LinkedList<Street> list;
    LinkedList<Street> deadList;
    int highways;

    public StreetQueue() {
        list = new LinkedList<Street>();
        deadList = new LinkedList<Street>();
        highways = 0;
    }
    
    public void add(Street s) {
        if (s == null) return;
        if (s.getRoadWidth() == 4) highways++;
        if (list.Count == 0) {
            list.AddFirst(s);
            return;
        }
        LinkedListNode<Street> curr = list.First;
        for (int i = 0; i < list.Count; i++) {
            if (curr.Value.getRoadWidth() < s.getRoadWidth()) {
                list.AddBefore(curr, s);
                return;
            }
            curr = curr.Next;
        }
        list.AddLast(s);
    }

    public void remove() {
        deadList.AddFirst(list.First.Value);
        list.RemoveFirst();
    }

    public Street current() {
        return list.First.Value;
    }

    public Mesh CombineStreets() {
        Mesh streetMesh = new Mesh();
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
}