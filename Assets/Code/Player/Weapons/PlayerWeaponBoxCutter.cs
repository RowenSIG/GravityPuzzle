using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

public class PlayerWeaponBoxCutter : PlayerWeapon
{
    [SerializeField]
    private Transform boxCenterTransform;

    private struct TriCutPoint
    {
        public int subMesh;

        public int triIndex0;
        public int triIndex1;
        public int triIndex2;

        public int tri0;
        public int tri1;
        public int tri2;

        public ePlane plane;

        public Plane unityPlane;

        public Vector3 localCutPos0;
        public Vector3 localCutPos1;

        public int pointCount;
    }
    private Collider currentTargetCollider = null;

    private List<Vector3> lastFrameCastPoints = new(32);
    private List<TriCutPoint> lastFrameTriCutPoints = new(64);

    private RaycastHit[] castHitBuffer = new RaycastHit[32];
    private List<int> triBuffer = new (65535);
    private List<Vector3> vertsBuffer = new(65535);
    private List<Vector3> intersectionPointBuffer = new(4); //should be max 2

    private Bounds localBoxBounds;

    public float width;
    public float height;
    //depth is fixed
    public float depth = 10f;

    private bool canFire = false;
    private bool CanFire
    {
        get
        {
            return canFire;
        }
    }

    public override void UpdateWeapon(float deltaTime, bool leftFire, bool rightFire)
    {
        DebugChecking();

        if(CanFire && rightFire)
        {
            Fire();
        }
        
        if(rightFire == false)
        {
            canFire = true;
        }
    }

    private void DebugChecking()
    {
        lastFrameCastPoints.Clear();
        lastFrameTriCutPoints.Clear();
        
        //we want to cut whatever we see in front of us (just the first thing) 
        var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var rayOrigin = ray.origin; 


        int ignoreLayer = LayerMask.NameToLayer("Player");
        int layerMask = ~(1 << ignoreLayer);

        var boxExtents = new Vector3(width, height, 0.01f);
        var numHits = Physics.BoxCastNonAlloc(rayOrigin, boxExtents, ray.direction, castHitBuffer, transform.rotation, depth, layerMask);

        if(numHits > 0)
        {
            float nearestDist = Mathf.Infinity;
            RaycastHit nearestHit = default;
            //find nearest:
            for(int i = 0 ; i < numHits; i++)
            {
                var hit = castHitBuffer[i];
                var delta = hit.point - transform.position;
                if(delta.sqrMagnitude < nearestDist)
                {
                    nearestDist = delta.sqrMagnitude;
                    nearestHit = hit;
                }
            }

            //then we want to build our 'planes' for each of our forward pointing edges
            var collider = nearestHit.collider;
            currentTargetCollider = collider;
            var meshFilter = collider.GetComponent<MeshFilter>();
            if(meshFilter != null)
            {
                CastAllPlanesAgainstMesh(meshFilter);
            }
        }
        else
        {
            currentTargetCollider = null;
        }

        var localCenter = transform.InverseTransformPoint(player.PlayerCamera.transform.position);
        localCenter += Vector3.forward * depth /2f;
        var localSize = new Vector3(width, height, depth);
        localBoxBounds = new Bounds(localCenter, localSize);
    }

    private void CastAllPlanesAgainstMesh(MeshFilter meshFilter)
    {
        var meshWorldMatrix = meshFilter.transform.worldToLocalMatrix;
        var meshLocalMatrix = meshFilter.transform.localToWorldMatrix;
        var mesh = meshFilter.mesh;

        vertsBuffer.Clear();
        triBuffer.Clear();

        CastPlaneAgainstMesh(ePlane.TOP, mesh, meshWorldMatrix, meshLocalMatrix);
        CastPlaneAgainstMesh(ePlane.BOTTOM, mesh, meshWorldMatrix, meshLocalMatrix);
        CastPlaneAgainstMesh(ePlane.LEFT, mesh, meshWorldMatrix, meshLocalMatrix);
        CastPlaneAgainstMesh(ePlane.RIGHT, mesh, meshWorldMatrix, meshLocalMatrix);
    }

