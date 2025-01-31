using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshcutView : MonoBehaviour
{
    [SerializeField] private MeshCutManager _meshCutManager;

    public void OnClickReset()
    {
        _meshCutManager.ResetFunc();
    }

    public void OnClickSwitchObject()
    {
        _meshCutManager.SwitchObject();
    }
}
