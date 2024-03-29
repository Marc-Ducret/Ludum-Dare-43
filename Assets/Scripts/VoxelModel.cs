﻿using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Random = UnityEngine.Random;

public struct Voxel {
    public Vector3Int pos;
    public readonly Color color;
    public int depth;

    public Voxel(Vector3Int pos, Color color) {
        this.pos = pos;
        this.color = color;
        this.depth = -1;
    }

    public static readonly Vector3Int[] Axis = { Vector3Int.right, Vector3Int.up, new Vector3Int(0, 0, 1) };

    public Vector3Int TriangleVertex(int shift, int mask, bool invert) {
        Vector3Int offset;
        if (invert) {
            offset = Axis[(shift + 2) % 3] * ((mask >> 0) & 1) +
                     Axis[(shift + 1) % 3] * ((mask >> 4) & 1);
            offset = Vector3Int.one - offset;
        }
        else {
            offset = Axis[(shift + 1) % 3] * ((mask >> 0) & 1) +
                     Axis[(shift + 2) % 3] * ((mask >> 4) & 1);

        }
        return pos + offset;
    }
}

[RequireComponent(typeof(MeshFilter))]
public class VoxelModel : MonoBehaviour {
    public TextAsset model;
    public bool horizontalCenter;
    public bool noDownFaces;
    public float colorNoise;

    public Rigidbody particlePrefab;

    private Mesh mesh;

    public Vector3Int Size { get; private set; }
    public Voxel[,,] Voxels { get; private set; }
    public List<Voxel> VoxelsList { get; private set; }

    private float explodeDuration = 5f;

