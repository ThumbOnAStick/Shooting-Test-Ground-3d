using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlaceData", menuName = "Misc/PlaceData", order = 1)]
[Serializable]
public class Place_Data_SO : ScriptableObject
{
    public GameObject prefab;
    public int layer;
    public Color color;
}
