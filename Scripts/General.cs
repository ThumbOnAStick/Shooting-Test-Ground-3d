using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class General 
{
    public int progress = 0;
    public string test_level="";

    string general_data_path
    {
        get
        {
            return Application.dataPath + "/General/Data";
        }
    }

    public void Save()
    {
        string json= JsonUtility.ToJson(this);
        File.WriteAllText(general_data_path, json);
    }


}


