using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ai;
using stg_grid;
using System.Linq;
using Unity.VisualScripting;
using System.Threading.Tasks;
using UnityEngine.Rendering.UI;
using UnityEngine.Rendering;

public class Isometric_controller_ai : Isometric_unit
{

    [HideInInspector]
    public Isometric_unit chase_target;
    [HideInInspector]
    public bool following_path;
    [HideInInspector]
    public bool brake;
    [HideInInspector]
    public Vector2Int next_grid_location;
    [HideInInspector]
    public List<path_node> current_path = new();
    [HideInInspector]   
    public bool triggered;
    public float delay = 1f;
    public Isometric_unit leader
    {
        get
        {
            if (is_player_team)
            {
                if (_game_manager.player == null)
                {
                    return this;
                }
                return _game_manager.player;
            }
            else
            {

                return _game_manager.enemies[0];
            }
        }
    }

    public enum movement_mode { chase, follow };
    public movement_mode mode;
    
    public IEnumerator RndDuration()
    {
        float r = UnityEngine.Random.Range(0, .5f);
        yield return new WaitForSeconds(r);
        StartCoroutine(Think());
    }
    public IEnumerator Think()
    {
        start.Run();
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(Think());
    }


    public override void start_calls()
    {
        base.start_calls();
        StartCoroutine(Start_Locking());
        if (is_player_team)
        {
            mode = movement_mode.follow;
        }
        else
        {
            mode = movement_mode.chase;
        }
        StartCoroutine(RndDuration());
    }

    public override void update_methods()
    {
        if (_game_manager.pause)
        {
            return;
        }
        follow_path();
    }

    public void move_to_location(Vector2 location)
    {
        Vector2 self_flat = transform.position.flat();
        Vector2 self_to_target_dir = location + Vector2.one * 0.5f - self_flat;
        Vector2 current_dir = transform.forward.flat();

        float angle = Vector2.SignedAngle(self_to_target_dir, current_dir);

        float spin_speed = 100;
        if (angle < -5f)
        {
            change_dir(-spin_speed);
        }
        else if (angle > 5f)
        {
            change_dir(spin_speed);
        }
        else
        {
            transform.forward = new Vector3(self_to_target_dir.x, 0, self_to_target_dir.y);
            transform.position += speed * Time.deltaTime * transform.forward;
        }


    }

    public void follow_path()
    {
        is_moving = false;
        if (brake)
        {
            return;
        }

        if (current_path.Count < 1)
        {
            return;
        }

        if (flat_po_grid == current_path.Last().coordinate)
        {
            //refresh
            return;
        }
        if (mode == movement_mode.follow && Vector2.Distance(flat_po, leader.flat_po) < 3)
        {
            return;
        }

        //movement
        if (next_grid_location != flat_po_grid || Vector2.Distance(flat_po, next_grid_location + Vector2.one * 0.5f) > 0.1f)
        {
            move_to_location(next_grid_location);
            is_moving = true;
            return;
        }
        else
        {
            var target = current_path.Find(x => x.coordinate == flat_po_grid);
            int current = current_path.IndexOf(target);
            int next = current + 1;
            next_grid_location = current_path[next].coordinate;
            //Debug.Log("current: " + current_path[current].coordinate + "next: " + current_path[next].coordinate);
        }
    }


    public async Task Stop_Using_Path()
    {
        await Task.Delay(100);
        path_stack.ais_using_path_finding.Remove(this);
    }

    //public IEnumerator Stop_Thinking()
    //{
    //    yield return new WaitForSeconds(1.5f);
    //    if (decision_stack.ai_thinking.Contains(this))
    //    {
    //        decision_stack.ai_thinking.Remove(this);
    //    }
    //}

    public bool start_thinking_dondition()
    {

        return (has_enemy(this,out chase_target)) && !_game_manager.pause;
    }

    public bool need_new_path()
    {
        if (speed < 1)
        {
            return false;
        }
        if (current_path.Count < 1)
        {
            return true;
        }
        return flat_po_grid == current_path.Last().coordinate;
    }
    public bool has_shell_to_shoot()
    {
        return weapon_info.current_num > 0;
    }

