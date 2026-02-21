using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//need ability to orient picked up things.
//need to be able to move ... smoothly
//detect Detachment and make bodies
//also eliminate bits which are too tiny. 

public class PlayerWeaponBoxCutter : PlayerWeapon
{
    [SerializeField]
    private Transform boxCenterTransform;

    private struct TriCutPoint
    {
        public Collider collider;
        public int subMesh;

        public int triIndex0;
        public int triIndex1;
        public int triIndex2;

        public int tri0;
        public int tri1;
        public int tri2;

        public ePlane plane;

        public Plane unityPlane;

        public Plane cutPlane;

        public Vector3 localCutPos0;
        public Vector3 localCutPos1;

        public int pointCount;
    }

    private class CutResult
    {
        public GameObject cutCreatedObject;
        public MeshCollider cutCreatedMeshCollider;
        public MeshFilter cutCreatedMeshFilter;
        public Transform cutCreatedParentTransform;

        public Transform cutSourceTransform;
        public Vector3 cutSourceLocalScale;
        public Vector3 cutCreatedWorldPosition;
        public Quaternion cutSourceLocalRotation;

        public MeshCollider cutSourceMeshCollider;
        public MeshFilter cutSourceMeshFilter;

        public Rigidbody cutSourceRigidBody;

        public ePlane cutPlane;

        public float bodyMass; //calculate
    }
    
    private enum ePlane
    {
        INVALID = 0,

        BOTTOM = 10,
        TOP = 20,
        LEFT = 30, 
        RIGHT = 40,

        MIDDLE_HORIZONTAL = 50,

        POLYGON = 60,
    }

    public enum eMode
    {
        INVALID = 0,

        BOX = 10,
        SLICE = 20,

        N_SIDED_POLYGON = 30,
    }

    public enum eGuidanceMode
    {
        INVALID = 0,

        PLAYER_FORWARD = 10,
        TARGET_NORMAL = 20,
    }

    public eGuidanceMode guidanceMode = eGuidanceMode.PLAYER_FORWARD;

    private List<CutResult> cuttingResults = new List<CutResult>();

    private Dictionary<ePlane, List<Vector3>> lastFrameCastPoints = new(8);
    private List<TriCutPoint> lastFrameTriCutPoints = new(64);
    private List<Collider> lastFrameCollidersHit = new ();

    private Vector3 nearestHitNormal;
    private Vector3 nearestHitPoint;
    private Vector3 nearestHitColliderRight;
    private Vector3 nearestHitColliderUp;

    private RaycastHit[] castHitBuffer = new RaycastHit[32];
    private List<int> triBuffer = new (65535);
    private List<int> triBuffer2 = new (65535);
    private List<Vector3> vertsBuffer = new(65535);
    private List<Vector3> normalsBuffer = new (65535);
    private List<Vector2> uvBuffer = new (65535);
    private List<Vector3> intersectionPointBuffer = new(4); //should be max 2

    public float width;
    public float height;
    //depth is fixed
    public float depth = 10f;

    public eMode mode = eMode.SLICE;

    public float planeRotation = 0f;
    public float planeRotationSpeed = 45f;
    public float sizeChangeSpeed = 1f;
    private float minSize = 0.1f;
    private float maxSize = 1f;

    public bool cutOnlyNearest;

    public int polygonSideCount = 3;

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
        Checking();

        if(CanFire && rightFire)
        {
            Fire();
        }
        
        if(rightFire == false)
        {
            canFire = true;
        }

        if(Keyboard.current.rKey.isPressed)
        {
            planeRotation += planeRotationSpeed * Time.deltaTime;   
        }
        if(Keyboard.current.tKey.isPressed)
        {
            planeRotation -= planeRotationSpeed * Time.deltaTime;   
        }
        if(Keyboard.current.fKey.isPressed)
        {
            width -= sizeChangeSpeed * Time.deltaTime;
            width = Mathf.Clamp(width, minSize, maxSize);
        }
        if(Keyboard.current.gKey.isPressed)
        {
            width += sizeChangeSpeed * Time.deltaTime;
            width = Mathf.Clamp(width, minSize, maxSize);
        }
        if(Keyboard.current.vKey.isPressed)
        {
            height -= sizeChangeSpeed * Time.deltaTime;
            height = Mathf.Clamp(height, minSize, maxSize);
        }
        if(Keyboard.current.bKey.isPressed)
        {
            height += sizeChangeSpeed * Time.deltaTime;
            height = Mathf.Clamp(height, minSize, maxSize);
        }

        if(Keyboard.current.yKey.wasPressedThisFrame)
        {
            if(mode == eMode.BOX)
                mode = eMode.SLICE;
            else if(mode == eMode.SLICE)
                mode = eMode.N_SIDED_POLYGON;
            else if(mode == eMode.N_SIDED_POLYGON)
                mode = eMode.BOX;
        }

