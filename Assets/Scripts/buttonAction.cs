using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class buttonAction : MonoBehaviour
{
    public buildingSelector bs; 
    public Button but;

    // Start is called before the first frame update
    void Start()
    {
        but.onClick.AddListener(TaskOnClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void TaskOnClick() {
        bs.UpdateSelection(null, false);
    }
}
