using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class Lever : Draggable
{
    [SerializeField] bool toggleLever;
    bool toggleReset;

    [SerializeField] HingeJoint hinge;
    public HingeJoint Hinge => hinge;
    
    [SerializeField, Range(0f, 1f)] float threshold = .7f;

    [SerializeField] DraggableReturnType draggableReturnType = DraggableReturnType.NormalizedRotation;

    [SerializeField] AudioClip toggleSound;
    [SerializeField] float toggleSoundVolume = .5f;

    //JointMotor motor;
    JointSpring spring;
    float hingeMaxRotation, hingeMinRotation;
    float stringRotationMax, stringRotationMin;
    float onThreshold, offThreshold;
    Quaternion originalRotation;

    bool isOn;
    float currentValue;

    public bool IsOn
    {
        get { return isOn; }
        set { isOn = value; }
    }


    public UnityEvent<float> OnMove;
    public UnityEvent<bool> OnSwitch;

    float previousFrameHingeAngle;


    private void Start()
    {
        //default position
        spring = hinge.spring;

        originalRotation = transform.rotation;
        hingeMaxRotation = hinge.limits.max;
        hingeMinRotation = hinge.limits.min;
        OnDrag += DragLogic;
        OnDragRelease += DragRelease;

        onThreshold = Mathf.Lerp(hingeMinRotation, hingeMaxRotation, threshold);
        offThreshold = Mathf.Lerp(hingeMaxRotation, hingeMinRotation, threshold);

        stringRotationMax = Mathf.LerpUnclamped(hingeMinRotation, hingeMaxRotation, 1.4f);
        stringRotationMin = Mathf.LerpUnclamped(hingeMaxRotation, hingeMinRotation, 1.4f);

        toggleReset = true;

        if (toggleSound != null) OnSwitch.AddListener(PlayToggleSound);
    }

    public void PlayToggleSound(bool toggle)
    {
        AudioManager.Instance.PlayAudio(toggleSound, transform.position, 50f, toggleSoundVolume, .1f, AudioMixGroup.SFX, true);
    }

    public float LeverNormal()
    {
        return Mathf.InverseLerp(hinge.limits.min, hinge.limits.max, hinge.angle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position +  GetPlaneNormal() * 10f);
    }

    public void DragLogic()
    {

        switch (draggableReturnType)
        {
            case DraggableReturnType.Degrees:
                currentValue = hinge.angle;
                break;
            case DraggableReturnType.NormalizedRotation:
            default:
                currentValue = Mathf.InverseLerp(hinge.limits.min, hinge.limits.max, hinge.angle);
                break;
        }


        OnMove?.Invoke(Mathf.Abs(currentValue));
    }

    public void DragRelease()
    {
    }

    private void Update()
    {
        float hingeAngle = hinge.angle;

        if (toggleLever)
        {
            if (hingeAngle < offThreshold) toggleReset = true;

            if(toggleReset && hingeAngle > onThreshold)
            {
                isOn = !isOn;
                OnSwitch?.Invoke(isOn);
                toggleReset = false;
            }
        }
        else
        {
            bool overThreshold = (isOn ? (hingeAngle < offThreshold) : (hingeAngle > onThreshold));
            if (overThreshold)
            {
                isOn = !isOn;

                spring.targetPosition = isOn ? stringRotationMax : stringRotationMin;
                hinge.spring = spring;
                OnSwitch?.Invoke(isOn);
                //play click sound, turn smth on/off
            }
        }

        previousFrameHingeAngle = hingeAngle;
    }

}
