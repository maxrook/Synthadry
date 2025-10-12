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
    public bool IsOpen { get { return isOpen; } }
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }
    [ContextMenu("OpenDoor")]
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
    private void OnCollisionEnter(Collision collision)
    {
        if (!isOpen)
            return;
        if (collision.gameObject.GetComponent<PlayerMovement>())
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