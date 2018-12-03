using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BuildingSelector : MonoBehaviour {
    private BuildingIcon selected;
    public Camera camera;
    public LayerMask mask;
    public Material ghostMaterial;

    private Building ghost;

    private void Start() {
        selected = null;
    }

    private void Update() {
        if (selected != null) {
            if (Input.GetMouseButtonDown(1)) {
                UpdateSelection(null);
                return;
            }
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, 100, mask);
            var grid = WorldGrid.instance;
            var gridPos = grid.GridPos(hit.point - grid.RealVec(selected.buildingPrefab.size) / 2);
            var snapPosition = grid.RealPos(gridPos, 0, false);
            ghost.transform.position = snapPosition;
            var canPlace = grid.CanPlaceAt(gridPos, selected.buildingPrefab.size);
            ghostMaterial.color = canPlace ? Color.green : Color.red;
            
            if (Input.GetMouseButtonDown(0) && canPlace) {
                var building = Instantiate(selected.buildingPrefab);
                building.transform.position = snapPosition;
            }
        }
    }

    public void UpdateSelection(BuildingIcon select) {
        selected = select;
        if (ghost != null) {
            Destroy(ghost.gameObject);
            ghost = null;
        }
        if (selected != null) {
            ghost = Instantiate(selected.buildingPrefab);
            ghost.enabled = false;
            ghost.GetComponent<MeshRenderer>().material = ghostMaterial;
        }
    }
}
