using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetQueue {
    LinkedList<Street> list;
    LinkedList<Street> deadList;

    public StreetQueue() {
        list = new LinkedList<Street>();
        deadList = new LinkedList<Street>();
    }
    
    public void add(Street s) {
        if (s == null) return;
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
        CombineInstance[] streets = new CombineInstance[list.Count + deadList.Count];
        LinkedListNode<Street> curr = list.First;
        for (int i = 0; i < list.Count + deadList.Count; i++) {
            if (i == list.Count) curr = deadList.First;
            streets[i].mesh = curr.Value.getMesh();
            curr = curr.Next;
        }
        streetMesh.CombineMeshes(streets, true, false);
        return streetMesh;
    }

    public int getLiveStreets() {
        return list.Count;
    }

    public Street checkForIntersections(Street s) {
        foreach (Street check in deadList) {
            
        }
        return null;
    }

    public float checkIntersectSegment(Street extending, Vector3[] vertices) {
        float dist = 2 << 20;
        for (float i = 0; i <= 1; i += 0.2f) dist = Mathf.Min(Vector3.Distance(extending.getPos(), Vector3.Lerp(vertices[3], vertices[5], i)), dist);
        Vector3 checkpoint = extending.extendRay(extending.getPos(), extending.getDir(), dist);
        return ((checkpoint.x > vertices[3].x && checkpoint.x < vertices[5].x) || (checkpoint.x < vertices[3].x && checkpoint.x > vertices[5].x)) && (checkpoint.z > vertices[3].z && checkpoint.z < vertices[5].z || checkpoint.z < vertices[3].z && checkpoint.z > vertices[5].z) ? dist : -1;
    }
}