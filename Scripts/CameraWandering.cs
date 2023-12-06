using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraWandering : MonoBehaviour
{
    Transform m_c
    {
        get
        {
            return Camera.main.transform;
        }
    }

}
