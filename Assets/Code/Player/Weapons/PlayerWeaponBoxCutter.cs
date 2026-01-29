using System.Collections.Generic;
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
                        Log($"intersectionCount not 2 [{intersectionPointBuffer.Count}]");
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

                    triCut.localCutPos0 = intersectionPointBuffer[0];
                    if(intersectionPointBuffer.Count == 2)
                    {
                        triCut.localCutPos1 = intersectionPointBuffer[1];
                    }
                    triCut.pointCount = intersectionPointBuffer.Count;
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
                    meshCollider.sharedMesh = Instantiate(targetMesh.sharedMesh);
                    meshCollider.convex = true;


                    targetRb.ResetCenterOfMass();
                    targetRb.ResetInertiaTensor();
                    
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
        List<int> newTrisAdded = new(512);
        List<int> triIndicesRemoved = new(512);

        var meshFilter = meshCollider.GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        var localToWorldMatrix = meshCollider.transform.localToWorldMatrix;

        //we have a list of tricuts from our raycasts. 


        foreach(var triCut in lastFrameTriCutPoints)
        {
            if(triCut.pointCount != 2)
                continue;

            var point0 = vertsBuffer[triCut.tri0];
            var point1 = vertsBuffer[triCut.tri1];
            var point2 = vertsBuffer[triCut.tri2];

            //we're adding 2 new verts
            int newTri0 = vertsBuffer.Count;
            vertsBuffer.Add(triCut.localCutPos0);
            int newTri1 = vertsBuffer.Count;
            vertsBuffer.Add(triCut.localCutPos1);

            newTrisAdded.Add(newTri0);
            newTrisAdded.Add(newTri1);

            //we are going to LEAVE our other vert in the mesh. 
            
            var worldPoint0 = localToWorldMatrix.MultiplyPoint3x4(point0);
            var point0InsideBounds = LocalBoundsContains(worldPoint0);
            var worldPoint1 = localToWorldMatrix.MultiplyPoint3x4(point1);
            var point1InsideBounds = LocalBoundsContains(worldPoint1);
            var worldPoint2 = localToWorldMatrix.MultiplyPoint3x4(point2);
            var point2InsideBounds = LocalBoundsContains(worldPoint2);

            int numInside = 0;
            if(point0InsideBounds)
                numInside += 1;
            if(point1InsideBounds)
                numInside += 1;
            if(point2InsideBounds)
                numInside += 1;


            //there are 4 possible scenarios:

            //1. we are cutting a point off our tri
            // - 1 point is inside our bounds and 2 outside
            if(numInside == 1)
            {
                Log("One inside");

                //there's a non zero chance our vert cut positions are in a good order.

                //1 - we eliminate our original tri:
                triIndicesRemoved.Add(triCut.triIndex0);
                triIndicesRemoved.Add(triCut.triIndex1);
                triIndicesRemoved.Add(triCut.triIndex2);

                triBuffer[triCut.triIndex0] = 0;
                triBuffer[triCut.triIndex1] = 0;
                triBuffer[triCut.triIndex2] = 0;


                var survivingTri0 = 0;
                var survivingTri1 = 0;
                if(point0InsideBounds)
                {
                    Log("0 inside");
                    survivingTri0 = triCut.tri1;
                    survivingTri1 = triCut.tri2;
                }
                else if(point1InsideBounds)
                {
                    Log("1 inside");

                    survivingTri0 = triCut.tri0;
                    survivingTri1 = triCut.tri1;
                }
                else if(point2InsideBounds)
                {
                    Log("2 inside");

                    survivingTri0 = triCut.tri0;
                    survivingTri1 = triCut.tri1;
                }

                triBuffer.Add(newTri0);
                triBuffer.Add(newTri1);
                triBuffer.Add(survivingTri1);

                triBuffer.Add(survivingTri0);
                triBuffer.Add(survivingTri1);
                triBuffer.Add(newTri1);
            }

            //2. we are cutting the base off our tri:
            // - 2 points are inside our bounds, 1 is outside
            else if(numInside == 2)
            {
                Log("Two inside");   
            }

            else if(numInside == 0)
            {
                //3. we are cutting into our tri:
                // - all points are outside but we have cut an edge
                // - and our bounds is not completely contained by the tri
                Log("Zero inside");
            
                //4. we are cutting a hole in our tri:
                // - all the points are ouside
                // - and our bounds IS completely contained by the tri
            }
        }

        if(newTrisAdded.Count > 0)
        {
            mesh.vertices = vertsBuffer.ToArray();
            mesh.triangles = triBuffer.ToArray();
            mesh.UploadMeshData(markNoLongerReadable: false);
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
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

    private bool LocalBoundsContains(Vector3 worldVert)
    {
        var localVert = transform.InverseTransformPoint(worldVert);
        return localBoxBounds.Contains(localVert);
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