    private void CastPlaneAgainstMesh(ePlane plane, Mesh mesh, Matrix4x4 meshWorldMatrix, Matrix4x4 meshLocalMatrix)
    {
        var worldPlane = GetPlane(plane);
        var localBackA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backA);
        var localBackb = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backB);
        var localFrontA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontA);
        var localFrontB = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontB);

        //cache this?
        var numSubmeshes = mesh.subMeshCount;
        for(int i = 0 ; i < numSubmeshes; i++)
        {
            mesh.GetTriangles(triBuffer, i);
            mesh.GetVertices(vertsBuffer);

            int numTriangles = triBuffer.Count / 3;

            for(int j = 0 ; j < numTriangles; j ++)
            {
                var triIndex0 = 0 + j * 3;
                var triIndex1 = 1 + j * 3;
                var triIndex2 = 2 + j * 3;
                var tri0 = triBuffer[triIndex0];
                var tri1 = triBuffer[triIndex1];
                var tri2 = triBuffer[triIndex2];

                var point0 = vertsBuffer[tri0];
                var point1 = vertsBuffer[tri1];
                var point2 = vertsBuffer[tri2];

                MeshIntersection.GetTrianglePlaneIntersection(intersectionPointBuffer,
                point0, point1, point2,
                localBackA, localBackb, localFrontB, localFrontA);

                if (intersectionPointBuffer.Count > 0)
                {
                    if(intersectionPointBuffer.Count != 2)
                    {
                       // Log($"intersectionCount not 2 [{intersectionPointBuffer.Count}]");
                       continue;
                    }
                    foreach(var localIntersectionPoint in intersectionPointBuffer)
                    {
                        var worldIntersectionPoint = meshLocalMatrix.MultiplyPoint3x4(localIntersectionPoint);
                        lastFrameCastPoints.Add(worldIntersectionPoint);
                    }

                    var triCut = new TriCutPoint();
                    triCut.subMesh = i;
                    triCut.triIndex0 = triIndex0;
                    triCut.triIndex1 = triIndex1;
                    triCut.triIndex2 = triIndex2;
                    triCut.tri0 = tri0;
                    triCut.tri1 = tri1;
                    triCut.tri2 = tri2;
                    triCut.plane = plane;
                    triCut.unityPlane = new Plane(point0, point1, point2);

                    triCut.pointCount = 2;
                    
                    triCut.pointCount = 2;
                    triCut.localCutPos0 = intersectionPointBuffer[0];
                    triCut.localCutPos1 = intersectionPointBuffer[1];

                    lastFrameTriCutPoints.Add(triCut);
                }
            }
        }
    }

    private void Fire()
    {
        canFire = false;

        
        var meshCollider = TryConvertColliderIntoMesh(currentTargetCollider);
        currentTargetCollider = meshCollider;

        TryCuttingMeshCollider(meshCollider);
    }

    private MeshCollider TryConvertColliderIntoMesh(Collider target)
    {
        if(target == null) 
            return null;

        switch(target)
        {
            default:
                return null;

            case BoxCollider box: 
            case SphereCollider sphere:
            case CapsuleCollider capsule:
                Debug.Log($"target[{target}] is [{target.GetType()}] collider");
                {
                    var gobj = target.gameObject;
                    var targetRb = target.attachedRigidbody;
                    
                    var targetMesh = gobj.GetComponent<MeshFilter>();
                    var meshCollider = gobj.AddComponent<MeshCollider>();
                    var mesh = Instantiate(targetMesh.sharedMesh);
                    
                    meshCollider.sharedMesh = mesh;
                    meshCollider.convex = true;
                    meshCollider.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices;

                    if(targetRb != null)
                    {
                        targetRb.ResetCenterOfMass();
                        targetRb.ResetInertiaTensor();
                    }
                    Destroy(target);

                    return meshCollider;
                }

            case MeshCollider mesh:
                Debug.Log($"target[{target}] is already a collider");
                return mesh;
        }
    }

    private void TryCuttingMeshCollider(MeshCollider meshCollider)
    {
        //this is the bit where it gets serious
        HashSet<int> newTrisAdded = new(512);
        HashSet<int> triIndicesRemoved = new(512);

        var meshFilter = meshCollider.GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        var localToWorldMatrix = meshCollider.transform.localToWorldMatrix;

        //we have a list of tricuts from our raycasts. 


        foreach(var triCut in lastFrameTriCutPoints)
        {
            if(triCut.pointCount != 2)
                continue;

            //regardless, we remove the old tri
            triIndicesRemoved.Add(triCut.triIndex0);
            triIndicesRemoved.Add(triCut.triIndex1);
            triIndicesRemoved.Add(triCut.triIndex2);

            triBuffer[triCut.triIndex0] = 0;
            triBuffer[triCut.triIndex1] = 0;
            triBuffer[triCut.triIndex2] = 0;


            var point0 = vertsBuffer[triCut.tri0];
            var point1 = vertsBuffer[triCut.tri1];
            var point2 = vertsBuffer[triCut.tri2];

            var worldPoint0 = localToWorldMatrix.MultiplyPoint3x4(point0);
            var worldPoint1 = localToWorldMatrix.MultiplyPoint3x4(point1);
            var worldPoint2 = localToWorldMatrix.MultiplyPoint3x4(point2);
            var point0InsideBounds = LocalBoundsContains(worldPoint0);
            var point1InsideBounds = LocalBoundsContains(worldPoint1);
            var point2InsideBounds = LocalBoundsContains(worldPoint2);
            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, triCut.plane);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, triCut.plane);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, triCut.plane);
            
            Log($"Plane [{triCut.plane}] vert check: 0[{point0OnCutSideOfPlane}] 1[{point1OnCutSideOfPlane}] 2[{point2OnCutSideOfPlane}]");

            int numInsideBounds = 0;
            if(point0InsideBounds) numInsideBounds ++;
            if(point1InsideBounds) numInsideBounds ++;
            if(point2InsideBounds) numInsideBounds ++;
            
            Log($"Plane [{triCut.plane}] numInsidebounds[{numInsideBounds}]");

            if(numInsideBounds == 3)
            {
                //all 3 verts inside cut bounds - just destroy the thing!
                continue;
            }

            //we're adding 2 new verts
            int newTri0 = vertsBuffer.Count;
            int newTri1 = vertsBuffer.Count + 1;
            bool gotOldVert0 = false;
            bool gotOldVert1 = false;
            for(int i = 0 ; i < vertsBuffer.Count; i++)
            {
                var existingVert0 = vertsBuffer[i];
                if(existingVert0 == triCut.localCutPos0 && gotOldVert0 == false)
                {
                     newTri0 = i;
                     gotOldVert0 = true;
                }   
                if(existingVert0 == triCut.localCutPos1 && gotOldVert1 == false)
                {
                    newTri1 = i;
                    gotOldVert1 = true;
                }
            }

            if (gotOldVert0 == false)
            {
                newTri0 = vertsBuffer.Count;
                vertsBuffer.Add(triCut.localCutPos0);
            }
            if(gotOldVert1 == false)
            {
                newTri1 = vertsBuffer.Count;
                vertsBuffer.Add(triCut.localCutPos1);
            }
            
            newTrisAdded.Add(newTri0);
            newTrisAdded.Add(newTri1);

            var trisOnGoodSide = new List<int>();

            var facePlane = new Plane(vertsBuffer[triCut.tri1], vertsBuffer[triCut.tri0], vertsBuffer[triCut.tri2]);

            //to do a BOX cut we need to do more than simply check bounds or not - as we end up with nobody taking care
            //of the sort of 'corner tri'. if we do a bounds check we end up making a cap using the wrong list - it does that annoying slanty cut thing where newvert-left links to tri-point upper-right

            if(point0OnCutSideOfPlane == false)
                trisOnGoodSide.Add(triCut.tri0);
            if(point1OnCutSideOfPlane == false)
                trisOnGoodSide.Add(triCut.tri1);
            if(point2OnCutSideOfPlane == false)
                trisOnGoodSide.Add(triCut.tri2);

            var triList = new List<int>();
            triList.AddRange(trisOnGoodSide);
            triList.Add(newTri0);
            triList.Add(newTri1);

            BuildCap(triList, facePlane);
        }

        /*
        EliminateRemovedVerts(localToWorldMatrix);
        
        //we have added a bunch of new verts. 
        //can we stitch them together?
        var newTris = new List<int>(newTrisAdded);
        var topUnityPlane = GetLocalUnityPlane(ePlane.TOP, localToWorldMatrix);
        BuildCap(newTris, topUnityPlane);
      */

        var result = MeshIntersection.TidyMesh(triBuffer, vertsBuffer);

        if(newTrisAdded.Count > 0)
        {
            mesh.Clear();
            mesh.vertices = result.verts.ToArray();
            mesh.triangles = result.tris.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            meshFilter.sharedMesh = mesh;

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
            Physics.BakeMesh(mesh.GetInstanceID(), true);
        }
    }

    private void EliminateRemovedVerts(Matrix4x4 localToWorldMatrix)
    {
        
        //let's do it.
        //find all faces which are entirely on the wrong side of our plane:
        int numTriangles = triBuffer.Count / 3;
        for(int i = 0 ; i < numTriangles; i++)
        {
            var triIndex0 = 0 + i * 3;
            var triIndex1 = 1 + i * 3;
            var triIndex2 = 2 + i * 3;

            var tri0 = triBuffer[triIndex0];
            var tri1 = triBuffer[triIndex1];
            var tri2 = triBuffer[triIndex2];

            var point0 = vertsBuffer[tri0];
            var point1 = vertsBuffer[tri1];
            var point2 = vertsBuffer[tri2];

            var worldPoint0 = localToWorldMatrix.MultiplyPoint3x4(point0);
            var worldPoint1 = localToWorldMatrix.MultiplyPoint3x4(point1);
            var worldPoint2 = localToWorldMatrix.MultiplyPoint3x4(point2);
            
            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, ePlane.BOTTOM);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, ePlane.BOTTOM);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, ePlane.BOTTOM);

            if(point0OnCutSideOfPlane && point1OnCutSideOfPlane && point2OnCutSideOfPlane)
            {
                //whole face is to be dropped:
                triBuffer[triIndex0] = 0;
                triBuffer[triIndex1] = 0;
                triBuffer[triIndex2] = 0;
            }
        }
    }

    private enum ePlane
    {
        TOP = 0,
        LEFT = 1,
        RIGHT = 2,
        BOTTOM = 3,
    }

    private (Vector3 backA, Vector3 backB, Vector3 frontA, Vector3 frontB) GetPlane(ePlane plane)
    {
        var source = player.PlayerCamera.transform;

        var forward = source.forward * depth ;
        var right = source.right * width / 2f;
        var up = source.up * height / 2f;

        var center = source.position ;

        Vector3 tlb = center + up + (-right);// + (-forward);
        Vector3 tlf = center + up + (-right) + forward;
        Vector3 trb = center + up + right;// + (-forward);
        Vector3 trf = center + up + right + forward;

        Vector3 blb = center + (-up) + (-right);// + (-forward);
        Vector3 blf = center + (-up) + (-right) + forward;
        Vector3 brb = center + (-up) + right;// + (-forward);
        Vector3 brf = center + (-up) + right + forward;

        switch(plane)
        {
            default: return default;

            case ePlane.TOP: return (tlb, trb, tlf, trf);
            case ePlane.BOTTOM: return (blb, brb, blf, brf);
            case ePlane.LEFT: return (tlb, blb, tlf, blf);
            case ePlane.RIGHT: return (trb, brb, trf, brf);
        }
    }

    private Plane GetLocalUnityPlane(ePlane plane, Matrix4x4 localToWorldMatrix)
    {
        var worldPlane = GetPlane(plane);

        var worldToLocalMatrix = localToWorldMatrix.inverse;
        var localBackA = worldToLocalMatrix.MultiplyPoint( worldPlane.backA );
        var localBackB = worldToLocalMatrix.MultiplyPoint( worldPlane.backB );
        var localFrontA = worldToLocalMatrix.MultiplyPoint( worldPlane.frontA );
        var localPlane = new Plane( localBackA, localBackB, localFrontA );
        return localPlane;
    }

    private bool LocalBoundsContains(Vector3 worldVert)
    {
        var localVert = transform.InverseTransformPoint(worldVert);
        return localBoxBounds.Contains(localVert);
    }


    private bool VertPositionedOnCutSideOfPlane(Vector3 worldVert, ePlane plane)
    {

        //we need to get our plane 'normal' and get the dot against the localVert 
        var planeVerts = GetPlane(plane);
        var myPlane = new Plane(planeVerts.backA, planeVerts.frontA, planeVerts.frontB);

        switch(plane)
        {
            default:
            case ePlane.TOP: return myPlane.GetSide(worldVert) == false;
            case ePlane.RIGHT: return myPlane.GetSide(worldVert) == false ;

            case ePlane.BOTTOM: return myPlane.GetSide(worldVert);
            case ePlane.LEFT: return myPlane.GetSide(worldVert);
        }
    }

    private void CaptureMatchedWindingPlane(int tri0, int tri1, int tri2, Plane referencePlane)
    {
        var vert0 = vertsBuffer[tri0];
        var vert1 = vertsBuffer[tri1];
        var vert2 = vertsBuffer[tri2];
        var testPlane = new Plane(vert0, vert1, vert2);

        if(Vector3.Dot(testPlane.normal, referencePlane.normal) > 0)
        {
            triBuffer.Add(tri0);
            triBuffer.Add(tri1);
            triBuffer.Add(tri2);
        }
        else
        {
            triBuffer.Add(tri0);
            triBuffer.Add(tri2);
            triBuffer.Add(tri1);
        }
    }

    private class CapVert
    {
        public int tri;
        public Vector3 vert;

        public float rotationalAngle;
    }

    private void BuildCap(List<int> tris, Plane referencePlane)
    {
        if(tris.Count <= 0)
            return;

        var capVerts = new List<CapVert>();
        foreach(var tri in tris)
        {
            var capvert = new CapVert() { tri = tri, vert = vertsBuffer[tri] };
            capVerts.Add(capvert); 
        }

        //to get our verts in order, we're going to do that weird 'centroid' thing:
        Vector3 centroid = Vector3.zero;
        foreach(var capvert in capVerts)
            centroid += capvert.vert;
        
        centroid /= capVerts.Count;

        Vector3 planeVector = (capVerts[0].vert - centroid).normalized;
        capVerts[0].rotationalAngle = 0f;

        for(int i = 1; i < capVerts.Count; i++)
        {
            var capVert = capVerts[i];
            var capVertVector = (capVert.vert - centroid).normalized;
            capVert.rotationalAngle = Vector3.SignedAngle(capVertVector, planeVector, referencePlane.normal);
        }

        //now we sort them:
        capVerts.Sort( (a,b) => a.rotationalAngle.CompareTo(b.rotationalAngle));

        var flippedPlane = new Plane(-referencePlane.normal, -referencePlane.distance);
        //and then build a cap using a sort of 'fan'
        for(int i = 1 ; i < capVerts.Count - 1; i++)
        {
            //tri fan
            var tri0 = capVerts[0].tri;
            var tri1 = capVerts[i].tri;
            var tri2 = capVerts[i + 1].tri;

            CaptureMatchedWindingPlane(tri0, tri1, tri2, flippedPlane);
        }
    }

    private void Log(string log)
    {
        Debug.Log($"[PlayerWeaponBoxCutter] {log}");
    }

    
    private void OnDrawGizmos()
    {
        if(player == null || player.PlayerCamera == null)
            return;

        var pointSize = Vector3.one * 0.05f;
        Gizmos.color = Color.green;

        var topPlane = GetPlane(ePlane.TOP);
        Gizmos.DrawCube(topPlane.backA, pointSize);
        Gizmos.DrawCube(topPlane.backB, pointSize);
        Gizmos.DrawCube(topPlane.frontA, pointSize);
        Gizmos.DrawCube(topPlane.frontB, pointSize);
        Gizmos.DrawLine( topPlane.backA, topPlane.frontA );
        Gizmos.DrawLine( topPlane.backB, topPlane.frontB );

        var bottomPlane = GetPlane(ePlane.BOTTOM);
        Gizmos.DrawCube(bottomPlane.backA, pointSize);
        Gizmos.DrawCube(bottomPlane.backB, pointSize);
        Gizmos.DrawCube(bottomPlane.frontA, pointSize);
        Gizmos.DrawCube(bottomPlane.frontB, pointSize);
        Gizmos.DrawLine( bottomPlane.backA, bottomPlane.frontA );
        Gizmos.DrawLine( bottomPlane.backB, bottomPlane.frontB );
     
        Gizmos.color = Color.red;
        foreach(var lastFrameCastPoint in lastFrameCastPoints)
        {
            Gizmos.DrawCube(lastFrameCastPoint, pointSize * 2);
        }

        // Gizmos.color = Color.yellow;
        // var orig = Gizmos.matrix;
        // Gizmos.matrix = transform.localToWorldMatrix;
        // Gizmos.DrawWireCube(localBoxBounds.center, localBoxBounds.size * 0.95f);
        // Gizmos.DrawWireCube(localBoxBounds.center, localBoxBounds.size * 0.98f);
        // Gizmos.matrix = orig;
      
    }

}
