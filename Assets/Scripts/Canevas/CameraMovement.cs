using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {
    
    public float acceleration;
    public float rotAcceleration;
    public float smooth;
    public bool useMouse;
    private int pixelDelta = 15;
    private int pixelLimit = 100;

    private Vector3 velocity;
    private float angularVelocity;

    private void Update() {
        if (useMouse) {
            if (Input.mousePosition.x >= Screen.width - pixelDelta && Input.mousePosition.x <= Screen.width + pixelLimit)
                Move(transform.right);
            if (Input.mousePosition.x <= pixelDelta && Input.mousePosition.x >= -pixelLimit)
                Move(-transform.right);
            if (Input.mousePosition.y >= Screen.height - pixelDelta && Input.mousePosition.y <= Screen.height + pixelLimit)
                Move(transform.forward);
            if (Input.mousePosition.y <= pixelDelta && Input.mousePosition.y >= -pixelLimit)
                Move(-transform.forward);
        }
        
        Move(Input.GetAxis("Horizontal") * transform.right);
        Move(Input.GetAxis("Vertical") * transform.forward);
        angularVelocity += Input.GetAxis("Rotate") * Time.deltaTime * rotAcceleration;
        
        transform.position = Vector3.SmoothDamp(transform.position, transform.position, ref velocity, smooth);
        var rotation = transform.rotation.eulerAngles;
        rotation.y = rotation.y + angularVelocity * Time.deltaTime;
        angularVelocity -= 10 / smooth * Time.deltaTime * angularVelocity;
        transform.rotation = Quaternion.Euler(rotation);
    }

    private void Move(Vector3 direction) {
        velocity += direction * Time.deltaTime * acceleration;
    }
}
