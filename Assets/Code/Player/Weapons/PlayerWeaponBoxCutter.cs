using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//need ability to orient picked up things.
//need to be able to move ... smoothly
//need for if i'm not pointing directly at a thing, for our plane cast to catch it
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
    }

    private List<CutResult> cuttingResults = new List<CutResult>();

    private List<Vector3> lastFrameCastPoints = new(32);
    private List<TriCutPoint> lastFrameTriCutPoints = new(64);

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


    public float planeRotation = 0f;
    public float planeRotationSpeed = 45f;
    public float sizeChangeSpeed = 1f;
    private float minSize = 0.1f;
    private float maxSize = 1f;

    public bool cutOnlyNearest;

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
    }

    private void Checking()
    {
        ClearCache();
        
        //we want to cut whatever we see in front of us (just the first thing) 
        var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var rayOrigin = ray.origin; 


        int ignoreLayer = LayerMask.NameToLayer("Player");
        int layerMask = ~(1 << ignoreLayer);

        var boxExtents = new Vector3(width, height, 0.01f);
        var numHits = Physics.BoxCastNonAlloc(rayOrigin, boxExtents, ray.direction, castHitBuffer, transform.rotation, depth, layerMask);

        if (numHits > 0)
        {
            //using a simple raycast, find the thing we're actually pointing at
            var rayHit = Physics.Raycast(rayOrigin, ray.direction, out var hitInfo, depth, layerMask);

            if (rayHit == false)
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
            }

            var bodyHit = hitInfo.collider.attachedRigidbody;
            if (bodyHit != null)
            {
                var allColliders = bodyHit.GetComponentsInChildren<Collider>();
                foreach (var collider in allColliders)
                {
                    var meshFilter = collider.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        CastPlaneAgainstMesh(collider, meshFilter, ePlane.BOTTOM, shortenedPlanes: true);
                        CastPlaneAgainstMesh(collider, meshFilter, ePlane.TOP, shortenedPlanes: true);
                        CastPlaneAgainstMesh(collider, meshFilter, ePlane.RIGHT, shortenedPlanes: true);
                        CastPlaneAgainstMesh(collider, meshFilter, ePlane.LEFT, shortenedPlanes: true);
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
        var localBackA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backA);
        var localBackb = meshWorldMatrix.MultiplyPoint3x4(worldPlane.backB);
        var localFrontA = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontA);
        var localFrontB = meshWorldMatrix.MultiplyPoint3x4(worldPlane.frontB);

        //cache this?
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
                        lastFrameCastPoints.Add(worldIntersectionPoint);
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
                    triCut.cutPlane = new Plane(localBackA, localFrontB, localBackb);
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
                colliders.Add(cutpoint.collider);
        }

        foreach(var collider in colliders)
        {
            var meshCollider = TryConvertColliderIntoMesh(collider);
            Cut(meshCollider);
        }
    }

    private void Cut(MeshCollider meshCollider)
    {
        GameObject finalObject = null;
        var meshFilter = meshCollider.GetComponent<MeshFilter>();
        var plane = ePlane.TOP;
        
        plane = ePlane.BOTTOM;

        ClearCache();
        CastPlaneAgainstMesh(meshCollider, meshFilter, plane);
        TryCuttingMeshCollider(meshCollider, lastFrameTriCutPoints, plane, false);

        Log($"cut result count [{plane}]: [{cuttingResults.Count}]");
        if(cuttingResults.Count > 0)
        {
            ResolveCutResult(cuttingResults[0], false);
            meshCollider = cuttingResults[0].cutCreatedMeshCollider;
            meshFilter = cuttingResults[0].cutCreatedMeshFilter;
            finalObject = cuttingResults[0].cutCreatedObject;
        }

        plane = ePlane.TOP;
        ClearCache();
        CastPlaneAgainstMesh(meshCollider, meshFilter, plane);
        TryCuttingMeshCollider(meshCollider, lastFrameTriCutPoints, plane, false);

        Log($"cut result count [{plane}]: [{cuttingResults.Count}]");
        if(cuttingResults.Count > 0)
        {
            ResolveCutResult(cuttingResults[0], false);
            meshCollider = cuttingResults[0].cutCreatedMeshCollider;
            meshFilter = cuttingResults[0].cutCreatedMeshFilter;
            finalObject = cuttingResults[0].cutCreatedObject;
        }

        plane = ePlane.LEFT;
        ClearCache();
        CastPlaneAgainstMesh(meshCollider, meshFilter, plane);
        TryCuttingMeshCollider(meshCollider, lastFrameTriCutPoints, plane, false);

        Log($"cut result count [{plane}]: [{cuttingResults.Count}]");
        if(cuttingResults.Count > 0)
        {
            ResolveCutResult(cuttingResults[0], false);
            meshCollider = cuttingResults[0].cutCreatedMeshCollider;
            meshFilter = cuttingResults[0].cutCreatedMeshFilter;
            finalObject = cuttingResults[0].cutCreatedObject;
        }

        plane = ePlane.RIGHT;
        ClearCache();
        CastPlaneAgainstMesh(meshCollider, meshFilter, plane);
        TryCuttingMeshCollider(meshCollider, lastFrameTriCutPoints, plane, false);

        Log($"cut result count [{plane}]: [{cuttingResults.Count}]");
        if(cuttingResults.Count > 0)
        {
            ResolveCutResult(cuttingResults[0], false);
            meshCollider = cuttingResults[0].cutCreatedMeshCollider;
            meshFilter = cuttingResults[0].cutCreatedMeshFilter;
            finalObject = cuttingResults[0].cutCreatedObject;
        }

        if(finalObject != null)
        {
            GameObject.Destroy(finalObject);
        }
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
            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, plane);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, plane);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, plane);
            
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

            BuildCap(triList, facePlane, triBuffer, true);

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

            BuildCap(triList2, facePlane, triBuffer2, true);

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
            
            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, plane);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, plane);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, plane);

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
        var limitPlane = GetLocalUnityPlane(localToWorldMatrix, plane);
        BuildCap(newTris, limitPlane, triBuffer, true);

        var result = MeshIntersection.TidyMesh(triBuffer, vertsBuffer, normalsBuffer, uvBuffer);

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

            var point0OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint0, plane, true);
            var point1OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint1, plane, true);
            var point2OnCutSideOfPlane = VertPositionedOnCutSideOfPlane(worldPoint2, plane, true);

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
        limitPlane = GetLocalUnityPlane(localToWorldMatrix, plane);
        BuildCap(newTris, limitPlane, triBuffer2, false);

        result = MeshIntersection.TidyMesh(triBuffer2, vertsBuffer, normalsBuffer, uvBuffer);

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
        clone.name = originalObject.name + "_" + plane;
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

    private (Vector3 backA, Vector3 backB, Vector3 frontA, Vector3 frontB) GetPlane(ePlane plane, bool shortened = false)
    {
        var planeDepth = depth;
        var planeWidth = width;

        if(shortened == false)
        {
            planeDepth = 100f;
            planeWidth = 100f;
        }

        var source = player.PlayerCamera.transform;

        var forward = source.forward * planeDepth ;
        var right = source.right * planeWidth / 2f;
        var up = source.up * height / 2f;

        float rotation = 0;
        switch(plane)
        {
            case ePlane.BOTTOM: rotation = 0f;break;
            case ePlane.TOP: rotation = 180f;break;
            case ePlane.LEFT:
             rotation = 270f;
             up = source.up * width / 2f;
            break;
            case ePlane.RIGHT: 
            rotation = 90f;
             up = source.up * width / 2f;
            break;
        }

        rotation += planeRotation;
        var orientation = Quaternion.AngleAxis(rotation, forward);
        right = orientation * right;
        up = orientation * up;

        var center = source.position - up;

        Vector3 lb = center + (-right) + (-forward);
        Vector3 lf = center + (-right) + forward;
        Vector3 rb = center + right + (-forward);
        Vector3 rf = center + right + forward;

        return (lb, rb, lf, rf);
    }


    private Plane GetLocalUnityPlane(Matrix4x4 localToWorldMatrix, ePlane plane)
    {
        var worldPlane = GetPlane(plane);

        var worldToLocalMatrix = localToWorldMatrix.inverse;
        var localBackA = worldToLocalMatrix.MultiplyPoint( worldPlane.backA );
        var localBackB = worldToLocalMatrix.MultiplyPoint( worldPlane.backB );
        var localFrontA = worldToLocalMatrix.MultiplyPoint( worldPlane.frontA );
        var localPlane = new Plane( localBackA, localBackB, localFrontA );
        return localPlane;
    }

    private bool VertPositionedOnCutSideOfPlane(Vector3 worldVert, ePlane plane, bool flip = false)
    {

        //we need to get our plane 'normal' and get the dot against the localVert 
        var planeVerts = GetPlane(plane);
        var myPlane = new Plane(planeVerts.backA, planeVerts.frontA, planeVerts.frontB);

        return myPlane.GetSide(worldVert) != flip;
    }

    private void CaptureMatchedWindingPlane(int tri0, int tri1, int tri2, Plane referencePlane, List<int> triBuffer)
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

    private class CapVert
    {
        public int tri;
        public Vector3 vert;

        public float rotationalAngle;
    }


    private void BuildCap(List<int> tris, Plane referencePlane, List<int> triBuffer, bool flip)
    {
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

        Plane testPlane;
        if(flip)
        {
            testPlane = new Plane(-referencePlane.normal, -referencePlane.distance);
        }
        else
        {
            testPlane = referencePlane;
        }
        //and then build a cap using a sort of 'fan'
        for(int i = 1 ; i < capVerts.Count - 1; i++)
        {
            //tri fan
            var tri0 = capVerts[0].tri;
            var tri1 = capVerts[i].tri;
            var tri2 = capVerts[i + 1].tri;

            CaptureMatchedWindingPlane(tri0, tri1, tri2, testPlane, triBuffer);
        }
    }

    private class CapVertPair
    {
        public int tri;

        public Vector3 activeVert;
        public Vector3 inactiveVert;
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

        Gizmos.color = Color.red;
        for(int i = 0; i < lastFrameCastPoints.Count - 1; i++)
        {
            Gizmos.DrawLine( lastFrameCastPoints[i], lastFrameCastPoints[i + 1] );
            //Gizmos.DrawCube(lastFrameCastPoint, pointSize * 2);
        }
    }

}
