using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK.SecondaryControllerGrabActions;

public class BlobController : MonoBehaviour {
    
    [Tooltip("The damping value of the blob's springiness.")]
    [Range(0, 1)]
    public float springDamp = .05f;

    private RotationalScaleGrabAction grabAction;
    private Vector3 startingScale;

    Transform grabPoint;
    Transform initialGrabPoint;
    private float scaleVelocity = 0f;
    private float scaleAcceleration = 0f;

    private void Start()
    {
        grabAction = GetComponent<RotationalScaleGrabAction>();
        if (grabAction == null)
        {
            Debug.Log("[BlobController] No RotationScaleGrabAction script on " + name);
        }

        startingScale = transform.localScale;
    }

    // Splits the blob into two blobs in each hand once a threshold for the scale is met
    private void Split()
    {

    }

    // Sends useful values out to SuperCollider
	private void OutputControls()
    {

    }

    private void Update()
    {
        float springDisplacement = startingScale.x - transform.localScale.x;
        scaleVelocity = Mathf.SmoothDamp(scaleVelocity, springDisplacement * springDamp, ref scaleAcceleration, .05f);
        Vector3 springScale = transform.localScale + Vector3.right * scaleVelocity;

        if (!grabAction.IsInitialised())
        {
            transform.localScale = springScale;
        }

        if (grabAction.IsInitialised())
        {
            grabPoint = grabAction.GrabPoint();
            initialGrabPoint = grabAction.InitialGrabPoint();
        }
        else if (grabPoint && initialGrabPoint)
        {
            transform.Translate(grabPoint.position - initialGrabPoint.position, Space.World);
        }

        if (transform.localScale.x <= .01f)
        {
            transform.localScale = new Vector3(.01f, transform.localScale.y, transform.localScale.z);
        }
    }
}
