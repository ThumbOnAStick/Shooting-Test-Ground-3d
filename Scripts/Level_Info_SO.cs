using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Level_Info_SO;

[CreateAssetMenu(menuName = "Misc/LevelInfo", fileName = "LevelInfor", order = 2)]
public class Level_Info_SO : ScriptableObject
{
    public string level_name;
    public int map_size;
    [Serializable]
    public class cell_info
    {
        public Vector2Int place_location;
        [SerializeField]
        public Place_Data_SO place_data;

        public cell_info_string InfoString()
        {
            return new()
            {
                place_location = place_location,
                place_data_name = place_data.name,
            };
        }

    }
    [Serializable]
    public class cell_info_string
    {
        public Vector2Int place_location;
        public string place_data_name;

        public cell_info Packed()
        {
            string path = "PlaceDatas/" + place_data_name;

            return new()
            {
                place_location = place_location,
                place_data = Resources.Load<Place_Data_SO>(path)
            };
        }
    }

    public List<cell_info> cell_infos;



    public Level_Info_File SerializedInfo()
    {
        List<cell_info_string> cell_info_stirngs = new();
        cell_infos.ForEach(x => cell_info_stirngs.Add(x.InfoString()));
        return new()
        {
            _level_name = level_name,
            _map_size = map_size,
            _cell_info_strings = cell_info_stirngs
        };
    }

    public void Save(string level_name)
    {
        Level_Info_File class_file = SerializedInfo();
        string json_file =JsonUtility.ToJson(class_file);
        string path= Application.dataPath+"/LevelDatas/"+ level_name;
        System.IO.File.WriteAllText(path, json_file);
    }
}

public class Level_Info_File
{
    public string _level_name;
    public int _map_size;
    public List<cell_info_string> _cell_info_strings;



    public Level_Info_SO Packed()
    {
        List<cell_info> _cell_infos = new();
        //_cell_info_strings.ForEach(x => _cell_infos.Add(x.Packed()));
        Place_Data_SO pointer=ScriptableObject.CreateInstance<Place_Data_SO>();
        string pointer_name = pointer.name;
        int length =_cell_info_strings.Count;
        
        for(int i = 0; i < length; i++)
        {
            string next_name = _cell_info_strings[i].place_data_name;
            if (pointer_name != next_name)
            {
                pointer =Resources.Load<Place_Data_SO>("PlaceDatas/"+ next_name);
                pointer_name= next_name;
            }
            _cell_infos.Add(new() { place_data = pointer, place_location = _cell_info_strings[i].place_location });
        }

        Level_Info_SO file =ScriptableObject.CreateInstance<Level_Info_SO>();
        file.cell_infos = _cell_infos;
        file.level_name = _level_name;
        file.map_size = _map_size;

        return file;
     
    }
}

public static class ReadLevel 
{
    public static bool ReadLevelByName(string _level_name,out Level_Info_SO output)
    {
        string path = Application.dataPath + "/LevelDatas/" + _level_name;
        if (!File.Exists(path))
        {
            output = ScriptableObject.CreateInstance<Level_Info_SO>();
            output.level_name= _level_name;
            output.cell_infos = new();
            output.map_size = 10;
            return false;
        }
        string json_file = File.ReadAllText(path);    
        Level_Info_File class_file =JsonUtility.FromJson<Level_Info_File>(json_file);
        output = class_file.Packed();
        return true;
    } 
}
