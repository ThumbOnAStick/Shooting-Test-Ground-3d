using damage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ai;
using System;
using tank_ui;
using UnityEngine.UI;

[Serializable]
public class damagable
{
    public int max_health=10;
    public health_info health;
    public health_bar health_ui;

    public void repair_all()
    {
        health.current_hit_point = max_health;
        health_ui.recover_all();
    }

}


[Serializable]
public class weaponary:ICloneable
{
    public GameObject projectile_prefab;
    public GameObject explosion_prefab;
    public int damage;
    public int max_num;
    public float projectile_speed;
    public float reload_multiplier;
    public float duration;
    public bool is_player_team;
    public string damage_giver;

    //current
    [HideInInspector]
    public Vector2 dir;
    [HideInInspector]
    public int current_num;
    [HideInInspector]
    public bool reloading;
    [HideInInspector]
    public bool shooting;
    [HideInInspector]
    public RectTransform weapon_bar;

    public void Init(string _damage_giver)
    {
        current_num = max_num;
        damage_giver = _damage_giver;
    }


    public object Clone()
    {
        return this.MemberwiseClone();
    }
}


namespace damage
{
    public struct health_info
    {
        public string owner;
        public int current_hit_point;
        [HideInInspector]
        public damagable damageable_script;
      

        public health_info(string _owner, damagable _damageable_script)
        {
            owner = _owner;
            damageable_script = _damageable_script; 
            current_hit_point = _damageable_script.max_health;
        }
    }
    public struct health_info_building
    {
        public string owner;
        public int current_hit_point;
        public GameObject instance;

        public health_info_building(string _owner,int max_health,GameObject _instance)
        {
            owner = _owner;
            current_hit_point = max_health;
            instance= _instance;
        }
    }

    public struct damage_info
    {
        public string damage_giver;
        public int damage;
    }

    public static class damagable_methods
    {
        public static void apply_damage(this damagable health_info, damage_info damage, game_manager manager)
        {

            health_info.health.current_hit_point = Math.Max(health_info.health.current_hit_point - damage.damage, 0);
            if (health_info.health.current_hit_point == 0)
            {
                return;
            }
            health_info.health_ui.health_bar_update(health_info, damage.damage, manager);

        }

    }

    public static class damage_animation
    {
        public static void explode(this game_manager manager,Transform target)
        {
            manager.StartCoroutine(explosion(target,target.position,0,manager));
        }

        public static IEnumerator explosion(Transform target,Vector3 object_origin, int index,game_manager manager)
        {
            if(target==null)
                yield break;
            float rnd1 = UnityEngine.Random.value;
            float rnd2 = UnityEngine.Random.value;
            float rnd3 = UnityEngine.Random.value;

            Vector3 rnd_dir = new Vector3(rnd1,rnd2,rnd3).normalized/5;
            target.position = object_origin + rnd_dir;
            yield return new WaitForSeconds(0.2f);
            if (index < 5)
            {
                manager.StartCoroutine(explosion(target,object_origin,index + 1,manager));
            }
        }
    }

}
