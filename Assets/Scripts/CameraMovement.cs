using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class CameraMovement : MonoBehaviour
{
    public float speed = 5.0f;
    private int pixelDelta = 15;
    private int pixelLimit = 100;
    
    void Start()
    {
        
    }

    void Update()
    {
        //Right
        if ((Input.mousePosition.x >= Screen.width - pixelDelta) && (Input.mousePosition.x <= Screen.width + pixelLimit))
        {
            Move(Vector3.right);
            //Right + Forward
            if ((Input.mousePosition.y >= Screen.height - pixelDelta) && (Input.mousePosition.y <= Screen.height + pixelLimit))
            {
                Move(Vector3.forward);
            }
            //Right + Back
            else if ((Input.mousePosition.y <= pixelDelta) && (Input.mousePosition.y >= -pixelLimit))
            {
                Move(Vector3.back);
            }
        }
        //Left
        else if ((Input.mousePosition.x <= pixelDelta) && (Input.mousePosition.x >= -pixelLimit))
        {
            Move(Vector3.left);
            //Left + Forward
            if ((Input.mousePosition.y >= Screen.height - pixelDelta) && (Input.mousePosition.y <= Screen.height + pixelLimit))
            {
                Move(Vector3.forward);
            }
            //Left + Back
            else if ((Input.mousePosition.y <= pixelDelta) && (Input.mousePosition.y >= -pixelLimit))
            {
                Move(Vector3.back);
            }
        }
        //Forward
        else if ((Input.mousePosition.y >= Screen.height - pixelDelta) && (Input.mousePosition.y <= Screen.height + pixelLimit))
        {
            Move(Vector3.forward);
        }
        //Back
        else if ((Input.mousePosition.y <= pixelDelta) && (Input.mousePosition.y >= -pixelLimit))
        {
            Move(Vector3.back);
        }
    }
    
    void Move(Vector3 direction)
    {
        transform.position += direction * Time.deltaTime * speed;
    }
}
