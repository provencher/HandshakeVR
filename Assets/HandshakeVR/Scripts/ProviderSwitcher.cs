﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Interaction;

using Valve.VR;

using CatchCo;

namespace HandshakeVR
{
    public class ProviderSwitcher : MonoBehaviour
    {
        [SerializeField]
        LeapServiceProvider defaultProvider;

        [SerializeField]
        CustomProvider customProvider;

        bool isDefault = true;

        public bool IsDefault { get { return isDefault; } }

        [Tooltip("If you want to use the SteamVR hand mesh, enable this.")]
        [SerializeField]
        bool hideLeapHandsOnSwitch = false;

        [SerializeField]
        HandModelManager modelManager;

        Leap.Unity.Interaction.InteractionManager interactionManager;
		PlatformControllerManager controllerManager;
        InteractionHand leftInteractionHand;
        InteractionHand rightInteractionHand;

		Leap.Unity.HandModelBase leftAbstractHand;
		Leap.Unity.HandModelBase rightAbstractHand;

		public Leap.Unity.HandModelBase LeftAbstractHandModel { get { return leftAbstractHand; } }
		public Leap.Unity.HandModelBase RightAbstractHandModel { get { return rightAbstractHand; } }

		[Header("SteamVR Variables")]
        [SerializeField]
        SteamVR_Behaviour_Pose leftHandPose;

        [SerializeField]
        GameObject leftHandVisual;
        SteamVRInteractionController leftInteractionController;
		SkeletalControllerHand leftSkeletalControllerHand;

        [SerializeField]
        SteamVR_Behaviour_Pose rightHandPose;

        [SerializeField]
        GameObject rightHandVisual;
        SteamVRInteractionController rightInteractionController;
		SkeletalControllerHand rightSkeletalControllerHand;

        [Header("Debugging")]
        [SerializeField]
        bool manualProviderSwitching = false;

        private void Awake()
        {
            interactionManager = Leap.Unity.Interaction.InteractionManager.instance;
			if(interactionManager != null) controllerManager = interactionManager.GetComponent<PlatformControllerManager>();

			DataHand[] dataHands = modelManager.GetComponentsInChildren<DataHand>(true);

			leftAbstractHand = (HandModelBase)dataHands.First(item => item is DataHand && item.Handedness == Chirality.Left);
			rightAbstractHand = (HandModelBase)dataHands.First(item => item is DataHand && item.Handedness == Chirality.Right);

            if(interactionManager)
            { 
                foreach(InteractionController controller in interactionManager.interactionControllers)
                {
                    if(controller is InteractionHand)
                    {
                        InteractionHand hand = (InteractionHand)controller;

                        if (hand.isLeft) leftInteractionHand = hand;
                        else rightInteractionHand = hand;

						hand.leapProvider = defaultProvider;
                    }
                    else if (controller is SteamVRInteractionController)
                    {
                        SteamVRInteractionController interactionController = (SteamVRInteractionController)controller;

                        if (interactionController.isLeft) leftInteractionController = interactionController;
                        else rightInteractionController = interactionController;
                    }
                }
            }

			SkeletalControllerHand[] hands = transform.parent.GetComponentsInChildren<SkeletalControllerHand>(true);
			leftSkeletalControllerHand = hands.First(item => item.IsLeft);
			rightSkeletalControllerHand = hands.First(item => !item.IsLeft);
        }

        // Use this for initialization
        void Start()
        {
            SetProvider();
        }

        // Update is called once per frame
        void Update()
        {
            if (manualProviderSwitching)
            {
                if (Input.GetKeyUp(KeyCode.Space)) SwitchProviders();
            }
            else
            {
                if (isDefault)
                {
                    if (ShouldUseControllers()) SwitchProviders();
                }
                else
                {
                    if (!ShouldUseControllers()) SwitchProviders();
                }
            }

			if(!isDefault)
			{
				// update our tracking/active state properly.
				leftSkeletalControllerHand.IsActive = leftHandPose.isValid;
				rightSkeletalControllerHand.IsActive = rightHandPose.isValid;
			}
			else
			{
				leftSkeletalControllerHand.IsActive = false;
				rightSkeletalControllerHand.IsActive = false;
			}
        }

        private bool ShouldUseControllers()
        {
            return leftHandPose.isValid || rightHandPose.isValid;
        }

        void SetProvider()
        {
            if (isDefault)
            {
                modelManager.leapProvider = defaultProvider;
				if (leftInteractionHand)
                {
                    leftInteractionHand.leapProvider = defaultProvider;
                }
                if (rightInteractionHand)
                {
                    rightInteractionHand.leapProvider = defaultProvider;
                }
				if (controllerManager) controllerManager.ControllersEnabled = false;
            }
            else
            {
                modelManager.leapProvider = customProvider;
                if (leftInteractionHand)
                {
                    leftInteractionHand.leapProvider = customProvider;
                }
                if (rightInteractionHand)
                {
                    rightInteractionHand.leapProvider = customProvider;
                }
				if (controllerManager) controllerManager.ControllersEnabled = true;

				for (int i=0; i < modelManager.transform.childCount; i++) // this method will fail if the hand objects aren't children of the model manager
                {
					HandModel handModel = modelManager.transform.GetChild(i).GetComponent<HandModel>();

                    if(handModel != null && handModel.HandModelType == ModelType.Graphics) modelManager.transform.GetChild(i).gameObject.SetActive(!hideLeapHandsOnSwitch);
                }
            }

            modelManager.GraphicsEnabled = isDefault || !hideLeapHandsOnSwitch;
            leftHandVisual.gameObject.SetActive(!modelManager.GraphicsEnabled);
            rightHandVisual.gameObject.SetActive(!modelManager.GraphicsEnabled);

            Hands.Provider = (isDefault) ? defaultProvider : (LeapProvider)customProvider;
			customProvider.IsActive = !isDefault;
		}			

        [ExposeMethodInEditor]
        void SwitchProviders()
        {
            isDefault = !isDefault;

            SetProvider();
        }
    }
}