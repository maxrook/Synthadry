using System;
using Unity.VisualScripting;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    Animator _animator;
    [SerializeField] private bool initialOpenState = false;
     private bool isOpen = false;
    [SerializeField] private Material OpenedMaterial;
    [SerializeField] private Material ClosedMaterial;
    [SerializeField] private bool log = false;
    [SerializeField] Rigidbody _rigidbody;

    public bool IsOpen { get { return isOpen; } }
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (initialOpenState)
            StartOpening();
    }
    [ContextMenu("StartOpening")]
    public void StartOpening()  
    {
        _animator.SetBool("isOpen", true);
    }
    public void StartClosing()
    {
        _animator.SetBool("isOpen", false);
    }
    public void TransitNextScene()
    {
        ScenesManager.Instance.ChangeScene(nextSceneName);
    }
    //visuals
    private void SetOpened()
    {
        isOpen= true;
    }
    private void OnTriggerEnter(Collider collider)
    {
        if(log) Debug.Log("OnTriggerEnter");
        if (!isOpen)
            return;
        if (collider.gameObject.GetComponent<PlayerMovement>())
        {
            TransitNextScene();
        }
    }
}
public class DoorVM
{
    private bool isOpen = false;
    public bool IsOpen { get; }
    public DoorVM(bool initialState)
    {
        isOpen = initialState;
    }
    public void Open()
    {
    }
}