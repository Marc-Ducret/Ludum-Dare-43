using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SpeedButton : MonoBehaviour {

    public float gameSpeed;

    private void Start() {
        GetComponent<Button>().onClick.AddListener(TaskOnClick);
    }

    private void TaskOnClick() {
        Time.timeScale = gameSpeed;
    }
}
