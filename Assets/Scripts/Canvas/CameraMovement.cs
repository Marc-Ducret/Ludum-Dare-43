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
    private float zoom;

    private Camera cam;

    private void Start() {
        cam = GetComponentInChildren<Camera>();
    }

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
        angularVelocity += Input.GetAxis("Rotate") * Time.unscaledDeltaTime * rotAcceleration;
        zoom += Input.GetAxis("Mouse ScrollWheel");
        zoom = Mathf.Clamp(zoom, 0, .9f);
        
        transform.position = Vector3.SmoothDamp(transform.position, transform.position, ref velocity, smooth, 100,
            Time.unscaledDeltaTime);
        var rotation = transform.rotation.eulerAngles;
        rotation.y = rotation.y + angularVelocity * Time.unscaledDeltaTime;
        angularVelocity -= 10 / smooth * Time.unscaledDeltaTime * angularVelocity;
        transform.rotation = Quaternion.Euler(rotation);

        cam.transform.localPosition = Vector3.up *
                                 Mathf.Lerp(cam.transform.localPosition.y, (1 - zoom) * 35, Time.unscaledDeltaTime * 5 / smooth);
        cam.transform.localEulerAngles = Vector3.right *
                                    Mathf.Lerp(cam.transform.localEulerAngles.x, (1 - zoom) * 45, Time.unscaledDeltaTime * 5 / smooth);
    }

    private void Move(Vector3 direction) {
        velocity += direction * Time.unscaledDeltaTime * acceleration;
    }
}
