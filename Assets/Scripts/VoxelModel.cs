using System;
using System.Collections.Generic;
using System.Globalization;
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
            ((color >> 16) & 0xFF) / (float) 0xFF,
            ((color >>  8) & 0xFF) / (float) 0xFF,
            ((color >>  0) & 0xFF) / (float) 0xFF
        );
    }

    public static readonly Vector3Int[] Axis = {Vector3Int.right, Vector3Int.up, new Vector3Int(0, 0, 1)};

    public Vector3Int TriangleVertex(int shift, int mask, bool invert) {
        Vector3Int offset;
        if (invert) {
            offset = Axis[(shift + 2) % 3] * ((mask >> 0) & 1) +
                     Axis[(shift + 1) % 3] * ((mask >> 4) & 1);
            offset = Vector3Int.one - offset;
        } else {
            offset = Axis[(shift + 1) % 3] * ((mask >> 0) & 1) +
                     Axis[(shift + 2) % 3] * ((mask >> 4) & 1);
            
        }
        return pos + offset;
    }
}

[RequireComponent(typeof(MeshFilter))]
public class VoxelModel : MonoBehaviour {
    private Mesh mesh;
    public TextAsset model;
    public bool horizontalCenter;
    public bool noDownFaces;
    private Voxel[,,] voxels;
    private List<Voxel> voxelsList;

    public Rigidbody particlePrefab;

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
                    int.Parse(tokens[2]),
                    int.Parse(tokens[1])
                ),
                int.Parse(tokens[3], NumberStyles.HexNumber)
            ));
        }

        var minBound = voxelsList[0].pos;
        var maxBound = voxelsList[0].pos;
        foreach (var voxel in voxelsList) {
            minBound = Vector3Int.Min(voxel.pos, minBound);
            maxBound = Vector3Int.Max(voxel.pos, maxBound);
        }

        size = maxBound - minBound + Vector3Int.one;
        voxelsList = voxelsList.ConvertAll(v => new Voxel(v.pos - minBound, v.color));
        voxels = new Voxel[size.x, size.y, size.z];
        foreach (var v in voxelsList) {
            voxels[v.pos.x, v.pos.y, v.pos.z] = v;
        }
    }

    private void GenerateMesh() {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();

        Func<Vector3Int, int> idx = vertex => {
            vertices.Add(vertex);
            return vertices.Count - 1;
        };

        var triangles = new List<int>();

        foreach (var v in voxelsList) {
            for (var sign = +1; sign >= -1; sign -= 2) {
                for (var shift = 0; shift < 3; shift++) {
                    if (shift == 1 && sign == 1 && noDownFaces) continue;
                    var dir = Voxel.Axis[shift] * -sign;
                    if (GetVoxel(v.pos + dir).color != new Color()) continue;
                    for (var i = 0; i < 4; i++) {
                        normals.Add(dir);
                        colors.Add(v.color);
                    }

                    var v00 = idx(v.TriangleVertex(shift, 0x00, sign < 0));
                    var v01 = idx(v.TriangleVertex(shift, 0x01, sign < 0));
                    var v11 = idx(v.TriangleVertex(shift, 0x11, sign < 0));
                    var v10 = idx(v.TriangleVertex(shift, 0x10, sign < 0));
                    triangles.Add(v00);
                    triangles.Add(v11);
                    triangles.Add(v01);
                    
                    triangles.Add(v00);
                    triangles.Add(v10);
                    triangles.Add(v11);
                }
            }
        }
        
        var offset = horizontalCenter ? Vector3.ProjectOnPlane(size, Vector3.up) / 2 : Vector3.zero;
        mesh.vertices = vertices.ConvertAll(v => v - offset).ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
    }

    [ContextMenu("Start")]
    private void Start() {
        mesh = new Mesh();
        mesh.Clear();
        ParseModel();
        GenerateMesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private static void SetMeshColor(Mesh mesh, Color color) {
        var colors = new Color[mesh.vertices.Length];
        for (var i = 0; i < colors.Length; i++) colors[i] = color;
        mesh.colors = colors;
    }

    [ContextMenu("Explode")]
    public void Explode() {
        const float force = 200f;
        var groundCenter = transform.position;
        var origin = transform.position;
        if (horizontalCenter) origin -= Vector3.ProjectOnPlane(size, Vector3.up) / 2;
        else groundCenter += Vector3.ProjectOnPlane(size, Vector3.up) / 2;
        foreach(var voxel in voxelsList) {
            var particle = Instantiate(
                particlePrefab,
                origin + transform.TransformVector(voxel.pos) + Vector3.one * .5F,
                transform.rotation
            );
            particle.AddExplosionForce(force, groundCenter, size.magnitude);
            SetMeshColor(particle.GetComponent<MeshFilter>().mesh, voxel.color);
            
            Destroy(particle.gameObject, 500);
        }
        Destroy(gameObject);
    }
}
