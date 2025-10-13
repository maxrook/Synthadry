using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private MobController mobController;
    [SerializeField] private DoorController door;
    private void Awake()
    {
        mobController.StateChanged += OnMobStateChanged;
    }

    private void OnMobStateChanged(MobController.MobState state)
    {
        if (state == MobController.MobState.Dead)
        {
            door.StartOpening();
        }
    }
}
