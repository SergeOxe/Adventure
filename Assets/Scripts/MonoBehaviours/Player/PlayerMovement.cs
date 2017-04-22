using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.EventSystems;


public class PlayerMovement : MonoBehaviour {

    [SerializeField] Animator m_Animator;
    [SerializeField] NavMeshAgent m_Agent;

    [SerializeField] float m_InputHoldDelay = 0.5f;
    [SerializeField] float m_TurnSpeedThreshold = 0.5f;
    [SerializeField] float m_SpeedDampTime = 0.1f;
    [SerializeField] float m_SlowingSpeed = 0.175f;
    [SerializeField] float m_TurnSmoothing = 15f;

    private Interactable m_CurrentInteractable;
    private bool m_HandleInput = true;



    WaitForSeconds m_InputHoldWait;

    Vector3 m_DestinationPosition;
    const float stopDistanceProportion = 0.1f;
    const float navMeshSampleDistance = 4f;

    private readonly int hashSpeedPara = Animator.StringToHash("Speed");
    private readonly int hashLocomotionTag = Animator.StringToHash("Locomotion");


    public const string startingPositionKey = "starting position";


    private void Start()
    {
        m_Agent.updateRotation = false;
        m_InputHoldWait = new WaitForSeconds(m_InputHoldDelay);

        m_DestinationPosition = this.transform.position;
    }

    private void OnAnimatorMove()
    {
        m_Agent.velocity = m_Animator.deltaPosition / Time.deltaTime;
    }

    private void Update()
    {
        if (m_Agent.pathPending)
        {
            return;
        }
  
        float speed = m_Agent.desiredVelocity.magnitude;

        if(m_Agent.remainingDistance <= m_Agent.stoppingDistance * stopDistanceProportion)
        {
            Stopping(out speed);
        }
        else if(m_Agent.remainingDistance <= m_Agent.stoppingDistance)
        {
            Slowing(out speed, m_Agent.remainingDistance);
        }
        else if(speed > m_TurnSpeedThreshold)
        {
            Moving();
        }

        m_Animator.SetFloat(hashSpeedPara, speed, m_SpeedDampTime,Time.deltaTime);

    }

    private void Stopping(out float speed)
    {
        m_Agent.Stop();
        transform.position = m_DestinationPosition;
        speed = 0f;

        if(m_CurrentInteractable)
        {
            transform.rotation = m_CurrentInteractable.interactionLocation.rotation;
            m_CurrentInteractable.Interact();
            m_CurrentInteractable = null;
            StartCoroutine(WaitForInteraction());

        }

    }

    private void Slowing(out float speed,float distanceToDestionation)
    {
        m_Agent.Stop();
        transform.position = Vector3.MoveTowards(transform.position, m_DestinationPosition, m_SlowingSpeed * Time.deltaTime);
        float proportionalDistance  = 1f - distanceToDestionation/m_Agent.stoppingDistance;
        speed = Mathf.Lerp(m_SlowingSpeed,0f,proportionalDistance);

        Quaternion targetRotation = m_CurrentInteractable ? m_CurrentInteractable.interactionLocation.rotation : transform.rotation;

        Quaternion.Lerp(transform.rotation, targetRotation, proportionalDistance);
    }


    private void Moving()
    {
        Quaternion targetRotation = Quaternion.LookRotation(m_Agent.desiredVelocity);
        this.transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, m_TurnSmoothing*Time.deltaTime);
    }

    public void OnGroundClick(BaseEventData data)
    {
        if (!m_HandleInput)
        {
            return;
        }

        m_CurrentInteractable = null;

        PointerEventData pData = (PointerEventData)data;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(
            pData.pointerCurrentRaycast.worldPosition,out hit, 
            navMeshSampleDistance,NavMesh.AllAreas))
        {
            m_DestinationPosition = hit.position;
        }
        else
        {
            m_DestinationPosition = pData.pointerCurrentRaycast.worldPosition;
        }

        m_Agent.SetDestination(m_DestinationPosition);
        m_Agent.Resume();

    }

    public void OnInteractableClicked(Interactable interactable)
    {
        if(!m_HandleInput)
        {
            return;
        }

        m_CurrentInteractable = interactable;
        m_DestinationPosition = interactable.interactionLocation.position;

        m_Agent.SetDestination(m_DestinationPosition);
        m_Agent.Resume();
    }

    IEnumerator WaitForInteraction()
    {
        m_HandleInput = false;
        yield return m_InputHoldWait;

        while (m_Animator.GetCurrentAnimatorStateInfo(0).tagHash != hashLocomotionTag)
        {
            yield return null;
        }
        m_HandleInput = true;

    }
}