        if(Keyboard.current.uKey.wasPressedThisFrame)
        {
            if(guidanceMode == eGuidanceMode.TARGET_NORMAL)
                guidanceMode = eGuidanceMode.PLAYER_FORWARD;
            else
                guidanceMode = eGuidanceMode.TARGET_NORMAL;
        }

        if(Keyboard.current.jKey.wasPressedThisFrame)
        {
            polygonSideCount -= 1; 
            polygonSideCount = Mathf.Clamp(polygonSideCount, 3, 16);
        }
        if(Keyboard.current.kKey.wasPressedThisFrame)
        {
            polygonSideCount += 1;
            polygonSideCount = Mathf.Clamp(polygonSideCount, 3, 16);
        }
    }

    private void Checking()
    {
        lastFrameCollidersHit.Clear();
        ClearCache();
        
        //we want to cut whatever we see in front of us (just the first thing) 
        var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var rayOrigin = ray.origin; 


        int ignoreLayer = LayerMask.NameToLayer("Player");
        int layerMask = ~(1 << ignoreLayer);

        var boxExtents = new Vector3(width, height, 0.01f);
        var numHits = Physics.BoxCastNonAlloc(rayOrigin, boxExtents / 2f, ray.direction, castHitBuffer, transform.rotation, depth, layerMask);

        if (numHits > 0)
        {
            //using a simple raycast, find the thing we're actually pointing at
            var rayHit = Physics.Raycast(rayOrigin, ray.direction, out var hitInfo, depth, layerMask);

            if(rayHit)
            {
                nearestHitPoint = hitInfo.point;
            }


            if (rayHit == false || hitInfo.collider.attachedRigidbody == null)
            {
                bool yesHit = false;
                var nearestProx = 1000f;
                for(int i = 0 ; i < numHits; i++)
                {
                    var hit = castHitBuffer[i];
                    if(hit.collider.attachedRigidbody == null)
                        continue;

                    var dist = Vector3.Distance(hit.point, rayOrigin);
                    if( dist < nearestProx )
                    {
                        nearestProx = dist;
                        hitInfo = hit;
                        yesHit = true;
                    }
                }

                if(yesHit == false)
                    return;

                //any hit on the box CAN be manipulated to find the center of the box as cast
                var hitPlane = new Plane(hitInfo.normal, hitInfo.point);
                if(hitPlane.Raycast(ray, out float enter))
                {
                    nearestHitPoint = rayOrigin + ray.direction * enter;
                }
            }

            rawHitPoint = hitInfo.point;
            rawHitNorm = hitInfo.normal;

            nearestHitColliderRight = hitInfo.collider.transform.right;
            nearestHitColliderUp = hitInfo.collider.transform.up;
            nearestHitNormal = hitInfo.normal;

            var bodyHit = hitInfo.collider.attachedRigidbody;
            if (bodyHit != null)
            {
                for(int i = 0 ; i < numHits; i++)
                {
                    var hit = castHitBuffer[i];
                    var collider = hit.collider;
                    if(collider.attachedRigidbody == bodyHit)
                        lastFrameCollidersHit.Add(collider);
                }

                var allColliders = bodyHit.GetComponentsInChildren<Collider>();
                foreach (var collider in allColliders)
                {

                    var meshFilter = collider.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        if(mode == eMode.BOX)
                        {
                            CastPlaneAgainstMesh(collider, meshFilter, ePlane.BOTTOM, shortenedPlanes: true);
                            CastPlaneAgainstMesh(collider, meshFilter, ePlane.TOP, shortenedPlanes: true);
                            CastPlaneAgainstMesh(collider, meshFilter, ePlane.RIGHT, shortenedPlanes: true);
                            CastPlaneAgainstMesh(collider, meshFilter, ePlane.LEFT, shortenedPlanes: true);
                        }
                        else if(mode == eMode.SLICE)
                        {
                            CastPlaneAgainstMesh(collider, meshFilter, ePlane.MIDDLE_HORIZONTAL, shortenedPlanes: false);
                        }
                        else if(mode == eMode.N_SIDED_POLYGON)
                        {
                            CastPolyPlanesAgainstMesh(collider, meshFilter, shortenedPlanes: true);
                        }
                    }
                }
            }
        }
    }

    private void CastPlaneAgainstMesh(Collider collider, MeshFilter meshFilter, ePlane plane, bool shortenedPlanes = false)
    {
        var meshWorldMatrix = meshFilter.transform.worldToLocalMatrix;
        var meshLocalMatrix = meshFilter.transform.localToWorldMatrix;
        var mesh = meshFilter.mesh;
        CastPlaneAgainstMesh(collider, mesh, meshWorldMatrix, meshLocalMatrix, plane, shortenedPlanes);
    }  

    private void CastPolyPlanesAgainstMesh(Collider collider, MeshFilter meshFilter, bool shortenedPlanes = false)
    {
        for(int i = 0 ; i < polygonSideCount; i++)
        {
            var worldPlane = GetPolyPlane(i, polygonSideCount, shortenedPlanes);
            CastPolyPlaneAgainstMesh(collider, meshFilter, worldPlane, shortenedPlanes);
        }
    }
    private void CastPolyPlaneAgainstMesh(Collider collider, MeshFilter meshFilter, SlicePlane worldPlane, bool shortenedPlanes = false)
    {
        var meshWorldMatrix = meshFilter.transform.worldToLocalMatrix;
        var meshLocalMatrix = meshFilter.transform.localToWorldMatrix;
        var mesh = meshFilter.mesh;
        var plane = ePlane.POLYGON;
        
        CastPlaneAgainstMesh(collider, mesh, meshWorldMatrix, meshLocalMatrix, plane, worldPlane);
    }

    private void ClearCache()
    {
        dic.Clear();
        lastFrameCastPoints.Clear();
        lastFrameTriCutPoints.Clear();
        cuttingResults.Clear();
        vertsBuffer.Clear();
        triBuffer.Clear();
        triBuffer2.Clear();
        normalsBuffer.Clear();
        uvBuffer.Clear();
    }

    private void CastPlaneAgainstMesh(Collider collider, Mesh mesh, Matrix4x4 meshWorldMatrix, Matrix4x4 meshLocalMatrix, ePlane plane, bool shortenedPlanes = false)
    {
        var worldPlane = GetPlane(plane, shortenedPlanes);
        CastPlaneAgainstMesh(collider, mesh, meshWorldMatrix, meshLocalMatrix, plane, worldPlane);
    }
    private void CastPlaneAgainstMesh(Collider collider, Mesh mesh, Matrix4x4 meshWorldMatrix, Matrix4x4 meshLocalMatrix, ePlane plane, SlicePlane worldPlane)
    {
        var localBackA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backA);
        var localBackb = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backB);
        var localFrontA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontA);
        var localFrontB = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontB);
        var localPlane = new Plane(localBackA, localFrontB, localBackb);

        if(MeshIntersection.PlaneIntersectsBounds(localPlane, mesh.bounds) == false)
        {
            return;
        }

        var numSubmeshes = mesh.subMeshCount;
        for(int i = 0 ; i < numSubmeshes; i++)
        {
            mesh.GetTriangles(triBuffer, i);
            mesh.GetTriangles(triBuffer2, i);
            mesh.GetVertices(vertsBuffer);
            mesh.GetNormals(normalsBuffer);
            mesh.GetUVs(0, uvBuffer);

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

                        if(lastFrameCastPoints.TryGetValue(plane, out var castPointsList) == false)
                        {
                            castPointsList = new List<Vector3>();
                            lastFrameCastPoints[plane] = castPointsList;
                        }
                        castPointsList.Add(worldIntersectionPoint);
                    }

                    var triCut = new TriCutPoint();
                    triCut.collider = collider;
                    triCut.subMesh = i;
                    triCut.triIndex0 = triIndex0;
                    triCut.triIndex1 = triIndex1;
                    triCut.triIndex2 = triIndex2;
                    triCut.tri0 = tri0;
                    triCut.tri1 = tri1;
                    triCut.tri2 = tri2;
                    triCut.unityPlane = new Plane(point0, point1, point2);
                    triCut.cutPlane = localPlane;
                    triCut.plane = plane;

                    triCut.pointCount = 2;
                    triCut.localCutPos0 = intersectionPointBuffer[0];
                    triCut.localCutPos1 = intersectionPointBuffer[1];

                    lastFrameTriCutPoints.Add(triCut);
                }
            }
        }
    }
     Dictionary<Collider, List<TriCutPoint>> dic = new Dictionary<Collider, List<TriCutPoint>>(1024);

    private void Fire()
    {
        canFire = false;

        //so we are going to cut our current 'target' - that will be inside our planes
        //meaning we want to do 2 horizontal cuts (top and bottom)
        //then cut the left and right parts down.

        Rigidbody rigidBody = null;
        HashSet<Collider> colliders = new ();

        foreach(var cutpoint in lastFrameTriCutPoints)
        {
            if(rigidBody == null)
            {
                rigidBody = cutpoint.collider.attachedRigidbody;
            }

            if(rigidBody == cutpoint.collider.attachedRigidbody)
            {
                colliders.Add(cutpoint.collider);
                lastFrameCollidersHit.Remove(cutpoint.collider);
            }
        }

        foreach(var collider in colliders)
        {
            var meshCollider = TryConvertColliderIntoMesh(collider);
            Cut(meshCollider);
        }
        
        ProcessUnCutColliders();
    }

    private void Cut(MeshCollider meshCollider)
    {
        GameObject finalObject = null;
        var meshFilter = meshCollider.GetComponent<MeshFilter>();

        if (mode == eMode.BOX)
        {
            Cut(ePlane.BOTTOM, ref meshCollider, ref meshFilter, ref finalObject, false);
            Cut(ePlane.TOP, ref meshCollider, ref meshFilter, ref finalObject, false);
            Cut(ePlane.LEFT, ref meshCollider, ref meshFilter, ref finalObject, false);
            Cut(ePlane.RIGHT, ref meshCollider, ref meshFilter, ref finalObject, false);
            if (finalObject != null)
            {
                GameObject.Destroy(finalObject);
            }
        }
        else if(mode == eMode.SLICE)
        {
            Cut(ePlane.MIDDLE_HORIZONTAL, ref meshCollider, ref meshFilter, ref finalObject, separateCutParts: true, shortenedPlanes: false);
        }
        else if(mode == eMode.N_SIDED_POLYGON)
        {
            for(int i = 0 ; i < polygonSideCount; i++)
            {
                var plane = ePlane.POLYGON;
                var worldPlane = GetPolyPlane(i, polygonSideCount, false);
                CutPoly(plane, worldPlane, ref meshCollider, ref meshFilter, ref finalObject, separateCutParts: false, shortenedPlanes: false);
            }
            if (finalObject != null)
            {
                GameObject.Destroy(finalObject);
            }
        }
    }

    private void Cut(ePlane plane, ref MeshCollider meshCollider, ref MeshFilter meshFilter, ref GameObject finalObject, bool separateCutParts, bool shortenedPlanes = false)
    {
        ClearCache();
        CastPlaneAgainstMesh(meshCollider, meshFilter, plane, shortenedPlanes);
        TryCuttingMeshCollider(meshCollider, lastFrameTriCutPoints, plane, separateCutParts);

        Log($"cut result count [{plane}]: [{cuttingResults.Count}]");
        if(cuttingResults.Count > 0)
        {
            ResolveCutResult(cuttingResults[0], separateCutParts);
            meshCollider = cuttingResults[0].cutCreatedMeshCollider;
            meshFilter = cuttingResults[0].cutCreatedMeshFilter;
            finalObject = cuttingResults[0].cutCreatedObject;
        }
    } 
    private void CutPoly(ePlane plane, SlicePlane worldPlane, ref MeshCollider meshCollider, ref MeshFilter meshFilter, ref GameObject finalObject, bool separateCutParts, bool shortenedPlanes = false)
    {
        ClearCache();
        CastPolyPlaneAgainstMesh(meshCollider, meshFilter, worldPlane, shortenedPlanes);
        TryCuttingMeshCollider(meshCollider, lastFrameTriCutPoints, worldPlane, plane, separateCutParts);

        Log($"cut result count [{plane}]: [{cuttingResults.Count}]");
        if(cuttingResults.Count > 0)
        {
            ResolveCutResult(cuttingResults[0], separateCutParts);
            meshCollider = cuttingResults[0].cutCreatedMeshCollider;
            meshFilter = cuttingResults[0].cutCreatedMeshFilter;
            finalObject = cuttingResults[0].cutCreatedObject;
        }
    }

    private void ProcessUnCutColliders()
    {
      
        //NOT WORKING - things disappear when they shouldn't.

        foreach(var collider in lastFrameCollidersHit)
        {
            bool fullyInsidePoly = PolygonContainsMesh(collider as MeshCollider, out var localPlanes);

            bool destroy = false;

            if(fullyInsidePoly)
            {
                destroy = true;
            }
            else
            {
                destroy = MeshIntersection.MeshIsInsideConvexVolume(localPlanes, (collider as MeshCollider).sharedMesh.vertices);
            }


            if(destroy)
            {
                if (collider.transform.childCount > 0)
                {
                    //let's just remove the mesh renderer and collider:
                    var renderer = collider.GetComponent<MeshRenderer>();
                    var filter = collider.GetComponent<MeshFilter>();

                    Destroy(collider);
                    Destroy(renderer);
                    Destroy(filter);
                }
                else
                {
                    //so... we have colliders we hit but our rays didn't intersect them at all?
                    GameObject.Destroy(collider.gameObject);
                }
            }
        }
    }

    private bool PolygonContainsMesh(MeshCollider collider, out List<Plane> planes)
    {
        
        var meshWorldMatrix = collider.transform.worldToLocalMatrix;
        planes = new List<Plane>();
        for(int i = 0 ; i < polygonSideCount; i++)
        {
            var worldPlane = GetPolyPlane(i, polygonSideCount, false);

            var localBackA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backA);
            var localBackb = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backB);
            var localFrontB = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontB);
            var localPlane = new Plane(localBackA, localBackb, localFrontB);


            planes.Add(localPlane);
        }
        
        return MeshIntersection.BoundsFullyInsideConvexVolume(planes, collider.sharedMesh.bounds);
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
                Debug.Log($"target[{target}] is already a meshcollider");
                return mesh;
        }
    }

    private void TryCuttingMeshCollider(MeshCollider meshCollider, List<TriCutPoint> triCutPoints, ePlane plane, bool separateCutParts)
    {
        var slicePlane = GetPlane(plane);
        TryCuttingMeshCollider(meshCollider, triCutPoints, slicePlane, plane, separateCutParts);
    }
    private void TryCuttingMeshCollider(MeshCollider meshCollider, List<TriCutPoint> triCutPoints, SlicePlane slicePlane, ePlane plane, bool separateCutParts)
    {
        //this is the bit where it gets serious
        HashSet<int> newTrisAdded = new(512);
        HashSet<int> triIndicesRemoved = new(512);

        var meshFilter = meshCollider.GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        var localToWorldMatrix = meshCollider.transform.localToWorldMatrix;

        int lastSubMeshGot = -1;

        //we have a list of tricuts from our raycasts. 
        foreach(var triCut in triCutPoints)
        {
            if(triCut.pointCount != 2)
                continue;

            if(triCut.subMesh != lastSubMeshGot)
            {
                mesh.GetTriangles(triBuffer, triCut.subMesh);
                mesh.GetTriangles(triBuffer2, triCut.subMesh);
                mesh.GetVertices(vertsBuffer);
                mesh.GetNormals(normalsBuffer);
                mesh.GetUVs(0, uvBuffer);
                lastSubMeshGot = triCut.subMesh;
            }

            //regardless, we remove the old tri
            triIndicesRemoved.Add(triCut.triIndex0);
            triIndicesRemoved.Add(triCut.triIndex1);
            triIndicesRemoved.Add(triCut.triIndex2);

            triBuffer[triCut.triIndex0] = 0;
            triBuffer[triCut.triIndex1] = 0;
            triBuffer[triCut.triIndex2] = 0; 
            
            triBuffer2[triCut.triIndex0] = 0;
            triBuffer2[triCut.triIndex1] = 0;
            triBuffer2[triCut.triIndex2] = 0;

            var point0 = vertsBuffer[triCut.tri0];
            var point1 = vertsBuffer[triCut.tri1];
            var point2 = vertsBuffer[triCut.tri2];

            var worldPoint0 = localToWorldMatrix.MultiplyPoint3x4(point0);
            var worldPoint1 = localToWorldMatrix.MultiplyPoint3x4(point1);
            var worldPoint2 = localToWorldMatrix.MultiplyPoint3x4(point2);
            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, slicePlane);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, slicePlane);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, slicePlane);
            
           // Log($"Plane vert check: 0[{point0OnCutSideOfPlane}] 1[{point1OnCutSideOfPlane}] 2[{point2OnCutSideOfPlane}]");

          
            //we're adding 2 new verts
            int newTri0 = vertsBuffer.Count;
            int newTri1 = vertsBuffer.Count + 1;

            //what is our NORMAL, well it's our plane's normal!
            var newNormal = normalsBuffer[triCut.tri0];

            var newUV0 = GetUV(triCut.localCutPos0, triCut.tri0, triCut.tri1, triCut.tri2);
            var newUV1 = GetUV(triCut.localCutPos1, triCut.tri0, triCut.tri1, triCut.tri2);

            newTri0 = vertsBuffer.Count;
            vertsBuffer.Add(triCut.localCutPos0);
            normalsBuffer.Add(newNormal);
            uvBuffer.Add(newUV0);

            newTri1 = vertsBuffer.Count;
            vertsBuffer.Add(triCut.localCutPos1);
            normalsBuffer.Add(newNormal);
            uvBuffer.Add(newUV1);
            
            var trisOnGoodSide = new List<int>();
            var facePlane = new Plane(vertsBuffer[triCut.tri1], vertsBuffer[triCut.tri0], vertsBuffer[triCut.tri2]);

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

            MeshIntersection.BuildCap(triList, facePlane, triBuffer, vertsBuffer, true);

            //also, the other side of the plane... ?
            var trisOnBadSide = new List<int>();
            
            if(point0OnCutSideOfPlane)
                trisOnBadSide.Add(triCut.tri0);
            if(point1OnCutSideOfPlane)
                trisOnBadSide.Add(triCut.tri1);
            if(point2OnCutSideOfPlane)
                trisOnBadSide.Add(triCut.tri2);
                

            var triList2 = new List<int>();
            triList2.AddRange(trisOnBadSide);
            triList2.Add(newTri0);
            triList2.Add(newTri1);

            MeshIntersection.BuildCap(triList2, facePlane, triBuffer2, vertsBuffer, true);

            //we duplicate our verts so our cap doesn't share normals with the SIDES
            var planeNormal = triCut.cutPlane.normal;
            var capTri0 = vertsBuffer.Count;
            var capTri1 = vertsBuffer.Count + 1;
            vertsBuffer.Add(triCut.localCutPos0);
            vertsBuffer.Add(triCut.localCutPos1);
            normalsBuffer.Add(planeNormal);
            normalsBuffer.Add(planeNormal);
            uvBuffer.Add(newUV0);
            uvBuffer.Add(newUV1);

            //my new tris need to be added more carefully. concave meshes can end up making our verts connect across gaps
            //my idea is something like rasterization, we pass through faces to get here and count them. but it won't work.
            newTrisAdded.Add(capTri0);
            newTrisAdded.Add(capTri1);
        }

      
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
            
            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, slicePlane);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, slicePlane);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, slicePlane);

            if(point0OnCutSideOfPlane && point1OnCutSideOfPlane && point2OnCutSideOfPlane)
            {
                //whole face is to be dropped:
                triBuffer[triIndex0] = 0;
                triBuffer[triIndex1] = 0;
                triBuffer[triIndex2] = 0;
            }
        }

        if (newTrisAdded.Count <= 0)
            return;
        //we have added a bunch of new verts. 
        //can we stitch them together?
        var newTris = new List<int>(newTrisAdded);
        var limitPlane = GetLocalUnityPlane(localToWorldMatrix, slicePlane);
        MeshIntersection.BuildCap(newTris, limitPlane, triBuffer, vertsBuffer, true);

        var result = MeshIntersection.TidyMesh(triBuffer, vertsBuffer, normalsBuffer, uvBuffer, limitPlane.flipped);

        mesh.Clear();
        mesh.vertices = result.verts.ToArray();
        mesh.triangles = result.tris.ToArray();
        mesh.normals = result.normals.ToArray();
        mesh.uv = result.uvs.ToArray();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
        Physics.BakeMesh(mesh.GetInstanceID(), true);


        numTriangles = triBuffer2.Count / 3;
        for (int i = 0; i < numTriangles; i++)
        {
            var triIndex0 = 0 + i * 3;
            var triIndex1 = 1 + i * 3;
            var triIndex2 = 2 + i * 3;

            var tri0 = triBuffer2[triIndex0];
            var tri1 = triBuffer2[triIndex1];
            var tri2 = triBuffer2[triIndex2];

            var point0 = vertsBuffer[tri0];
            var point1 = vertsBuffer[tri1];
            var point2 = vertsBuffer[tri2];

            var worldPoint0 = localToWorldMatrix.MultiplyPoint3x4(point0);
            var worldPoint1 = localToWorldMatrix.MultiplyPoint3x4(point1);
            var worldPoint2 = localToWorldMatrix.MultiplyPoint3x4(point2);

            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, slicePlane, true);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, slicePlane, true);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, slicePlane, true);

            if (point0OnCutSideOfPlane && point1OnCutSideOfPlane && point2OnCutSideOfPlane)
            {
                triBuffer2[triIndex0] = 0;
                triBuffer2[triIndex1] = 0;
                triBuffer2[triIndex2] = 0;
            }
        }

        //Flip the normals now - the chopped off part's cleaved faces need opposite normals
        foreach (var tri in newTrisAdded)
        {
            normalsBuffer[tri] = -normalsBuffer[tri];
        }

        newTris = new List<int>(newTrisAdded);
        limitPlane = GetLocalUnityPlane(localToWorldMatrix, slicePlane);
        MeshIntersection.BuildCap(newTris, limitPlane, triBuffer2, vertsBuffer, false);

        result = MeshIntersection.TidyMesh(triBuffer2, vertsBuffer, normalsBuffer, uvBuffer, limitPlane);

        var localCenterOfMassOffset = Vector3.zero;
        if(separateCutParts)
        {
            localCenterOfMassOffset = MeshIntersection.NormaliseVertsByCenterOfMass(result.verts);
        }

        var originalBody = meshCollider.attachedRigidbody;
        var originalObject = originalBody != null ? originalBody.gameObject : meshCollider.gameObject;
        var originalTransform = originalObject.transform;

        mesh = Instantiate(mesh);
        mesh.Clear();
        mesh.vertices = result.verts.ToArray();
        mesh.triangles = result.tris.ToArray();
        mesh.normals = result.normals.ToArray();
        mesh.uv = result.uvs.ToArray();
        mesh.RecalculateBounds();

        var clone = new GameObject();
        clone.name = originalObject.name;
        var cutMeshFilter = clone.AddComponent<MeshFilter>();
        var cutMeshCollider = clone.AddComponent<MeshCollider>();
        cutMeshCollider.convex = true;
        cutMeshFilter.sharedMesh = mesh;
        cutMeshCollider.sharedMesh = null;
        cutMeshCollider.sharedMesh = mesh;

        var cutResult = new CutResult();
        cutResult.cutCreatedObject = clone;

        cutResult.cutCreatedParentTransform = originalTransform.parent;
        cutResult.cutSourceTransform = originalTransform;
        cutResult.cutSourceLocalScale = originalTransform.localScale;
        cutResult.cutCreatedWorldPosition = originalTransform.TransformPoint(localCenterOfMassOffset);
        cutResult.cutSourceLocalRotation = originalTransform.localRotation;
        cutResult.cutCreatedMeshCollider = cutMeshCollider;
        cutResult.cutCreatedMeshFilter = cutMeshFilter;
        cutResult.cutSourceMeshCollider = meshCollider;
        cutResult.cutSourceMeshFilter = meshFilter;

        var rend = clone.AddComponent<MeshRenderer>();
        rend.material = originalObject.GetComponent<MeshRenderer>().material;
        Physics.BakeMesh(mesh.GetInstanceID(), true);

        if (separateCutParts)
        {
            if (originalBody != null)
            {
                cutResult.cutSourceRigidBody = originalBody;
            }
        }

        cutResult.cutPlane = plane;
        cuttingResults.Add(cutResult);
    }


    private void ResolveCutResult(CutResult result, bool separateAsNewBody)
    {
        if(separateAsNewBody == false)
        {
            result.cutCreatedObject.transform.SetParent(result.cutSourceTransform);
            result.cutCreatedObject.transform.localPosition = Vector3.zero;
            result.cutCreatedObject.transform.localRotation = Quaternion.identity;
            result.cutCreatedObject.transform.localScale = Vector3.one;
        }
        else
        {
            result.cutCreatedObject.transform.SetParent(result.cutCreatedParentTransform, false);
            result.cutCreatedObject.transform.localScale = result.cutSourceLocalScale;
            result.cutCreatedObject.transform.position = result.cutCreatedWorldPosition;
            result.cutCreatedObject.transform.localRotation = result.cutSourceLocalRotation;

            var cloneBody = result.cutCreatedObject.AddComponent<Rigidbody>();
            cloneBody.mass = result.bodyMass ;
            cloneBody.angularDamping = result.cutSourceRigidBody.angularDamping;
            cloneBody.linearDamping = result.cutSourceRigidBody.linearDamping;
            cloneBody.useGravity = true;
            cloneBody.automaticInertiaTensor = true;
            cloneBody.automaticCenterOfMass = true;
        }
    }

    private struct SlicePlane
    {
        public Vector3 backA;
        public Vector3 backB;
        public Vector3 frontA; 
        public Vector3 frontB;
    }
    private SlicePlane GetPlane(ePlane plane, bool shortened = false)
    {
        var planeDepth = depth;
        var planeWidth = width;

        if(shortened == false)
        {
            planeDepth = 100f;
            planeWidth = 100f;
        }

        var source = player.PlayerCamera.transform;
        var forward = source.forward;
        var right = source.right;
        var unitUp = source.up;

        if(guidanceMode == eGuidanceMode.TARGET_NORMAL)
        {
            forward = -nearestHitNormal ;
            right = Vector3.Cross(forward, -nearestHitColliderUp) ;

            if(Mathf.Abs(Vector3.Dot(forward, nearestHitColliderUp)) > 0.99f)
            {
                //it's actually vertical. so 'up' has to be not up...
                var rightY0 = nearestHitColliderRight;
                right = Vector3.Cross(forward, rightY0);
            }

            unitUp = Vector3.Cross(forward, right) ;
        }

        debugForward = forward;
        debugUp = unitUp;
        debugRight = right;

        forward *= planeDepth;
        right *= planeWidth / 2f;
        var up = unitUp * height / 2f;
        var pos = nearestHitPoint - forward/2f;

        float rotation = 0;
        switch(plane)
        {
            case ePlane.BOTTOM:
                rotation = 0f;
                break;
            case ePlane.TOP:
                rotation = 180f;
                break;
            case ePlane.LEFT:
                rotation = 270f;
                up = unitUp * width / 2f;
                break;
            case ePlane.RIGHT:
                rotation = 90f;
                up = unitUp * width / 2f;
                break;
            case ePlane.MIDDLE_HORIZONTAL:
                rotation = 0f;
                up = Vector3.zero;
                break;
        }

        rotation += planeRotation;
        var orientation = Quaternion.AngleAxis(rotation, forward);
        right = orientation * right;
        up = orientation * up;

        var center = pos - up;
        Vector3 lb = center + (-right) + (-forward);
        Vector3 lf = center + (-right) + forward;
        Vector3 rb = center + right + (-forward);
        Vector3 rf = center + right + forward;

        return new SlicePlane() { backA =lb, backB = rb, frontA = lf, frontB = rf};
    }

    
    private SlicePlane GetPolyPlane(int index, int count, bool shortened = false)
    {
        var planeDepth = depth;
        var planeWidth = width;

        var angle = Mathf.PI  / polygonSideCount;
        planeWidth = 2f * (height / 2f) * Mathf.Tan(angle);

        if(shortened == false)
        {
            planeDepth = 100f;
            planeWidth = 100f;
        }

        var source = player.PlayerCamera.transform;
        var forward = source.forward;
        var right = source.right;
        var unitUp = source.up;

        if(guidanceMode == eGuidanceMode.TARGET_NORMAL)
        {
            forward = -nearestHitNormal ;
            right = Vector3.Cross(forward, -nearestHitColliderUp) ;

            if(Mathf.Abs(Vector3.Dot(forward, nearestHitColliderUp)) > 0.99f)
            {
                //it's actually vertical. so 'up' has to be not up...
                var rightY0 = nearestHitColliderRight;
                right = Vector3.Cross(forward, rightY0);
            }

            unitUp = Vector3.Cross(forward, right) ;
        }
        

        debugForward = forward;
        debugUp = unitUp;
        debugRight = right;

        forward *= planeDepth;
        right *= planeWidth / 2f;
        var up = unitUp * height / 2f;
        var pos = nearestHitPoint - forward/2f;
      
        float rotation = index * (360f / count);

        rotation += planeRotation;
        var orientation = Quaternion.AngleAxis(rotation, forward);
        right = orientation * right;
        up = orientation * up;

        var center = pos - up;

        Vector3 lb = center + (-right) + (-forward);
        Vector3 lf = center + (-right) + forward;
        Vector3 rb = center + right + (-forward);
        Vector3 rf = center + right + forward;

        return new SlicePlane() { backA =lb, backB = rb, frontA = lf, frontB = rf};
    }



    private Plane GetLocalUnityPlane(Matrix4x4 localToWorldMatrix, SlicePlane plane)
    {
        var worldToLocalMatrix = localToWorldMatrix.inverse;
        var localBackA = worldToLocalMatrix.MultiplyPoint( plane.backA );
        var localBackB = worldToLocalMatrix.MultiplyPoint( plane.backB );
        var localFrontA = worldToLocalMatrix.MultiplyPoint( plane.frontA );
        var localPlane = new Plane( localBackA, localBackB, localFrontA );
        return localPlane;
    }

    private bool VertPositionedOnCutSideOfPlane(Vector3 worldVert, SlicePlane plane, bool flip = false)
    {
        //we need to get our plane 'normal' and get the dot against the localVert 
        var myPlane = new Plane(plane.backA, plane.frontA, plane.frontB);

        return myPlane.GetSide(worldVert) != flip;
    }


    private Vector2 GetUV(Vector3 vertPos, int tri0, int tri1, int tri2)
    {
        var point0 = vertsBuffer[tri0];
        var point1 = vertsBuffer[tri1];
        var point2 = vertsBuffer[tri2];

        var uv0 = uvBuffer[tri0];
        var uv1 = uvBuffer[tri1];
        var uv2 = uvBuffer[tri2];

        var uv = MeshIntersection.GetUV(vertPos, point0, point1, point2, uv0, uv1, uv2);
        return uv;
    }

    private void Log(string log)
    {
        Debug.Log($"[PlayerWeaponBoxCutter] {log}");
    }

    Vector3 debugUp;
    Vector3 debugRight;
    Vector3 debugForward;

    Vector3 rawHitPoint;
    Vector3 rawHitNorm;

    private void OnDrawGizmos()
    {
        if(player == null || player.PlayerCamera == null)
            return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(nearestHitPoint, Vector3.one * 0.025f);
        Gizmos.DrawLine(nearestHitPoint, nearestHitPoint + nearestHitNormal * 0.1f);

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(rawHitPoint, Vector3.one * 0.025f);
        Gizmos.DrawLine(rawHitPoint, rawHitPoint + rawHitNorm * 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(nearestHitPoint, nearestHitPoint + debugForward);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(nearestHitPoint, nearestHitPoint + debugUp);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(nearestHitPoint, nearestHitPoint + debugRight);

        Gizmos.color = Color.cyan;
        DrawCastPoints(ePlane.BOTTOM);
        DrawCastPoints(ePlane.TOP);
        DrawCastPoints(ePlane.LEFT);
        DrawCastPoints(ePlane.RIGHT);
        DrawCastPoints(ePlane.MIDDLE_HORIZONTAL);
        DrawCastPoints(ePlane.POLYGON);
    }

    private void DrawCastPoints(ePlane plane)
    {
        if(lastFrameCastPoints.TryGetValue(plane, out var list))
        {
            DrawLines(list);
        }
    }
    private static void DrawLines(List<Vector3> points)
    {
         for(int i = 0; i < points.Count - 1; i++)
        {
            Gizmos.DrawLine( points[i], points[i + 1] );
        }

    }
}
