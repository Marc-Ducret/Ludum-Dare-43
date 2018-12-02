using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateBody : MonoBehaviour {

    private Transform armLeft, armRight, legLeft, legRight;

    public float maxVelocity;
    public float smooth;
    public float animationSpeed;

    public Vector2 velocity;
    public float acting;
    public bool holding;
    
    private void Start() {
        var body = transform.Find("Body");
        armRight = body.Find("ArmRight");
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
        runIntensity = Mathf.Lerp(runIntensity, velocity.magnitude / maxVelocity, Time.deltaTime / smooth);
        runIntensity = Mathf.Min(1, runIntensity);

        var wave = Mathf.Sin(2 * Mathf.PI * Time.time * animationSpeed) * runIntensity;
        if (holding) {
            Rotate(armRight, -90, 0, 0);
            Rotate(armLeft, -90, 0, 0);
        } else {
            Rotate(armLeft , 0, 45,  wave);
            
            if (acting > 0) {
                Rotate(armRight, -65, 115, -Mathf.Sin(.75f * 2 * Mathf.PI * (1 - acting)));
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
    }
}
