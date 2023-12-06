using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TutorialSystem : game_manager_client
{
    public List<Transform> locations;
    public List<GameObject> enemies;
    public GameObject way_block_prefab;

    GameObject way_block_instance;

    [HideInInspector]
    public tut current_tut;

    private void Start()
    {
        current_tut = forward;
    }

    public void Update()
    {

        current_tut.update();
        Type current_type = current_tut.GetType();
        if (current_type == typeof(way_point_tut))
        {
            way_point_tut _current_tut = current_tut as way_point_tut;
            if (way_block_instance == null)
            {
                way_block_instance = Instantiate(way_block_prefab, _current_tut.target.position + Vector3.down * .9f, Quaternion.identity);
            }
            else
            {
                way_block_instance.transform.position = _current_tut.target.position + Vector3.down * .9f;
            }
        }
        else
        {
            if (way_block_instance != null)
                Destroy(way_block_instance);

        }
    }
    string subtitle_folder
    {
        get
        {
            return "Subtitles/";
        }
    }

    keyboard_tut forward
    {
        get
        {
            return new() { tut_system = this, target_key = KeyCode.W, next_tut = backward, sub = Resources.Load<Subtitile_SO>(subtitle_folder + "Forward") };
        }
    }

    keyboard_tut backward
    {
        get
        {
            return new() { tut_system = this, target_key = KeyCode.S, next_tut = turn, sub = Resources.Load<Subtitile_SO>(subtitle_folder + "Backward") };


        }
    }
    way_point_tut turn
    {
        get
        {
            return new() { tut_system = this, target = locations[0], next_tut = kill, sub = Resources.Load<Subtitile_SO>(subtitle_folder + "Turn") };
        }
    }
    destroy_target_tut kill
    {
        get
        {
            return new() { tut_system = this, target = enemies[0], next_tut = recruit, sub = Resources.Load<Subtitile_SO>(subtitle_folder + "Kill") };
        }
    }
    destroy_target_tut recruit
    {
        get
        {
            return new() { tut_system = this, target = enemies[1], next_tut = kill2, sub = Resources.Load<Subtitile_SO>(subtitle_folder + "Recruit") };
        }
    }
    destroy_target_tut kill2
    {
        get
        {
            return new() { tut_system = this, target = enemies[2], next_tut = null, sub = Resources.Load<Subtitile_SO>(subtitle_folder + "Kill2") };

        }
    }

}

public class tut
{
    public Subtitile_SO sub;
    public tut next_tut;
    public TutorialSystem tut_system;
    public game_manager manager
    {
        get
        {
            return tut_system._game_manager;
        }
    }
    public IEnumerator end_tut()
    {
        yield return new WaitForSeconds(1f);
        if (next_tut != null)
            tut_system.current_tut = next_tut;
    }
    public virtual void update()
    {
        if (sub.text != null)
            manager.subtitle_text.text = sub.text;
    }
}

public class keyboard_tut : tut
{
    public KeyCode target_key;
    public override void update()
    {
        base.update();
        if (Input.GetKeyDown(target_key))
        {
            manager.StartCoroutine(end_tut());
        }
    }
}

public class way_point_tut : tut
{
    public Transform target;
    public override void update()
    {
        base.update();
        if (manager.player == null)
            return;
        if (Vector3.Distance(manager.player.transform.position, target.position) < 1)
        {
            manager.StartCoroutine(end_tut());
        }
    }
}

public class destroy_target_tut : tut
{
    public GameObject target;
    public override void update()
    {
        base.update();
        if (target == null)
        {
            manager.StartCoroutine(end_tut());
        }
    }
}