    //layer 1
    decision start
    {
        get
        {
            return new(this, "start", start_thinking_dondition(), can_see_target, engage, chase,  delegate ()
            {
               // StartCoroutine(Stop_Thinking());
                //await decision_stack.DemandForPathFinding(this);
            });
        }
    }

    //layer2
    decision engage
    {
        get
        {
            return new(this, "engage", true, has_shell_to_shoot(), shoot, null, delegate ()
            {
                triggered = true;
            });
        }
    }

    decision chase
    {
        get
        {

            return new(this, "chase", need_new_path(), true, null, null, async delegate ()
            {
                await path_stack.DemandForPathFinding(this);
                if (!path_stack.ais_using_path_finding.Contains(this))
                    return;

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                brake = false;
                if (flat_po_grid == null)
                    return;
                cell self_cell = _game_manager.cells[flat_po_grid];
                cell target_cell = new();
                switch (mode)
                {
                    case movement_mode.chase:

                        if (triggered == false)
                        {
                            await Stop_Using_Path();
                            return;
                        }
                        Debug.Log("Try to Chase!");
                        if (chase_target == null)
                        {
                            await Stop_Using_Path();
                            return;
                        }
                        target_cell = _game_manager.cells[chase_target.flat_po_grid];
                        break;
                    case movement_mode.follow:
                        target_cell = _game_manager.cells[leader.flat_po_grid];
                        break;
                }

                bool has_new_path = the_grid.has_path(self_cell, target_cell, _game_manager, out current_path);
                if (has_new_path)
                {
                    next_grid_location = current_path.First().coordinate;
                    current_path.ToList().ForEach(x =>
                    {
                        Vector3 bottom_left = new Vector3(x.coordinate.x, 0, x.coordinate.y);
                    });
                }
                else
                {
                    brake = true;
                }
                watch.Stop();

                Debug.Log($"Execution Time: {watch.ElapsedMilliseconds} ms");

                await Stop_Using_Path();
            });

        }
    }

    //layer3
    decision shoot
    {
        get
        {
            return new(this, "shoot", true, true, null, null, delegate ()
            {
                brake = false;
                if (turret != null)
                    StartCoroutine(Shoot(new Vector2(turret.forward.x, turret.forward.z).normalized));
            });
        }
    }


}


namespace ai
{
    public class decision
    {
        public decision(Isometric_controller_ai _ai, string _name,bool run_or_not, bool run_child_condition = false, decision child1 = null, decision child2 = null, Action function = null)
        {
            name = _name;    
            run_condition = run_or_not;
            condition_child = run_child_condition;
            child_decision_possitive = child1;
            child_decision_negative = child2;
            action = function;
        }

        public string name;
        public bool condition_child;
        public decision child_decision_possitive;
        public decision child_decision_negative;

        public bool run_condition;
        public Action action;
        public void Run()
        {
            if (run_condition == false)
            {
                return;
            }
            if (action != null)
            {
                if (this != null)
                    action.Invoke();
            }
            if (condition_child)
            {
                if (child_decision_possitive == null)
                {
                    return;

                }
                child_decision_possitive.Run();
            }
            else
            {
                if (child_decision_negative == null)
                {

                    return;
                }
                child_decision_negative.Run();
            }
        }


    }



    //public static class decision_stack
    //{
    //    public readonly static int stack_limit = 50;
    //    public static List<Isometric_controller_ai> ai_thinking;
    //    public static List<Isometric_controller_ai> desperate_clients;

    //    public static async Task DemandForPathFinding(Isometric_controller_ai client)
    //    {
    //        if (!desperate_clients.Contains(client))
    //            desperate_clients.Add(client);

    //        await Task.Delay(100);

    //        int empty_slots = stack_limit-ai_thinking.Count;
    //        empty_slots = Math.Max(0, empty_slots);
    //        if (empty_slots > 0)
    //        {
    //            ai_thinking.Add(client);
    //        }
    //        desperate_clients.Remove(client);
    //    }
    //}

    public static class path_stack
    {
        public readonly static int stack_limit = 5;
        public static List<Isometric_controller_ai> ais_using_path_finding;

        public static async Task DemandForPathFinding(Isometric_controller_ai client)
        {
            await Task.Delay(100);
            if (ais_using_path_finding.Count < stack_limit)
            {
                ais_using_path_finding.Add(client);
            }

        }
    }

}
