#define MERGE_VERTSx
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.VisualScripting;

//AI CODE


public static class MeshIntersection
{
    private const float EPSILON = 0.000001f;
    private const float BIG_EPSILON = 0.001f;

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
    public static (List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs) TidyMesh(List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, Plane? cutPlane = null)
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
        var numFaces = tris.Count / 3; 
        var newTris = new List<int>(tris.Count);
        for(int i = 0; i < numFaces; i++)
        {
            var tri0 = tris[0 + i * 3];
            var tri1 = tris[1 + i * 3];
            var tri2 = tris[2 + i * 3];

            bool remove = false;

            if(tri0 == tri1 || tri1 == tri2 || tri2 == tri0)
            {
                continue;
            }

            for(int j = 0 ; j < i ; j ++)
            {
                var alreadyTri0 = tris[0 + j * 3];
                var alreadyTri1 = tris[1 + j * 3];
                var alreadyTri2 = tris[2 + j * 3];

                if (Same(tri0, tri1, tri2, alreadyTri0, alreadyTri1, alreadyTri2))
                {
                    remove = true;
                    break;
                }
            }

            if (remove)
                continue;

            //check zero area:
            var vert0 = verts[tri0];
            var vert1 = verts[tri1];
            var vert2 = verts[tri2];

            if (Same(vert0, vert1, vert2))
                continue;

            //check cutPlane Side:
            if (cutPlane.HasValue)
            {
                if (WrongSide(cutPlane.Value, vert0, vert1, vert2))
                {
                    Debug.Log("Tidy mesh Found bad vert");
                    remove = true;
                }
            }

            if (remove)
                continue;

            newTris.Add(tri0);
            newTris.Add(tri1);
            newTris.Add(tri2);
        }

        //can we now adjust our list of tris to merge tris which share verts:
        AttemptToMergeTris(newTris, verts);

        Dictionary<int, int> preserveVertInts = new();
        var newVerts = new List<Vector3>(verts.Count);
        var newNormals = new List<Vector3>(verts.Count);
        var newUVs = new List<Vector2>(verts.Count);
        for (int i = 0; i < verts.Count; i++)
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

        newTris.RemoveAll( p => p < 0 );
        
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

    public static bool Same(Vector3 vert0, Vector3 vert1, Vector3 vert2)
    {
        //for now:
        if( (vert0 - vert1).sqrMagnitude < vertEpsilonSquared )
                return true;
        if( (vert1 - vert2).sqrMagnitude < vertEpsilonSquared )
            return true;
        if( (vert2 - vert0).sqrMagnitude < vertEpsilonSquared )
            return true;

        return false;
    }

    private struct MergeFace
    {
        public int index, tri0, tri1, tri2;
    }
    private struct MergeEdge
    {
        public int tri0, tri1;
        public MergeEdge(int triA, int triB)
        {
            tri0 = triA < triB ? triA : triB;
            tri1 = triA < triB ? triB : triA;
        }
    }
    private struct MergeFacePair
    {
        public MergeEdge edge;
        public MergeFace face0;
        public MergeFace face1;
    }
    private struct MergeFaceResult
    {
        public MergeFace newFace;
        public MergeFace face0;
        public MergeFace face1;
    }

    private static Dictionary<MergeEdge, List<MergeFace>> edgeDic = new();

