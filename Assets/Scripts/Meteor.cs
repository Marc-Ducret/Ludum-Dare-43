using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VoxelModel))]
[RequireComponent(typeof(Rigidbody))]
public class Meteor : MonoBehaviour {

    public Vector3 initialVelocity;
    public float radius;
    
    private VoxelModel model;
    
    private void Start() {
        model = GetComponent<VoxelModel>();
        GetComponent<Rigidbody>().velocity = initialVelocity;
    }

    private void Update() {
        if (transform.position.y < .1f) {
            foreach(var worker in FindObjectsOfType<Worker>())
                if (Vector3.ProjectOnPlane(worker.transform.position - transform.position, Vector3.up).sqrMagnitude <
                    radius * radius)
                    worker.Die("suffered your godly wrath");

            foreach (var b in WorldGrid.instance.buildings)
                for (var y = b.pos.y; y < b.pos.y + b.size.y; y++)
                    for (var x = b.pos.x; x < b.pos.x + b.size.x; x++)
                        if (Vector3.ProjectOnPlane(WorldGrid.instance.RealPos(new Vector2Int(x, y)) -
                                                   transform.position, Vector3.up).sqrMagnitude < radius * radius) {
                            b.GetComponent<VoxelModel>().Explode();
                            b.size = new Vector2Int(0, 0);
                        }
            model.Explode();
        }
    }
}
