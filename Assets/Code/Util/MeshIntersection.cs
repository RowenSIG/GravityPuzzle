using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.UIElements;

//AI CODE


public static class MeshIntersection
{
    private const float EPSILON = 0.000001f;

    /// <summary>
    /// Checks intersection between a finite triangular face and a finite rectangular plane.
    /// Returns the points where the plane "cuts" the triangle.
    /// </summary>
    public static void GetTrianglePlaneIntersection( List<Vector3> outPoints, 
        Vector3 triV0, Vector3 triV1, Vector3 triV2, // Mesh Face
        Vector3 planeV0, Vector3 planeV1, Vector3 planeV2, Vector3 planeV3) // Finite Quad Plane
    {
        outPoints.Clear();

        // CheckEdgeAgainstFace(planeV0, planeV1, triV0, triV1, triV2, outPoints);
        // CheckEdgeAgainstFace(planeV1, planeV2, triV0, triV1, triV2, outPoints);
        // CheckEdgeAgainstFace(planeV2, planeV3, triV0, triV1, triV2, outPoints);
        // CheckEdgeAgainstFace(planeV3, planeV0, triV0, triV1, triV2, outPoints);
        
        CheckEdgeAgainstFace(triV0, triV1, planeV0, planeV1, planeV2, planeV3, outPoints);
        CheckEdgeAgainstFace(triV1, triV2, planeV0, planeV1, planeV2, planeV3, outPoints);
        CheckEdgeAgainstFace(triV2, triV0, planeV0, planeV1, planeV2, planeV3, outPoints);

    }

     private static void CheckEdgeAgainstFace(Vector3 L1, Vector3 L2, Vector3 v0, Vector3 v1, Vector3 v2, List<Vector3> results)
    {
        if (TryGetRayTriIntersection(L1, L2 - L1, v0, v1, v2, out Vector3 p, out float t))
        {
            if (t >= 0 && t <= 1 && !results.Contains(p)) results.Add(p);
        }
    }

    // Overload for Quad Face
    private static void CheckEdgeAgainstFace(Vector3 L1, Vector3 L2, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, List<Vector3> results)
    {
        // A quad is just two triangles (v0,v1,v2) and (v0,v2,v3)
        CheckEdgeAgainstFace(L1, L2, v0, v1, v2, results);
        CheckEdgeAgainstFace(L1, L2, v0, v2, v3, results);
    }

    private static bool IsPointInQuad(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // Simple boundary check: point must be on the 'inside' of all four quad edges
        return IsSameSide(p, v0, v1, v2) && IsSameSide(p, v1, v2, v3) &&
               IsSameSide(p, v2, v3, v0) && IsSameSide(p, v3, v0, v1);
    }

    private static bool IsSameSide(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 cp1 = Vector3.Cross(b - a, p - a);
        Vector3 cp2 = Vector3.Cross(b - a, c - a);
        return Vector3.Dot(cp1, cp2) >= 0;
    }

    private static bool TryGetRayTriIntersection(Vector3 origin, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 point, out float t)
    {
        point = Vector3.zero; t = 0;
        Vector3 edge1 = v1 - v0, edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(dir, edge2);
        float a = Vector3.Dot(edge1, h);
        if (a > -EPSILON && a < EPSILON) return false;

        float f = 1f / a;
        Vector3 s = origin - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0.0f || u > 1.0f) return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(dir, q);
        if (v < 0.0f || u + v > 1.0f) return false;

        t = f * Vector3.Dot(edge2, q);
        point = origin + dir * t;
        return true;
    }

    public static (List<int> tris, List<Vector3> verts) TidyMesh(List<int> tris, List<Vector3> verts)
    {
        Vector3 anyValidVert = verts[0];

        Dictionary<int, int> remapVertInts = new();

        //find alike verts:
        for(int i = 0 ; i < verts.Count; i ++)
        {
            var originalVert = verts[i];
            var tri = i;

            //is this the same vert as one later in the list?
            for(int j = verts.Count - 1; j > i; j--)
            {
                if(remapVertInts.ContainsKey(j))
                    continue;
                    
                var otherVert = verts[j];
                var otherTri = j;

                if((otherVert - originalVert).sqrMagnitude < 0.00001f)
                {
                    remapVertInts.Add(otherTri, tri);
                }
            }
        }

        //swap indices:
        for(int i = 0; i < tris.Count; i++)
        {
            var tri = tris[i];
            if(remapVertInts.TryGetValue(tri, out var newtri))
            {
                tris[i] = newtri;
            }
        }

        Dictionary<int, int> preserveVertInts = new();
        var newVerts = new List<Vector3>();
        for(int i = 0 ; i < verts.Count; i++)
        {
            // if(tris.Contains(i) == false)
            //     continue;

            preserveVertInts.Add(i, newVerts.Count);
            newVerts.Add(verts[i]);
        }

        //update any tri which was pointed at a vert to point at that same vert but its new point in the list
        for(int i = 0 ; i< tris.Count; i++)
        {
            var tri = tris[i];
            if(preserveVertInts.TryGetValue(tri, out var newIndex))
                tris[i] = newIndex;
        }

        //and finally, don't want repeated faces:
        List<int> newTris = new List<int>();

        var numFaces = tris.Count / 3; 
        for(int i = 0; i < numFaces; i++)
        {
            var tri0 = tris[0 + i * 3];
            var tri1 = tris[1 + i * 3];
            var tri2 = tris[2 + i * 3];

            bool duplicate = false;

            for(int j = 0 ; j < i ; j ++)
            {
                    
                var alreadyTri0 = tris[0 + j * 3];
                var alreadyTri1 = tris[1 + j * 3];
                var alreadyTri2 = tris[2 + j * 3];

                if(Same(tri0, tri1, tri2, alreadyTri0, alreadyTri1, alreadyTri2))
                    duplicate = true;
            }

            if(duplicate == false)
            {
                newTris.Add(tri0);
                newTris.Add(tri1);
                newTris.Add(tri2);
            }
        }

        return (newTris, newVerts);
    }

    private static bool Same(int tri0, int tri1, int tri2, int otherTri0, int otherTri1, int otherTri2)
    {
        if(tri0 == otherTri0 && tri1 == otherTri1 && tri2 == otherTri2)
            return true;
        if(tri0 == otherTri1 && tri1 == otherTri2 && tri2 == otherTri0)
            return true;
        if(tri0 == otherTri2 && tri1 == otherTri0 && tri2 == otherTri1)
            return true;
        return false;
    }
}

