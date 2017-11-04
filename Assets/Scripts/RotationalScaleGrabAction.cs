using System.Collections;
using System.Collections.Generic;
namespace VRTK.SecondaryControllerGrabActions
{
    using UnityEngine;

    public class RotationalScaleGrabAction : VRTK_BaseGrabAction
    {
        [Tooltip("The distance the secondary controller must move away from the original grab position before the secondary controller auto ungrabs the object.")]
        public float ungrabDistance = 1f;

        // Scaling variables
        protected Vector3 initialScale;
        protected Vector3 initialGrabPoint;
        protected float initialSeparation;

        // Rotating variables
        protected Quaternion initialRotation;
        protected Vector3 initialDistanceVector;
        protected Vector3 translationOffset;
        protected int leftHandPrimary = 1;

        /// <summary>
        /// The Initalise method is used to set up the state of the secondary action when the object is initially grabbed by a secondary controller.
        /// </summary>
        /// <param name="currentGrabbedObject">The Interactable Object script for the object currently being grabbed by the primary controller.</param>
        /// <param name="currentPrimaryGrabbingObject">The Interact Grab script for the object that is associated with the primary controller.</param>
        /// <param name="currentSecondaryGrabbingObject">The Interact Grab script for the object that is associated with the secondary controller.</param>
        /// <param name="primaryGrabPoint">The point on the object where the primary controller initially grabbed the object.</param>
        /// <param name="secondaryGrabPoint">The point on the object where the secondary controller initially grabbed the object.</param>
        public override void Initialise(VRTK_InteractableObject currentGrabbedObject, VRTK_InteractGrab currentPrimaryGrabbingObject, VRTK_InteractGrab currentSecondaryGrabbingObject, Transform primaryGrabPoint, Transform secondaryGrabPoint)
        {
            base.Initialise(currentGrabbedObject, currentPrimaryGrabbingObject, currentSecondaryGrabbingObject, primaryGrabPoint, secondaryGrabPoint);

            // Calculate the initial offset of the primary grab point from it's actual grabber so we don't have a sudden jump upon placing the object
            translationOffset = primaryGrabbingObject.controllerAttachPoint.transform.position - primaryInitialGrabPoint.position;
            Vector3 rightDir = primaryGrabbingObject.controllerAttachPoint.transform.position - secondaryGrabbingObject.controllerAttachPoint.transform.position;

            leftHandPrimary = 1;

            // If the primary grabbing controller is not the left hand...
            if ((primaryGrabbingObject.transform.rotation * primaryGrabbingObject.transform.position).x > (primaryGrabbingObject.transform.rotation * secondaryGrabbingObject.transform.position).x)
            {
                leftHandPrimary *= -1;
            }
            
            // Pull the grab points off of the object, rotate the object appropriately, then reparent the grab points
            Transform parentP = primaryGrabPoint.parent;
            Transform parentS = secondaryGrabPoint.parent;
            primaryGrabPoint.parent = null;
            secondaryGrabPoint.parent = null;

            transform.right = rightDir * leftHandPrimary;

            primaryGrabPoint.parent = parentP;
            secondaryGrabPoint.parent = parentS;

            // Set initial values
            initialScale = currentGrabbedObject.transform.localScale;
            initialRotation = currentGrabbedObject.transform.localRotation;

            initialGrabPoint = secondaryGrabPoint.position;
            initialDistanceVector = primaryGrabbingObject.transform.position - secondaryGrabbingObject.transform.position;
            initialSeparation = initialDistanceVector.magnitude;
        }

        /// <summary>
        /// The ProcessUpdate method runs in every Update on the Interactable Object whilst it is being grabbed by a secondary controller.
        /// </summary>
        public override void ProcessUpdate()
        {
            base.ProcessUpdate();
            CheckForceStopDistance(ungrabDistance);
        }

        /// <summary>
        /// The ProcessFixedUpdate method runs in every FixedUpdate on the Interactable Object whilst it is being grabbed by a secondary controller and performs the scaling action.
        /// </summary>
        public override void ProcessFixedUpdate()
        {
            base.ProcessFixedUpdate();
            if (initialised)
            {
                ScaleObjectX();
                RotateObject();

                if (grabbedObject.grabAttachMechanicScript.precisionGrab)
                {
                    transform.Translate(primaryGrabbingObject.controllerAttachPoint.transform.position - primaryInitialGrabPoint.position - translationOffset, Space.World);
                }
            }
        }

        public Transform GrabPoint()
        {
            if (primaryGrabbingObject)
            {
                return primaryGrabbingObject.controllerAttachPoint.transform;
            }
            return null;
        }

        public Transform InitialGrabPoint()
        {
            return primaryInitialGrabPoint;
        }

        public bool ScaleByGrabbedPoint(Vector3 scale)
        {
            if (primaryGrabbingObject != null && primaryInitialGrabPoint != null)
            {
                transform.localScale = scale;
                transform.Translate(primaryGrabbingObject.controllerAttachPoint.transform.position - primaryInitialGrabPoint.position - translationOffset, Space.World);
                return true;
            }

            return false;
        }

        protected virtual void ApplyScale(Vector3 newScale)
        {
            Vector3 existingScale = grabbedObject.transform.localScale;

            if (newScale.x > 0)
            {
                grabbedObject.transform.localScale = new Vector3(newScale.x, existingScale.y, existingScale.z); ;
            }
        }

        protected virtual void ScaleObjectX()
        {
            float currentSeparation = (primaryGrabbingObject.transform.position - secondaryGrabbingObject.transform.position).magnitude;

            var newScale = new Vector3(currentSeparation - initialSeparation, 0f, 0f) + initialScale;
            ApplyScale(newScale);
        }

        protected virtual void RotateObject()
        {
            transform.right = primaryGrabbingObject.controllerAttachPoint.transform.position - secondaryGrabbingObject.controllerAttachPoint.transform.position;
            transform.right *= leftHandPrimary;
        }
    }
}