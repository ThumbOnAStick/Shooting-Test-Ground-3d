using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName ="subtitle",menuName ="subtitle/subtitle",order =0)]
public class Subtitile_SO : ScriptableObject
{
    [TextArea]
    public string text;
}
