using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using VRTK.SecondaryControllerGrabActions;

public class BlobController : MonoBehaviour {

    [Tooltip("Strength by which the blobs hold together.")]
    public float stretchConstant = 1f;

    [Tooltip("The damping value of the blob's springiness.")]
    [Range(0, 1)]
    public float springDamp = .05f;

    private MCBlob blob;
    private VRTK_InteractableObject interactable;

    private RotationalScaleGrabAction grabAction;
    private Vector3 startingScale;

    Transform primaryGrabPoint;
    Transform primaryInitialGrabPoint;
    Transform secondaryGrabPoint;
    Transform secondaryInitialGrabPoint;

    private float scaleVelocity = 0f;
    private float scaleAcceleration = 0f;
    private float stretchVelocity = 0f;
    private float stretchAcceleration = 0f;
    
    private float stretch = 0f;
    public bool holdStretch = false;

    public float Stretch
    {
        get { return stretch; }
    }

    public Vector3 StartingScale
    {
        get { return startingScale; }
        set { startingScale = value; }
    }

    public void ApplyStretch(float stretchValue)
    {
        stretch = stretchValue * stretchConstant;
    }

    private void Start()
    {
        grabAction = GetComponent<RotationalScaleGrabAction>();
        if (grabAction == null)
        {
            Debug.Log("[BlobController] No RotationScaleGrabAction script on " + name);
        }
        blob = GetComponent<MCBlob>();
        if (blob == null)
        {
            Debug.Log("[BlobController] No MCBlob script on " + name);
        }
        interactable = GetComponent<VRTK_InteractableObject>();
        if (interactable == null)
        {
            Debug.Log("[BlobController] No VRTK_InteractableObject on " + name);
        }

        if (startingScale == Vector3.zero)
        {
            startingScale = transform.localScale;
        }
    }

    // Splits the blob into two blobs in each hand once a threshold for the scale is met
    private void Split()
    {

        // Deal with the controllers
        VRTK_InteractGrab primaryGrabber = interactable.GetGrabbingObject().GetComponent<VRTK_InteractGrab>();
        VRTK_InteractGrab secondaryGrabber = interactable.GetSecondaryGrabbingObject().GetComponent<VRTK_InteractGrab>();
        interactable.ForceStopInteracting(true);
        interactable.ForceStopSecondaryGrabInteraction();

        StartCoroutine(GrabAtEndOfFrame(primaryGrabber, secondaryGrabber));

        // Instantiate the blobs
        Vector3 leftBlob = new Vector3(blob.blobs[0][0], blob.blobs[0][1], blob.blobs[0][2]);
        Vector3 rightBlob = new Vector3(blob.blobs[1][0], blob.blobs[1][1], blob.blobs[1][2]);

        GameObject leftGO = Instantiate(this.gameObject);
        GameObject rightGO = Instantiate(this.gameObject);
        leftGO.GetComponent<Rigidbody>().isKinematic = false;
        rightGO.GetComponent<Rigidbody>().isKinematic = false;

        leftGO.transform.position = transform.position + transform.rotation * leftBlob;
        leftGO.transform.rotation = transform.rotation;
        leftGO.transform.localScale *= .9f;
        rightGO.transform.position = transform.position + transform.rotation * rightBlob;
        rightGO.transform.rotation = transform.rotation;
        rightGO.transform.localScale *= .9f;

        Debug.Log(leftGO.GetComponent<BlobController>().StartingScale);

        leftGO.GetComponent<BlobController>().stretch = .14f;
        leftGO.GetComponent<BlobController>().StartingScale = startingScale * .9f;
        rightGO.GetComponent<BlobController>().stretch = .14f;
        rightGO.GetComponent<BlobController>().StartingScale = startingScale * .9f;

        Debug.Log(leftGO.GetComponent<BlobController>().StartingScale);

        // Disable current object
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    // Sends useful values out to SuperCollider
	private void OutputControls()
    {

    }

    private void Update()
    {
        float scaleDisplacement = startingScale.x - transform.localScale.x;
        float stretchDisplacement = stretch;
        scaleVelocity = Mathf.SmoothDamp(scaleVelocity, scaleDisplacement * springDamp, ref scaleAcceleration, .05f);
        stretchVelocity = Mathf.SmoothDamp(stretchVelocity, stretchDisplacement * springDamp, ref stretchAcceleration, .05f);
        Vector3 springScale = transform.localScale + Vector3.right * scaleVelocity;
        float springStretch = stretch - stretchVelocity;

        if (!grabAction.IsInitialised() && !holdStretch)
        {
            transform.localScale = springScale;
            stretch = springStretch;
        }

        if (grabAction.IsInitialised())
        {
            primaryGrabPoint = grabAction.PrimaryGrabPoint();
            primaryInitialGrabPoint = grabAction.PrimaryInitialGrabPoint();
            secondaryGrabPoint = grabAction.SecondaryGrabPoint();
            secondaryInitialGrabPoint = grabAction.SecondaryInitialGrabPoint();
        }
        else if (primaryGrabPoint && primaryInitialGrabPoint)
        {
            transform.Translate(primaryGrabPoint.position - primaryInitialGrabPoint.position, Space.World);
        }

        if (stretch > 0.271f)
        {
            Split();
        }

        /*if (transform.localScale.x <= .01f)
        {
            transform.localScale = new Vector3(.01f, transform.localScale.y, transform.localScale.z);
        }*/

    }

    IEnumerator GrabAtEndOfFrame(VRTK_InteractGrab primary, VRTK_InteractGrab secondary)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        primary.AttemptGrab();
        secondary.AttemptGrab();

        Destroy(this.gameObject);
    }
}
