using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class CameraMovement : MonoBehaviour
{
    private float speed = 3.0f;
    private int pixelDelta = 15;
    private int pixelLimit = 100;
    
    void Start()
    {
        
    }

    void Update()
    {
        if ((Input.mousePosition.x >= Screen.width - pixelDelta) && (Input.mousePosition.x <= Screen.width + pixelLimit))
        {
            transform.position += Vector3.right * Time.deltaTime * speed;
        }
        else if ((Input.mousePosition.x <= pixelDelta) && (Input.mousePosition.x >= -pixelLimit))
        {
            transform.position += Vector3.left * Time.deltaTime * speed;
        }
        else if ((Input.mousePosition.y >= Screen.height - pixelDelta) && (Input.mousePosition.y <= Screen.height + pixelLimit))
        {
            transform.position += Vector3.forward * Time.deltaTime * speed;
        }
        else if ((Input.mousePosition.y <= pixelDelta) && (Input.mousePosition.y >= -pixelLimit))
        {
            transform.position += Vector3.back * Time.deltaTime * speed;
        }
    }
}