    private Voxel GetVoxel(Vector3Int pos) {
        if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= Size.x || pos.y >= Size.y || pos.z >= Size.z)
            return new Voxel();
        return Voxels[pos.x, pos.y, pos.z];
    }

    private void ParseModel() {
        VoxelsList = new List<Voxel>();
        foreach (var line in model.text.Split('\n')) {
            if (line.StartsWith("#")) continue;
            var tokens = line.Split(' ');
            if (tokens.Length < 4) continue;
            VoxelsList.Add(new Voxel(
                new Vector3Int(
                    int.Parse(tokens[0]),
                    int.Parse(tokens[2]),
                    int.Parse(tokens[1])
                ),
                RandomizeColor(int.Parse(tokens[3], NumberStyles.HexNumber))
            ));
        }

        var minBound = VoxelsList[0].pos;
        var maxBound = VoxelsList[0].pos;
        foreach (var voxel in VoxelsList) {
            minBound = Vector3Int.Min(voxel.pos, minBound);
            maxBound = Vector3Int.Max(voxel.pos, maxBound);
        }

        Size = maxBound - minBound + Vector3Int.one;
        VoxelsList = VoxelsList.ConvertAll(v => new Voxel(v.pos - minBound, v.color));
        Voxels = new Voxel[Size.x, Size.y, Size.z];
        foreach (var v in VoxelsList) {
            Voxels[v.pos.x, v.pos.y, v.pos.z] = v;
        }

        ComputeDepth();
    }

    private Color RandomizeColor(int color) {
        var noise = colorNoise + 3e-2f;
        Func<float, float, float> colorComp = (x, n) => Mathf.Clamp(x + (Random.value - .5f) * 2 * n, 0, 1);
        float h, s, v;
        Color.RGBToHSV(
            new Color(
                ((color >> 16) & 0xFF) / (float)0xFF,
                ((color >> 8 ) & 0xFF) / (float)0xFF,
                ((color >> 0 ) & 0xFF) / (float)0xFF
            ), 
            out h, out s, out v);
        
        return Color.HSVToRGB(colorComp(h, noise * .1f), colorComp(s, noise), colorComp(v, noise));
    }

    public void GenerateMesh(int maxDepth = -1) {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();

        Func<Vector3Int, int> idx = vertex => {
            vertices.Add(vertex);
            return vertices.Count - 1;
        };

        var triangles = new List<int>();

        foreach (var voxel in VoxelsList) {
            if (maxDepth >= 0 && voxel.depth > maxDepth) break;

            for (var sign = +1; sign >= -1; sign -= 2) {
                for (var shift = 0; shift < 3; shift++) {
                    if (shift == 1 && sign == 1 && noDownFaces) continue;
                    var dir = Voxel.Axis[shift] * -sign;
                    var neighbour = GetVoxel(voxel.pos + dir);
                    if ((maxDepth < 0 || neighbour.depth <= maxDepth) && neighbour.color.a > 0) continue;
                    
                    for (var i = 0; i < 4; i++) {
                        normals.Add(dir);
                        colors.Add(voxel.color);
                    }

                    var v00 = idx(voxel.TriangleVertex(shift, 0x00, sign < 0));
                    var v01 = idx(voxel.TriangleVertex(shift, 0x01, sign < 0));
                    var v11 = idx(voxel.TriangleVertex(shift, 0x11, sign < 0));
                    var v10 = idx(voxel.TriangleVertex(shift, 0x10, sign < 0));
                    triangles.Add(v00);
                    triangles.Add(v11);
                    triangles.Add(v01);

                    triangles.Add(v00);
                    triangles.Add(v10);
                    triangles.Add(v11);
                }
            }
        }

        var offset = horizontalCenter ? Vector3.ProjectOnPlane(Size, Vector3.up) / 2 : Vector3.zero;
        mesh.Clear();
        mesh.vertices = vertices.ConvertAll(v => v - offset).ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
    }

    [ContextMenu("Awake")]
    private void Awake() {
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
        const float force = 400f;
        const float maxPieces = 100;
        var groundCenter = transform.position;
        var origin = transform.position;
        var offset = transform.TransformVector(Vector3.ProjectOnPlane(Size, Vector3.up) / 2);
        if (horizontalCenter) origin -= offset;
        else groundCenter += offset;
        foreach (var voxel in VoxelsList) {
            if (Random.value > maxPieces / VoxelsList.Count) continue;
            var particle = Instantiate(
                particlePrefab,
                origin + transform.TransformVector(voxel.pos + Vector3.one * .5F),
                transform.rotation
            );
            particle.transform.localScale = transform.lossyScale;
            particle.AddExplosionForce(force, groundCenter, Size.magnitude);
            SetMeshColor(particle.GetComponent<MeshFilter>().mesh, voxel.color);

            Destroy(particle.gameObject, explodeDuration * (1 + Random.value));
        }
        Destroy(gameObject);
    }

    public void ComputeDepth() {
        Queue<Voxel> toVisit = new Queue<Voxel>();
        List<Voxel> visited = new List<Voxel>();
        for (int i = 0; i < VoxelsList.Count; i++) {
            Voxel v = VoxelsList[i];
            if (v.pos.y == 0) {
                v.depth = 0;
                toVisit.Enqueue(v);
                visited.Add(v);
                Voxels[v.pos.x, v.pos.y, v.pos.z] = v;
            }
        }

        while (toVisit.Count > 0) {
            Voxel v = toVisit.Dequeue();
            for (var sign = +1; sign >= -1; sign -= 2) {
                for (var shift = 0; shift < 3; shift++) {
                    var dir = Voxel.Axis[shift] * sign;
                    var nextPos = v.pos + dir;
                    var next = GetVoxel(nextPos);
                    if (next.color.a == 0) continue;
                    if (next.depth >= 0) continue;

                    next.depth = v.depth + 1;
                    toVisit.Enqueue(next);
                    visited.Add(next);
                    Voxels[next.pos.x, next.pos.y, next.pos.z] = next;
                }
            }
        }

        foreach (var v in VoxelsList)
            if(Voxels[v.pos.x, v.pos.y, v.pos.z].depth < 0)
                visited.Add(v);

        VoxelsList = visited;
    }

    public int MaxDepth() {
        return VoxelsList[VoxelsList.Count - 1].depth;
    }
}
