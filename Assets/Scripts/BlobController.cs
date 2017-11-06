using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using VRTK.GrabAttachMechanics;
using VRTK.SecondaryControllerGrabActions;

public class BlobController : MonoBehaviour {
    
    public bool followBeat = true;
    private float flightTime = 0f;

    [Space]

    [Tooltip("Strength by which the blobs hold together.")]
    public float stretchConstant = 1f;

    [Tooltip("The damping value of the blob's springiness.")]
    [Range(0, 1)]
    public float springDamp = .05f;

    [Space]

    public PrefabHolder prefabHolder;
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

    private bool hasBeenGrabbed = false;
    private bool _hasBeenGrabbed = false;

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

        interactable = GetComponent<VRTK_InteractableObject>();
        if (interactable == null)
        {
            Debug.Log("[BlobController] No VRTK_InteractableObject on " + name);
        }

        if (startingScale == Vector3.zero)
        {
            startingScale = transform.localScale;
        }

        _hasBeenGrabbed = false;
    }

    private void OnDestroy()
    {
        Metronome.Instance.onBeat -= Drop;
        Metronome.Instance.onBeat -= Jump;
    }

    // Splits the blob into two blobs in each hand once a threshold for the scale is met
    private void Split()
    {
        // Deal with the controllers
        VRTK_InteractGrab primaryGrabber = interactable.GetGrabbingObject().GetComponent<VRTK_InteractGrab>();
        Vector3 primaryPos = primaryGrabber.controllerAttachPoint.transform.position;
        VRTK_InteractGrab secondaryGrabber = interactable.GetSecondaryGrabbingObject().GetComponent<VRTK_InteractGrab>();
        Vector3 secondaryPos = secondaryGrabber.controllerAttachPoint.transform.position;

        interactable.ForceStopInteracting(true);
        interactable.ForceStopSecondaryGrabInteraction();

        StartCoroutine(GrabAtEndOfFrame(primaryGrabber, secondaryGrabber));

        // Instantiate the blobs
        GameObject leftGO = Instantiate(prefabHolder.prefab);
        GameObject rightGO = Instantiate(prefabHolder.prefab);
        leftGO.GetComponent<Rigidbody>().isKinematic = false;
        rightGO.GetComponent<Rigidbody>().isKinematic = false;

        //leftGO.transform.position = transform.position + transform.rotation * leftBlob;
        leftGO.transform.position = primaryPos * .95f + secondaryPos * .05f;
        leftGO.transform.rotation = transform.rotation;
        leftGO.transform.localScale *= .9f;
        //rightGO.transform.position = transform.position + transform.rotation * rightBlob;
        rightGO.transform.position = secondaryPos * .95f + primaryPos * .05f;
        rightGO.transform.rotation = transform.rotation;
        rightGO.transform.localScale *= .9f;

        leftGO.GetComponent<BlobController>().stretch = .14f;
        leftGO.GetComponent<BlobController>().StartingScale = startingScale * .9f;
        rightGO.GetComponent<BlobController>().stretch = .14f;
        rightGO.GetComponent<BlobController>().StartingScale = startingScale * .9f;

        // Disable current object
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    // Sends useful values out to SuperCollider
	private void OutputControls()
    {

    }

    private void Jump()
    {
        if (transform.position.y < .25f + transform.localScale.y / 2f)
        {
            GetComponent<Rigidbody>().velocity = Vector3.up * Mathf.Abs(Physics.gravity.magnitude) * flightTime;
        }
    }

    private void Drop()
    {
        Debug.Log(flightTime);
        Metronome.Instance.onBeat -= Drop;
        Metronome.Instance.onBeat += Jump;

        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().useGravity = true;
    }

    private void Update()
    {
        hasBeenGrabbed = hasBeenGrabbed || interactable.IsGrabbed();
        if (!interactable.IsGrabbed() && hasBeenGrabbed && !_hasBeenGrabbed)
        {
            if (followBeat)
            {
                float timeToFall = Mathf.Sqrt(Mathf.Abs(2 * transform.position.y / Physics.gravity.magnitude));
                float beats = Metronome.Instance.BeatInSeconds;
                timeToFall = Mathf.Round(timeToFall / beats) * beats;
                flightTime = timeToFall;
                transform.position = new Vector3(transform.position.x,
                                    timeToFall * timeToFall * Mathf.Abs(Physics.gravity.magnitude) / 2f
                                                + transform.localScale.y / 2f,
                                    transform.position.z);

                GetComponent<Rigidbody>().isKinematic = true;
                GetComponent<Rigidbody>().useGravity = false;

                Metronome.Instance.onBeat += Drop;
            }
            else
            {
                GetComponent<Rigidbody>().isKinematic = false;
                GetComponent<Rigidbody>().useGravity = true;
            }

            _hasBeenGrabbed = true;
        }

        if (!grabAction.IsInitialised())
        {
            float scaleDisplacement = startingScale.x - transform.localScale.x;
            float stretchDisplacement = stretch;
            scaleVelocity = Mathf.SmoothDamp(scaleVelocity, scaleDisplacement * springDamp, ref scaleAcceleration, .05f);
            stretchVelocity = Mathf.SmoothDamp(stretchVelocity, stretchDisplacement * springDamp, ref stretchAcceleration, .05f);
            Vector3 springScale = transform.localScale + Vector3.right * scaleVelocity;
            float springStretch = stretch - stretchVelocity;

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

        if (transform.localScale.x <= .01f)
        {
            transform.localScale = new Vector3(.01f, transform.localScale.y, transform.localScale.z);
        }

        float scaleMultiplier = .5f + .5f * startingScale.x / transform.localScale.x;
        transform.localScale = new Vector3(transform.localScale.x,
                                   startingScale.y * scaleMultiplier,
                                   startingScale.z * scaleMultiplier);

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
