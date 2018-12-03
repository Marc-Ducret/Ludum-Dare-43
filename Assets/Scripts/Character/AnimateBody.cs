using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateBody : MonoBehaviour {

    private Transform armLeft, armRight, legLeft, legRight, harvestHolder;
    private Transform harvest;

    public float maxVelocity;
    public float smooth;
    public float animationSpeed;

    public Vector2 velocity;
    public float verticalOffset;
    
    private float acting;
    private float actDuration;
    private int nHits;
    private bool holding;

    public bool IsActing {
        get { return acting > 0; }
    }

    public Transform[] harvestPrefabs;
    
    private void Start() {
        var body = transform.Find("Body");
        armRight = body.Find("ArmRight");
        harvestHolder = armRight.Find("Harvest");
        armLeft  = body.Find("ArmLeft");
        legRight = body.Find("LegRight");
        legLeft  = body.Find("LegLeft");
    }

    private float runIntensity;

    private static void Rotate(Transform part, float center, float amplitude, float cur) {
        part.localRotation = Quaternion.Slerp(part.localRotation,
            Quaternion.AngleAxis(center + amplitude * cur, Vector3.right), Time.deltaTime / .2f);
    }
    
    private void Update() {
        var gridPos = WorldGrid.instance.GridPos(transform.position);
        verticalOffset = WorldGrid.instance.cells[gridPos.y, gridPos.x].isRoad ? WorldGrid.instance.scale / 8 : 0;
        
        runIntensity = Mathf.Lerp(runIntensity, velocity.magnitude / maxVelocity, Time.deltaTime / smooth);
        runIntensity = Mathf.Min(1, runIntensity);

        var wave = Mathf.Sin(2 * Mathf.PI * Time.time * animationSpeed) * runIntensity;
        if (holding) {
            Rotate(armRight, -90, 0, 0);
            Rotate(armLeft, -90, 0, 0);
        } else {
            Rotate(armLeft , 0, 45,  wave);
            
            if (IsActing) {
                Rotate(armRight, -65, 115, -Mathf.Sin((nHits - .25f) * 2 * Mathf.PI * (actDuration - acting) / actDuration));
                acting = Mathf.Max(0, acting - Time.deltaTime);
            } else {
                Rotate(armRight, -55, 15, -wave);
            }
        }
        
        Rotate(legLeft , 0, 45, -wave);
        Rotate(legRight, 0, 45,  wave);

        if (velocity.sqrMagnitude > 1e-6) {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, 
                Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.y).normalized, Vector3.up), 
                Time.deltaTime / smooth
            );
        }

        var y = Mathf.Lerp(Vector3.Dot(transform.position, Vector3.up), verticalOffset, Time.deltaTime / smooth);
        transform.position = Vector3.ProjectOnPlane(transform.position, Vector3.up) + y * Vector3.up;
    }

    public void Hold(Resource res) {
        Drop();
        harvest = Instantiate(harvestPrefabs[(int) res], harvestHolder);
        holding = true;
    }

    public void Drop() {
        if (harvest != null) {
            Destroy(harvest.gameObject);
            harvest = null;
        }
        holding = false;
    }

    public void Act(float duration, int hits) {
        acting = duration;
        actDuration = duration;
        nHits = hits;
    }
}
