using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingIcon : MonoBehaviour {
    public BuildingSelector buildingSelector;
    public Building buildingPrefab;
    public string message;
    Text text;

    private void Start() {
        GetComponent<Button>().onClick.AddListener(TaskOnClick);
        text = GetComponentInParent<RectTransform>().GetComponentInChildren<Text>();
    }

    private void TaskOnClick() {
        buildingSelector.UpdateSelection(this);
    }
}
