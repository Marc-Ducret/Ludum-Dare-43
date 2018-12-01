using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageTraitement : MonoBehaviour {
    public Button but;
    public string batName;
    public BuildingSelector bs; 

    // Start is called before the first frame update
    void Start() {
        
        but.onClick.AddListener(TaskOnClick);  
        //Faire que l'on puisse ecouter aussi le click droit 
            
            
    }

    // Update is called once per frame
    void Update() {
    }

    void TaskOnClick() {
        Debug.Log("Click on " + batName);
        bs.UpdateSelection(batName, true);
        //Mettre le script pour placer un batiment 

    }
}
