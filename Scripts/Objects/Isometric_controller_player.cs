using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Isometric;
using stg_grid;
using tank_ui;
using UnityEngine.UI;
using System.Linq;
using System;

public class Isometric_controller_player : Isometric_unit
{


  
    public override void start_calls()
    {
        StartCoroutine(Start_Locking());
        StartCoroutine(cursor_update());
    }

    public override void update_methods()
    {
        if (_game_manager.pause)
        {
            return;
        }
        //interface
        check_movement();
        check_weaponary();
        
        //ui
        ammo_update();
        cursor_update();


    }


    public void ammo_update()
    {
        ammo.text = weapon_info.current_num.ToString();
    }
  
  
    public IEnumerator cursor_update()
    {


        RectTransform bottom_left = _game_manager.bottom_left;
        RectTransform top_right = _game_manager.top_right;
        Text lock_text = _game_manager.lock_text;


        lock_text.gameObject.SetActive(current_target != null);
        bottom_left.gameObject.SetActive(current_target != null);
        top_right.gameObject.SetActive(current_target != null);
        //color
        List<Image> b_ls = bottom_left.GetComponentsInChildren<UnityEngine.UI.Image>().ToList();
        List<Image> t_rs = top_right.GetComponentsInChildren<UnityEngine.UI.Image>().ToList();

        if (weapon_info.shooting)
        {
            b_ls.ForEach(x => x.color = Color.red);
            t_rs.ForEach(x => x.color = Color.red);
        }
        else if (weapon_info.reloading)
        {
            b_ls.ForEach(x => x.color = Color.white);
            t_rs.ForEach(x => x.color = Color.white);
        }
        else
        {
            b_ls.ForEach(x => x.color = Color.black);
            t_rs.ForEach(x => x.color = Color.black);
        }


        //target
        if (current_target == null)
        {
            yield return new WaitForSeconds(duration);
            cursor_update_next_move();
            yield break;
        }

        if (!i_can_see(current_target))
        {
            yield return new WaitForSeconds(duration);
            cursor_update_next_move();
            yield break;

        }

        Vector2 bottom_left_world_to_cam = current_target.bottom_left_on_screen(1f);
        Vector2 top_right_world_to_cam = current_target.top_right_on_screen(1f);

        //top left
        if (Vector2.Distance(bottom_left.position, bottom_left_world_to_cam) > 0.1f)
        {
            Vector2 dir1 = (bottom_left_world_to_cam - (Vector2)bottom_left.position);
            if (dir1.magnitude < 5)
            {
                bottom_left.position = bottom_left_world_to_cam;
                yield return new WaitForSeconds(duration);
                cursor_update_next_move();
                yield break;

            }


            bottom_left.position += (Vector3)dir1 * duration * 5;
        }
        bottom_left.position = new Vector3(Mathf.Clamp(bottom_left.position.x, 0, _game_manager.w - 10), Mathf.Clamp(bottom_left.position.y, 0, _game_manager.h - 10));


        //top right
        if (Vector2.Distance(top_right.position, top_right_world_to_cam) > 0.1f)
        {
            Vector2 dir2 = (top_right_world_to_cam - (Vector2)top_right.position);
            if (dir2.magnitude < 5)
            {
                top_right.position = top_right_world_to_cam;
                yield return new WaitForSeconds(duration);
                cursor_update_next_move();
                yield break;

            }
            top_right.position += (Vector3)dir2 * duration * 5;
        }
        top_right.position = new Vector3(Mathf.Clamp(top_right.position.x, 0, _game_manager.w - 10), Mathf.Clamp(top_right.position.y, 0, _game_manager.h - 10));

        yield return new WaitForSeconds(duration);
        cursor_update_next_move();
    }

    public void cursor_update_next_move()
    {
        StartCoroutine(cursor_update());
    }


    //gameplaly
    public void check_movement()
    {
        //movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 input = new() { x=horizontal, y = vertical};
        is_moving = input != Vector2.zero;
        Vector3 forward = transform.forward.normalized;
        Vector2 forward_flat = new Vector2(forward.x, forward.z).normalized;
        Vector2 next_location = flat_po + forward_flat*input.y;
        Vector2Int next_cell = new Vector2Int(Mathf.FloorToInt(next_location.x), Mathf.FloorToInt(next_location.y));
        int block_factor = 1;
        if (_game_manager.cells.ContainsKey(next_cell) && _game_manager.cells[next_cell].accessible == false)
        {
            block_factor = 0;
        }

        float spin_dir = 1;
        float spin_factor = 1;
        if (input.y != 0)
        {
            transform.position += Time.deltaTime * speed * forward * input.y * block_factor;

            spin_dir = vertical;
            spin_factor = 2;
        }

        if (input.x != 0)
        {
            float sin_speed = 50 * spin_dir;
            if (horizontal > 0)
            {
             
                change_dir(sin_speed, spin_factor);
            }
            else
            {
                change_dir(-sin_speed, spin_factor);
            }

        }


    }

    public void check_weaponary()
    {

        if (Input.GetKey(KeyCode.Space))
        {
            int before = weapon_info.current_num;
            StartCoroutine(Shoot(new Vector2(turret.forward.x, turret.forward.z).normalized));
            int after = weapon_info.current_num;
            if (before == after)
            {
                return;
            }

        }
    }

}


