using stg_grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Isometric_unit : game_manager_client
{
    public int speed;
    public float self_size;
    [HideInInspector]
    public float duration = 0.01f;
    public damagable health_info = new();
    public weaponary weapon_info = new();
    public bool is_player;
    public bool is_player_team;
    
    public Transform turret;

    [HideInInspector]
    public Transform current_target;
    [HideInInspector]
    public Vector2 flat_po
    {
        get
        {
            return new Vector2(transform.position.x, transform.position.z);
        }
    }
    [HideInInspector]
    public Vector2Int flat_po_grid
    {
        get
        {
            return transform.position.flat().world_2_grid();

        }
    }
    public UnityEngine.UI.Text ammo
    {
        get
        {
            return _game_manager.ammo_text;
        }
    }
    [HideInInspector]
    public bool Is_player_team
    {
        get
        {
            return is_player_team;
        }
        set
        {
            Is_player_team = (value || is_player);
        }
    }
    [HideInInspector]
    public Isometric_unit captain;
    [HideInInspector]
    public bool dead;
    [HideInInspector]
    public bool is_moving;
    [HideInInspector]
    public bool is_team_crew
    {
        get
        {
            return captain != null;
        }
    }
    [HideInInspector]
    public string tank_name
    {
        get
        {
            if (is_player)
            {
                return "player";
            }
            else if (is_player_team)
            {
                return "ally";
            }
            else
            {
                return "enemy";
            }
        }
    }
    [HideInInspector]
    public List<Material> self_mats;

    [HideInInspector]
    public bool can_see_target
    {
        get
        {
            if (current_target == null)
                return false;
            Vector2 target_flat = new Vector2(current_target.transform.position.x, current_target.transform.position.z);
            bool can_see_current_target = the_grid.can_see(flat_po, target_flat, cells);
              return can_see_current_target;
        }
    }

    readonly float auto_reload_duration=2;

    [SerializeField]
    float auto_reload_time_left=2;

    private void Start()
    {
        start_calls();
    }
    private void Update()
    {
        update_methods();
    }

    public bool i_can_see(Transform entity)
    {
        Vector2 entity_flat = new Vector2(entity.position.x, entity.position.z);
        bool can_see = the_grid.can_see(flat_po, entity_flat, _game_manager.cells);
        return can_see;
    }

    public override void awake_calls()
    {
        health_info.health = new(gameObject.name, health_info);
        weapon_info.is_player_team = is_player_team;
        weapon_info.Init(tank_name);
        foreach (var mesh in GetComponentsInChildren<MeshRenderer>())
        {
            self_mats.Add(mesh.material);
        }
    }

    public virtual void start_calls()
    {
        if (speed == 0)
        {
            cell standing_cell = _game_manager.cells[flat_po_grid];
            standing_cell.accessible = false;
            _game_manager.cells[flat_po_grid] = standing_cell;
        }
    }

    public virtual void update_methods()
    {
        if (_game_manager.pause)
        {
            return;
        }

    }


    public void change_dir(float increment,float factor=1)
    {
        Vector3 turret_dir = turret.forward;

        float start_angle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
        float end_deg = start_angle + increment * Time.deltaTime*factor;
        Vector2 end_dir = new Vector2(Mathf.Sin(end_deg * Mathf.Deg2Rad), Mathf.Cos(end_deg * Mathf.Deg2Rad));
        transform.forward = new Vector3(end_dir.x, 0, end_dir.y);

        turret.forward = turret_dir;


    }


    public bool has_enemy(Isometric_controller_ai self, out Isometric_unit enemy)
    {
        for (int i = 0; i < _game_manager.entities.childCount; i++)
        {
            Isometric_unit unit = _game_manager.entities.GetChild(i).GetComponent<Isometric_unit>();
            if (unit.is_player_team != is_player_team)
            {
                enemy = unit;
                return true;
            }

        }
        enemy = null;
        return false;
    }

    void try_auto_reload(float t)
    {
        auto_reload_time_left = Mathf.Max(0, auto_reload_time_left - t);
        if (auto_reload_time_left == 0)
        {
            auto_reload_time_left = auto_reload_duration;
            int new_ammo_count = Mathf.Min(weapon_info.current_num + 1, weapon_info.max_num);
            weapon_info.current_num=new_ammo_count;
        }
    }

    public IEnumerator Shoot(Vector2 dir)
    {
        if (weapon_info.shooting || weapon_info.current_num < 1)
        {
            yield break;
        }
        weapon_info.shooting = true;
        weapon_info.current_num--;
        weapon_info.dir = dir.normalized;

        Vector2 flat = new Vector2(transform.position.x + dir.normalized.x / 5f, transform.position.z + dir.normalized.y / 5f);
        _game_manager.create_projectile(weapon_info.projectile_prefab, flat, weapon_info);
        yield return new WaitForSeconds(weapon_info.duration);
        weapon_info.shooting = false;

    }


    public IEnumerator Start_Locking()
    {
        if (!is_player)
        {
            //if (SeekStack.vip.Count < SeekStack.limit)
            //{
            //    StartCoroutine(SeekStack.TryToJoin(this));
            //    yield return new WaitForSeconds(.1f);
            //    if (SeekStack.vip.Contains(this))
            //        LockOnTarget();
            //}
            LockOnTarget();
            float rnd = UnityEngine.Random.Range(0f, 1f);
            yield return new WaitForSeconds(.4f + rnd);
            try_auto_reload(.5f * weapon_info.reload_multiplier);
            //if (SeekStack.vip.Contains(this))
            //{
            //    SeekStack.vip.Remove(this);
            //}
            StartCoroutine(Start_Locking());
        }

        else
        {
            LockOnTarget();
            yield return new WaitForSeconds(.1f);
            try_auto_reload(.1f * weapon_info.reload_multiplier);
            StartCoroutine(Start_Locking());

        }


    }

    public void LockOnTarget()
    {

        if (_game_manager.pause)
        {
            return;
        }

        if (current_target != null)
        {
            Vector2 target_flat = new Vector2(current_target.transform.position.x, current_target.transform.position.z);
            bool can_see_current_target = the_grid.can_see(flat_po, target_flat, cells);
            if (can_see_current_target)
            {
                turret.LookAt(current_target);
                return;
                
            }
        }

        int entities = _game_manager.entities.childCount;
        for (int i = 0; i < entities; i++)
        {
            Transform entity = _game_manager.entities.GetChild(i);
            if (entity == transform)
            {
                continue;
            }
            Isometric_unit unit = entity.GetComponent<Isometric_unit>();
             if (unit.is_player_team == is_player_team)
            {
                continue;
            }
            bool can_see = the_grid.can_see(flat_po, the_grid.flat(entity.position), cells);
            if (can_see)
            {
                current_target = entity;
                turret.LookAt(current_target);
                return;
            }
        }
        turret.transform.forward = transform.forward;
        current_target = null;
            }
}
public static class SeekStack
{
    public static int limit=20;
    public static List<Isometric_unit> vip;

    public static IEnumerator TryToJoin(Isometric_unit client)
    {
        yield return new WaitForSeconds(.1f);
        int remaining_slot =  limit-vip.Count;
        if (remaining_slot>0)
        {
            vip.Add(client);
        }
    }
}