using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class LinkedRope : MonoBehaviour
{
    public int numLinks;

    public GameObject link0;
    public Vector3 linkOffset;

    protected List<GameObject> links = new List<GameObject>();
    protected virtual void Awake()
    {
        CreateLinks();
    }

    protected void CreateLinks()
    {
        links.Add(link0);
        for (int i = 0; i < numLinks - 1; i++)
        {
            CreateNewLink();
        }
    }

    private void CreateNewLink()
    {
        var targetLink = links.End();

        var clone = Instantiate(link0, transform);
        clone.transform.localRotation = link0.transform.localRotation;
        clone.transform.localPosition = targetLink.transform.localPosition + linkOffset;
        var joint = clone.GetComponent<CharacterJoint>();
        joint.connectedBody = targetLink.GetComponent<Rigidbody>();

        clone.name = "link" + links.Count;
        links.Add(clone);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
