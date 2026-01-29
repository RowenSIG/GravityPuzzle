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
        Vector3 planeV0, Vector3 planeV1, Vector3 planeV2, Vector3 planeV3) // Finite Quad Plane
    {
        outPoints.Clear();

        // 1. Check each tri edge against the plane
        CheckEdgeAgainstFace(triV0, triV1, planeV0, planeV1, planeV2, planeV3, outPoints);
        CheckEdgeAgainstFace(triV1, triV2, planeV0, planeV1, planeV2, planeV3, outPoints);
        CheckEdgeAgainstFace(triV2, triV0, planeV0, planeV1, planeV2, planeV3, outPoints);

        // 2. check each plane edge against the tri
        CheckEdgeAgainstFace(planeV0, planeV1, triV0, triV1, triV2, outPoints);
        CheckEdgeAgainstFace(planeV1, planeV2, triV0, triV1, triV2, outPoints);
        CheckEdgeAgainstFace(planeV2, planeV3, triV0, triV1, triV2, outPoints);
        CheckEdgeAgainstFace(planeV3, planeV0, triV0, triV1, triV2, outPoints);

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
}