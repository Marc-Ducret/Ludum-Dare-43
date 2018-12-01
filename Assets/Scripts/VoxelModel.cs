using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public struct Voxel {
    public Vector3Int pos;
    public readonly Color color;
    
    public Voxel(Vector3Int pos, Color color) {
        this.pos = pos;
        this.color = color;
    }

    public Voxel(Vector3Int pos, int color) {
        this.pos = pos;
        this.color = new Color(
            ((color >>  0) & 0xFF) / (float) 0xFF,
            ((color >>  8) & 0xFF) / (float) 0xFF,
            ((color >> 16) & 0xFF) / (float) 0xFF
        );
    }

    public Vector3Int[] ListVertices() {
        var vertices = new Vector3Int[8];
        var count = 0;
        for (var dx = 0; dx <= 1; dx++) {
            for (var dy = 0; dy <= 1; dy++) {
                for (var dz = 0; dz <= 1; dz++) {
                    vertices[count++] = pos + new Vector3Int(dx, dy, dz);
                }
            }
        }

        return vertices;
    }

    public static Vector3Int[] axis = {Vector3Int.right, Vector3Int.up, new Vector3Int(0, 0, 1)};

    public Vector3Int TriangleVertex(int shift, int mask, bool invert) {
        Vector3Int offset;
        if (invert) {
            offset = axis[(shift + 2) % 3] * ((mask >> 0) & 1) +
                     axis[(shift + 1) % 3] * ((mask >> 4) & 1);
            offset = Vector3Int.one - offset;
        } else {
            offset = axis[(shift + 1) % 3] * ((mask >> 0) & 1) +
                     axis[(shift + 2) % 3] * ((mask >> 4) & 1);
            
        }
        return pos + offset;
    }
}

[RequireComponent(typeof(MeshFilter))]
public class VoxelModel : MonoBehaviour {
    private Mesh mesh;
    public TextAsset model;
    private Voxel[,,] voxels;
    private List<Voxel> voxelsList;

    private Vector3Int size;

    private Voxel GetVoxel(Vector3Int pos) {
        if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= size.x || pos.y >= size.y || pos.z >= size.z)
            return new Voxel();
        return voxels[pos.x, pos.y, pos.z];
    }

    private void ParseModel() {
        voxelsList = new List<Voxel>();
        foreach (var line in model.text.Split('\n')) {
            if (line.StartsWith("#")) continue;
            var tokens = line.Split(' ');
            if (tokens.Length < 4) continue;
            voxelsList.Add(new Voxel(
                new Vector3Int(
                    int.Parse(tokens[0]),
                    int.Parse(tokens[1]),
                    int.Parse(tokens[2])
                ),
                int.Parse(tokens[3], NumberStyles.HexNumber)
            ));
        }

        var minBound = new Vector3Int();
        var maxBound = new Vector3Int();
        foreach (var voxel in voxelsList) {
            minBound = Vector3Int.Min(voxel.pos, minBound);
            maxBound = Vector3Int.Max(voxel.pos, maxBound);
        }

        size = maxBound - minBound + Vector3Int.one;
        Debug.Log("Size = " + size);
        voxelsList = voxelsList.ConvertAll(v => v = new Voxel(v.pos - minBound, v.color));
        voxels = new Voxel[size.x, size.y, size.z];
        foreach (var v in voxelsList) {
            voxels[v.pos.x, v.pos.y, v.pos.z] = v;
        }

        Debug.Log("Model Parsed");
    }

    private void GenerateMesh() {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();
        Debug.Log("Vertices Indexed");

        Func<Vector3Int, int> idx = vertex => {
            vertices.Add(vertex);
            return vertices.Count - 1;
        };

        var triangles = new List<int>();

        foreach (var v in voxelsList) {
            for (var sign = +1; sign >= -1; sign -= 2) {
                for (var shift = 0; shift < 3; shift++) {
                    var dir = Voxel.axis[shift] * -sign;
                    if (GetVoxel(v.pos + dir).color != new Color()) continue;
                    for (var i = 0; i < 6; i++) {
                        normals.Add(dir);
                        print(v.color);
                        colors.Add(v.color);
                    }
                    triangles.Add(idx(v.TriangleVertex(shift, 0x00, sign < 0)));
                    triangles.Add(idx(v.TriangleVertex(shift, 0x11, sign < 0)));
                    triangles.Add(idx(v.TriangleVertex(shift, 0x01, sign < 0)));

                    triangles.Add(idx(v.TriangleVertex(shift, 0x00, sign < 0)));
                    triangles.Add(idx(v.TriangleVertex(shift, 0x10, sign < 0)));
                    triangles.Add(idx(v.TriangleVertex(shift, 0x11, sign < 0)));
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
        Debug.Log("Mesh Generated");
    }

    [ContextMenu("Start")]
    private void Start() {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        ParseModel();
        GenerateMesh();
    }

    void Update() {
    }
}