    private static void AttemptToMergeTris(List<int> tris, List<Vector3> verts)
    {
        edgeDic.Clear();

        int numFaces = tris.Count / 3;
        List<MergeFace> faces = new List<MergeFace>(numFaces);
        
        //we build our mergetris:
        for(int i = 0 ; i < numFaces; i++)
        {
            int tri0 = tris[0 + i * 3];
            int tri1 = tris[1 + i * 3];
            int tri2 = tris[2 + i * 3];
            var merge0 = new MergeFace() { index = i, tri0 = tri0, tri1 = tri1, tri2 = tri2 };
            faces.Add(merge0);
        }

        //according to ai, we can build an edge map - a dic which tells us all the faces which share certain edges:
        for(int i = 0 ; i < faces.Count; i++)
        {
            var face = faces[i];
            var edge0 = new MergeEdge(face.tri0 , face.tri1);
            var edge1 = new MergeEdge(face.tri1 , face.tri2);
            var edge2 = new MergeEdge(face.tri2 , face.tri0);

            Add(edge0, face);
            Add(edge1, face);
            Add(edge2, face);
        }

        var mergeables = new List<MergeFacePair>();
        //and now we can check every single edge to see if it has 2 tris in its list:
        foreach(var edge in edgeDic.Keys)
        {
            var faceList = edgeDic[edge];
            if(faceList.Count == 2)
            {
                mergeables.Add( new MergeFacePair() { face0 = faceList[0] , face1 = faceList[1] , edge = edge });
            }
        }

        Debug.Log($"[MeshIntersection] [AttemptToMergeTris] - tris[{tris.Count}] verts[{verts.Count}] - faces[{faces.Count}] edges[{edgeDic.Count}] -> mergeables[{mergeables.Count}]");

        var merges = new List<MergeFaceResult>();
        foreach(var mergeable in mergeables)
        {
            var face0 = mergeable.face0;
            var face1 = mergeable.face1;
            if(Coplanar(face0, face1, verts) == false)
                continue;

            var edge = mergeable.edge;
            var face0tipTri = GetTip(face0, edge);
            var face1tipTri = GetTip(face1, edge);

            //at least one of our edge's points should be directly between our tips:
            var edge0OnLine = PointOnSegment(verts[edge.tri0], verts[face0tipTri], verts[face1tipTri]);
            var edge1OnLine = PointOnSegment(verts[edge.tri1], verts[face0tipTri], verts[face1tipTri]);

            if(edge0OnLine == false && edge1OnLine == false)
                continue;

            //apparently we can now build 2 faces:
            var newFace0 = new MergeFace() { tri0 = face0tipTri, tri1 = edge.tri0, tri2 = face1tipTri };
            
            bool matched = false;
            if(Coplanar(face0, newFace0, verts) && SameWinding(face0, newFace0, verts))
            {
                matched = true;
            }
            else 
            {
                newFace0 = new MergeFace() { tri0 = face0tipTri, tri1 = edge.tri1, tri2 = face1tipTri };
                if(Coplanar(face0, newFace0, verts) && SameWinding(face0, newFace0, verts))
                {
                    matched = true;
                }
            }

            if(matched)
            {
                var result = new MergeFaceResult() { face0 = face0, face1 = face1, newFace = newFace0 };
                merges.Add(result);
            }
        }
        
        List<int> merged = new List<int>();
        int notMerged = 0;
        foreach(var mergeFaceResult in merges)
        {
            var face0 = mergeFaceResult.face0;
            var face1 = mergeFaceResult.face1;
            var newFace0 = mergeFaceResult.newFace;

            if(merged.Contains(face0.index) || merged.Contains(face1.index))
            {
                notMerged += 1;
                continue;
            }

            //we now implant our new face in place of our face 0 and set face 1 to all -1's
            var triIndex0 = 0 + face0.index * 3;
            var triIndex1 = 1 + face0.index * 3;
            var triIndex2 = 2 + face0.index * 3;

            tris[triIndex0] = newFace0.tri0;
            tris[triIndex1] = newFace0.tri1;
            tris[triIndex2] = newFace0.tri2;

            var removeIndex0 = 0 + face1.index * 3;
            var removeIndex1 = 1 + face1.index * 3;
            var removeIndex2 = 2 + face1.index * 3;

            tris[removeIndex0] = -1;
            tris[removeIndex1] = -1;
            tris[removeIndex2] = -1;

            merged.Add(face0.index);
            merged.Add(face1.index);

        }

        Debug.Log($"[MeshIntersection] [AttemptToMergeTris] countremoved[{merges.Count}] notmerged[{notMerged}]");

    }

    private static void Add(MergeEdge edge, MergeFace face)
    {
        if (edgeDic.TryGetValue(edge, out var list) == false)
        {
            list = new List<MergeFace>();
            edgeDic[edge] = list;
        }
        list.Add(face);
    }

    private static bool Coplanar(MergeFace baseFace, MergeFace testFace, List<Vector3> verts)
    {
        Vector3 a = verts[baseFace.tri0];
        Vector3 b = verts[baseFace.tri1];
        Vector3 c = verts[baseFace.tri2];

        Plane p = new Plane(a, b, c);

        Vector3 t0 = verts[testFace.tri0];
        Vector3 t1 = verts[testFace.tri1];
        Vector3 t2 = verts[testFace.tri2];

        return Mathf.Abs(p.GetDistanceToPoint(t0)) < EPSILON &&
               Mathf.Abs(p.GetDistanceToPoint(t1)) < EPSILON &&
               Mathf.Abs(p.GetDistanceToPoint(t2)) < EPSILON;
    }

    private static bool SameWinding(
        MergeFace baseFace,
        MergeFace testFace,
        List<Vector3> verts)
    {
        Vector3 a0 = verts[baseFace.tri0];
        Vector3 b0 = verts[baseFace.tri1];
        Vector3 c0 = verts[baseFace.tri2];

        Vector3 originalNormal = Vector3.Cross(b0 - a0, c0 - a0).normalized;

        Vector3 a = verts[testFace.tri0];
        Vector3 b = verts[testFace.tri1];
        Vector3 c = verts[testFace.tri2];

        Vector3 newNormal = Vector3.Cross(b - a, c - a).normalized;

        return Vector3.Dot(originalNormal, newNormal) > 0f;
    }

    private static bool PointOnSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;

        // Collinearity
        if (Vector3.Cross(ab, ap).sqrMagnitude > EPSILON)
            return false;

        // Projection inside segment
        float dot = Vector3.Dot(ap, ab);
        if (dot < 0f) 
            return false;
        if (dot > ab.sqrMagnitude)
             return false;

        return true;
    }


    private static int GetTip(MergeFace face, MergeEdge edge)
    {
        if (face.tri0 != edge.tri0 && face.tri0 != edge.tri1)
            return face.tri0;

        if (face.tri1 != edge.tri0 && face.tri1 != edge.tri1)
            return face.tri1;

        return face.tri2;
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

    private static bool WrongSide(Plane plane, Vector3 vert0, Vector3 vert1, Vector3 vert2)
    {
        if (plane.GetDistanceToPoint(vert0) > BIG_EPSILON)
            return true;
        if (plane.GetDistanceToPoint(vert1) > BIG_EPSILON)
            return true;
        if (plane.GetDistanceToPoint(vert2) > BIG_EPSILON)
            return true;
        return false;
    }


}

