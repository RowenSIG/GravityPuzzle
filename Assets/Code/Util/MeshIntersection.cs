#define MERGE_VERTSx
using UnityEngine;
using System.Collections.Generic;

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
        Vector3 planeV0, Vector3 planeV1, Vector3 planeV2, Vector3 planeV3) 
    {
        outPoints.Clear();

        CheckEdgeAgainstFace(planeV0, planeV1, triV0, triV1, triV2, outPoints);
        CheckEdgeAgainstFace(planeV1, planeV2, triV0, triV1, triV2, outPoints);
        CheckEdgeAgainstFace(planeV2, planeV3, triV0, triV1, triV2, outPoints);
        CheckEdgeAgainstFace(planeV3, planeV0, triV0, triV1, triV2, outPoints);

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

    private static readonly float vertEpsilonSquared = (0.001f * 0.001f);
    public static (List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs) TidyMesh(List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs)
    {

#if MERGE_VERTS
        Dictionary<int, int> remapVertInts = new(verts.Count);
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

                if((otherVert - originalVert).sqrMagnitude < vertEpsilonSquared)
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
#endif

        //and finally, don't want repeated faces:
        var newTris = new List<int>(tris.Count);

        var numFaces = tris.Count / 3; 
        for(int i = 0; i < numFaces; i++)
        {
            var tri0 = tris[0 + i * 3];
            var tri1 = tris[1 + i * 3];
            var tri2 = tris[2 + i * 3];

            bool duplicate = false;

            if(tri0 == tri1 || tri1 == tri2 || tri2 == tri0)
            {
                continue;
            }

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

        Dictionary<int, int> preserveVertInts = new();
        var newVerts = new List<Vector3>(verts.Count);
        var newNormals = new List<Vector3>(verts.Count);
        var newUVs = new List<Vector2>(verts.Count);
        for(int i = 0 ; i < verts.Count; i++)
        {
            if(newTris.Contains(i) == false)
                continue;

            preserveVertInts.Add(i, newVerts.Count);
            newVerts.Add(verts[i]);
            newNormals.Add(normals[i]);
            newUVs.Add(uvs[i]);
        }

        //update any tri which was pointed at a vert to point at that same vert but its new point in the list
        for(int i = 0 ; i < newTris.Count; i++)
        {
            var tri = newTris[i];
            if(preserveVertInts.TryGetValue(tri, out var newIndex))
                newTris[i] = newIndex;
        }

        return (newTris, newVerts, newNormals, newUVs);
    }

    private static bool Same(int tri0, int tri1, int tri2, int otherTri0, int otherTri1, int otherTri2)
    {
        //can't be a proper face if two tris are the same
        if (tri0 == tri1 || tri1 == tri2 || tri2 == tri0)
            return true;

        if (tri0 == otherTri0 && tri1 == otherTri1 && tri2 == otherTri2)
            return true;
        if (tri0 == otherTri1 && tri1 == otherTri2 && tri2 == otherTri0)
            return true;
        if (tri0 == otherTri2 && tri1 == otherTri0 && tri2 == otherTri1)
            return true;
        return false;
    }

    public static Vector2 GetUV(Vector3 point, Vector3 vert0, Vector3 vert1, Vector3 vert2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
    {
        // Compute vectors
        Vector3 v0 = vert1 - vert0;
        Vector3 v1 = vert2 - vert0;
        Vector3 v2 = point - vert0;

        // Compute dot products
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);

        // Compute barycentric coordinates
        float denom = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1f - v - w;

        // Interpolate UV
        return u * uv0 + v * uv1 + w * uv2;

    }

    public static Vector3 NormaliseVertsByCenterOfMass(List<Vector3> verts)
    {
        //simple right? get center.. that's it
        Vector3 total = Vector3.zero;
        foreach(var vert in verts)
        {
            total += vert;
        }
        var offset = total / verts.Count;
        for(int i = 0 ; i < verts.Count; i++)
        {
            verts[i] = verts[i] - offset;
        }
        return offset;
    }
    public static List<int> TriangulateEarClipping(IList<Vector2> poly)
    {
        List<int> result = new List<int>();
        int n = poly.Count;

        if (n < 3)
            return result;

        // Working index list
        List<int> V = new List<int>(n);
        for (int i = 0; i < n; i++)
            V.Add(i);

        int guard = 0;

        while (V.Count > 3 && guard < 5000)
        {
            guard++;
            bool earFound = false;

            for (int i = 0; i < V.Count; i++)
            {
                int prev = V[(i - 1 + V.Count) % V.Count];
                int curr = V[i];
                int next = V[(i + 1) % V.Count];

                Vector2 a = poly[prev];
                Vector2 b = poly[curr];
                Vector2 c = poly[next];

                // Must be convex
                if (!IsConvex(a, b, c))
                    continue;

                // Check if any other point lies inside the triangle
                bool containsPoint = false;
                for (int j = 0; j < V.Count; j++)
                {
                    int vi = V[j];
                    if (vi == prev || vi == curr || vi == next)
                        continue;

                    if (PointInTriangle(poly[vi], a, b, c))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (containsPoint)
                    continue;

                // It's an ear
                result.Add(prev);
                result.Add(curr);
                result.Add(next);

                V.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                Debug.LogWarning("Ear clipping failed — polygon may be malformed");
                break;
            }
        }

        // Final triangle
        if (V.Count == 3)
        {
            result.Add(V[0]);
            result.Add(V[1]);
            result.Add(V[2]);
        }

        return result;
    }

    static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        return Cross(b - a, c - b) > 0f; // CCW convex
    }

    static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Cross(b - a, c - a);
        float s = Cross(c - a, p - a) / area;
        float t = Cross(a - b, p - b) / area;
        float u = 1 - s - t;
        return s >= 0 && t >= 0 && u >= 0;
    }

    public static float ComputeSignedArea(IList<Vector2> poly)
    {
        float area = 0f;

        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % poly.Count];

            area += (a.x * b.y) - (b.x * a.y);
        }

        return area * 0.5f;
    }
}

