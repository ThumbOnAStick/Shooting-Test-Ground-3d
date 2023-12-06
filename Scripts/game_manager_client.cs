using stg_grid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class game_manager_client : MonoBehaviour
{
    [HideInInspector]
    public game_manager _game_manager 
    {
        get
        {
            return FindObjectOfType<game_manager>();
        }
    }
  
    public Dictionary<Vector2Int, cell> cells
    {
        get
        {
            return _game_manager.cells;
        }
    }
    void Awake()
    {
    
        awake_calls();
    }

    public virtual void awake_calls()
    {

    }
}
