using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using stg_grid;
using System.Linq;
using Unity.VisualScripting;
using tank_ui;
using System;
using damage;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using Misc;
using static Level_Info_SO;
using ai;

public class game_manager : MonoBehaviour
{

    public enum LevelType { tutorial, test, formal }
    public LevelType type;
    //public
    [Header("transforms")]
    public Transform walls;
    public Transform obstacles;
    public Transform entities;
    public Transform projectiles;
    public Transform destructibles;
    public Transform misc;
    public RectTransform canvas;
    public RectTransform bottom_left;
    public RectTransform top_right;
    public RectTransform loading_screen;
    public RectTransform pause_menu;
    [Header("prefabs")]
    public GameObject way_block_prefab;
    public GameObject wall_block_prefab;
    public GameObject health_bar_player_prefab;
    public GameObject health_bar_ally_prefab;
    public GameObject health_bar_enemy_prefab;
    public GameObject weapon_bar_prefab;
    public GameObject smoke_prefab;
    public GameObject smoke_prefab1;
    public GameObject explosion_prefab;
    public GameObject brick_break_particle_prefab;
    public GameObject model2;
    public GameObject model3;
    public GameObject supply;
    public GameObject loading_ring;

    [Header("attributes")]
    public int map_size;
    public Vector2 health_bar_offset;
    public Vector2 weapon_bar_offset;

    [ColorUsageAttribute(true, true)]
    public Color high_light_color;
    public LayerMask ground_layer;
    public Material highlight;
    public Text ammo_text;
    public Text lock_text;
    public Text subtitle_text;
    public Text level_name_text;
    public Text kill_count_text;
    public UnityEngine.UI.Image game_condition_sprite;

    //hidden
    [HideInInspector]
    public Isometric_controller_player player;
    [HideInInspector]
    public int w, h;
    [HideInInspector]
    public List<Isometric_controller_ai> think_stack;

    [HideInInspector]
    public Dictionary<Vector2Int, cell> cells = new();
    [HideInInspector]
    public Dictionary<Vector2Int, health_info_building> destructible_health_infos=new();
    [HideInInspector]
    public bool pause;

    //private
    private Vector2 camera_center;
    private GameObject green_way_block;
    private List<GameObject> way_blocks = new();
    private List<Projectile> projectile_list = new();
    private Dictionary<string, Sprite> condition_sprite=new();
    int quintable_kill_count=0;
    int max_enemy_count = 0;
    int _think_stack;
    bool win = false;

    string general_data_path
    {
        get
        {
            return Application.dataPath + "/General/Data";
        }
    }

    General g_data
    {
        get
        {
            string general = File.ReadAllText(general_data_path);
            return JsonUtility.FromJson<General>(general);
        }
    }

    readonly string audio_weapon_firing="Fire";
    readonly string audio_hit_wall_1="Hit_Wall_1";
    readonly string audio_hit_wall_2 = "Hit_Wall_2";
    readonly string audio_explosion = "Explosion";
    readonly string audio_tank_track="Tank_Track";
    readonly string audio_hit_tank = "Hit_Tank";

    AudioManager audio_manager;
    [HideInInspector]
    public List<Isometric_unit> allies
    {
        get
        {
            List<Isometric_unit> result = new();
            for (int i = 0; i < entities.childCount; i++)
            {
                Transform entity = entities.GetChild(i);
                Isometric_unit unit = entity.GetComponent<Isometric_unit>();
                if (!unit.is_player && unit.is_player_team)
                {
                    result.Add(unit);
                }
            }
            return result;
        }
    }
    public List<Isometric_unit> enemies
    {
        get
        {
            List<Isometric_unit> result = new();
            for (int i = 0; i < entities.childCount; i++)
            {
                Transform entity = entities.GetChild(i);
                Isometric_unit unit = entity.GetComponent<Isometric_unit>();
                if (!unit.is_player_team)
                {
                    result.Add(unit);
                }
            }
            return result;
        }
    }
    private void Awake()
    {

        //load level
        switch (type)
        {
            case LevelType.tutorial:
                break;
            case LevelType.test:
                Debug.Log(g_data.test_level);
                if (g_data.test_level != "")
                {
                    string path = Application.dataPath + "/LevelDatas/" + g_data.test_level;
                    bool exists = File.Exists(path);
                    if (exists)
                    {
                        string data1 = File.ReadAllText(path);
                        Level_Info_File test = Level_Editor.CreateFromJSON(data1);
                        load_level_info(test.Packed());
                    }
                }
                break;
            case LevelType.formal:

                int progress = g_data.progress;
                if (!File.Exists(Application.dataPath + "/LevelDatas/level" + progress.ToString()))
                {
                    Level_Info_SO empty = new() { cell_infos = new(), level_name = "", map_size = 10 };
                    load_level_info(empty);

                    break;
                }
                string data = File.ReadAllText(Application.dataPath + "/LevelDatas/level" + progress.ToString());
                if (data != null)
                {
                    Level_Info_File campain = Level_Editor.CreateFromJSON(data);
                    load_level_info(campain.Packed());
                }
                break;
        }

        StartCoroutine(level_init());



    }

    private void Update()
    {

        //check interface
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pause = !pause;
        }
    }

    #region path
    Vector2Int touch_ground(Vector3 origin, Vector3 dir)
    {
        Vector3 pointer = origin;
        while (pointer.y > 0)
        {
            pointer += dir;
        }
        return the_grid.object_2_grid(new Vector2 { x = pointer.x, y = pointer.z });
    }
    private bool has_start, has_end;
    private cell start, end;
    private List<path_node> paths = new();
    #endregion

    #region gameplay
    void load_level_info(Level_Info_SO info)
    {
        map_size = info.map_size;
        List<cell_info> cell_Infos = info.cell_infos;
        level_name_text.text =info.level_name;
        StartCoroutine(level_name_text.text_fade_and_disable(this));
        foreach(cell_info cell_info in cell_Infos)
        {
            Transform target_parent;
            int layer = cell_info.place_data.layer;
            if (layer == 0)
            {
                target_parent = obstacles;
            }
            else if (layer == 1)
            {
                target_parent = destructibles;
            }
            else if (layer == 2)
            {
                target_parent = entities;
            }
            else
            {
                target_parent = misc;
            }
            Instantiate(cell_info.place_data.prefab, new Vector3(cell_info.place_location.x + 0.5f, 0, cell_info.place_location.y + 0.5f), Quaternion.identity, target_parent);
        }
    }
    void get_player()
    {
        player = FindObjectOfType<Isometric_controller_player>();
        camera_follow camera_script = Camera.main.GetComponent<camera_follow>();
        camera_script.player = player;
    }
 
    IEnumerator level_init()
    {
        pause = true;
        //load map
        for (int i = 0; i < map_size + 1; i++)
        {
            for (int j = 0; j < map_size + 1; j++)
            {
                cell new_cell_top_right = new() { coordinate = { x = i, y = j }, accessible = true };
                cell new_cell_top_left = new() { coordinate = { x = -i, y = j }, accessible = true };
                cell new_cell_bottom_left = new() { coordinate = { x = -i, y = -j }, accessible = true };
                cell new_cell_bottom_right = new() { coordinate = { x = i, y = -j }, accessible = true };

                cells.TryAdd(new Vector2Int(i, j), new_cell_top_right);
                cells.TryAdd(new Vector2Int(-i, j), new_cell_top_left);
                cells.TryAdd(new Vector2Int(-i, -j), new_cell_bottom_left);
                cells.TryAdd(new Vector2Int(i, -j), new_cell_bottom_right);
                if (j == map_size || i == map_size)
                {
                    Vector3 center_1 = new Vector3(i + 0.5f, 0f, j + 0.5f);
                    Vector3 center_2 = new Vector3(i + 0.5f, 0f, -j + 0.5f);
                    Vector3 center_3 = new Vector3(-i + 0.5f, 0f, j + 0.5f);
                    Vector3 center_4 = new Vector3(-i + 0.5f, 0f, -j + 0.5f);

                    Vector2Int co_1 = new Vector2Int(i, j);
                    cell obstacle_cell = cells[co_1];
                    obstacle_cell.accessible = false;
                    obstacle_cell.height = 1;
                    cells[co_1] = obstacle_cell;


                    Vector2Int co_2 = new Vector2Int(i, -j);
                    cell obstacle_cell1 = cells[co_2];
                    obstacle_cell1.accessible = false;
                    obstacle_cell1.height = 1;

                    cells[co_2] = obstacle_cell1;

                    Vector2Int co_3 = new Vector2Int(-i, j);
                    cell obstacle_cell2 = cells[co_3];
                    obstacle_cell2.accessible = false;
                    obstacle_cell2.height = 1;
                    cells[co_3] = obstacle_cell2;

                    Vector2Int co_4 = new Vector2Int(-i, -j);
                    cell obstacle_cell3 = cells[co_4];
                    obstacle_cell3.accessible = false;
                    obstacle_cell3.height = 1;
                    cells[co_4] = obstacle_cell3;

                    Instantiate(wall_block_prefab, center_1, Quaternion.identity, walls);
                    Instantiate(wall_block_prefab, center_2, Quaternion.identity, walls);
                    Instantiate(wall_block_prefab, center_3, Quaternion.identity, walls);
                    Instantiate(wall_block_prefab, center_4, Quaternion.identity, walls);

                }
            }

        }

        for (int i = 0; i < obstacles.childCount; i++)
        {
            Transform obstacle = obstacles.GetChild(i);
            Vector2 obstacle_in_isometric = new Vector2() { x = obstacle.position.x, y = obstacle.position.z };
            Vector2Int b_2_g = the_grid.block_2_grid(obstacle_in_isometric);
            if (!cells.ContainsKey(b_2_g))
            {
                continue;
            }
            cell obstacle_cell = cells[b_2_g];
            obstacle_cell.accessible = false;
            obstacle_cell.height = 1;
            cells[b_2_g] = obstacle_cell;

        }

        for (int i = 0; i < destructibles.childCount; i++)
        {
            Transform destructible = destructibles.GetChild(i);
            Vector2 destructible_in_isometric = new Vector2() { x = destructible.position.x, y = destructible.position.z };
            Vector2Int b_2_g = the_grid.block_2_grid(destructible_in_isometric);
            if (!cells.ContainsKey(b_2_g))
            {
                continue;
            }
            cell obstacle_cell = cells[b_2_g];
            obstacle_cell.accessible = false;
            obstacle_cell.height = 1;
            cells[b_2_g] = obstacle_cell;
            health_info_building info = new(b_2_g.ToString(), 2, destructible.gameObject);
            destructible_health_infos.Add(b_2_g, info);
        }

        get_player();
        get_width_height();
        bars_init();
        audio_manager = FindObjectOfType<AudioManager>();
        SetAmbience(audio_tank_track);
        max_enemy_count = enemies.Count;
        //decision_stack.ai_thinking = new();
        //decision_stack.desperate_clients = new();
        path_stack.ais_using_path_finding = new();
        
        SeekStack.vip = new();
        yield return new WaitForSeconds(1f);
        pause = false;

        //start updating
        StartCoroutine(update_entities());
    }

    IEnumerator update_entities()
    {
        pause_menu.gameObject.SetActive(pause);
        if (pause)
        {
            yield return new WaitForSeconds(0.01f);
            StartCoroutine(update_entities());
            yield break;
        }
        //check units
        int moving_units=0;
        for (int i = 0; i < entities.childCount; i++)
        {
            Transform entity = entities.GetChild(i);
            Isometric_unit unit = entity.GetComponent<Isometric_unit>();

            //bar follow 
            if (unit.health_info.health_ui.health_bar_instance == null)
            {
                continue;
            }
            bar_follow_controller(unit.health_info.health_ui.health_bar_instance.transform, entity, health_bar_offset, true);
            bar_follow_controller(unit.weapon_info.weapon_bar, entity, weapon_bar_offset, false);
            weaponary unit_weapon = unit.weapon_info;
            if (unit_weapon.max_num != 0)
            {
                float bullet_raito = (float)unit_weapon.current_num / unit_weapon.max_num;
                float final_width = 50 * bullet_raito;
                unit_weapon.weapon_bar.sizeDelta = new Vector2(final_width, unit_weapon.weapon_bar.sizeDelta.y);
            }

            //ambience
            if (unit.is_moving)
            {
                moving_units++;
            }
        }

        if (moving_units > 0)
        {
            audio_manager.ContinueAmbience();
        }
        else
        {
            audio_manager.PauseAmbience();
        }

        //check projectile
        List<Projectile> to_destroy_list = new();
        for (int i = 0; i < projectile_list.Count; i++)
        {
            Vector3 projectile_po = projectile_list[i].projectile_instance.transform.position;
            Vector2 flat_position = new Vector2(projectile_po.x, projectile_po.z);
            Vector2Int o2g = the_grid.object_2_grid(flat_position);

            //check cell
            if (cells.ContainsKey(o2g) && cells[o2g].accessible == false && cells[o2g].height != 0)
            {
                bool is_breakable_wall=false;
                if (destructible_health_infos.ContainsKey(o2g))
                {
                    is_breakable_wall = true;
                    damage_wall(o2g);
                }

                to_destroy_list.Add(projectile_list[i]);

                //audio
                if (is_breakable_wall)
                {
                    play_sound_effects(audio_hit_wall_1);
                }
                else
                {
                    play_sound_effects(audio_hit_wall_2);
                }
                continue;
            }

            //check entity
            for (int j = 0; j < entities.childCount; j++)
            {
                Transform entity = entities.GetChild(j);
                Isometric_unit unit = entity.GetComponent<Isometric_unit>();
                Vector2 entity_flat = new Vector2(entity.position.x, entity.position.z);
                if (Vector2.Distance(entity_flat, flat_position) < .75f && projectile_list[i].weaponary.is_player_team != unit.is_player_team)
                {
                    apply_damage_to_unit(unit, projectile_list[i].weaponary);
                    to_destroy_list.Add(projectile_list[i]);
                    continue;
                }
            }


            projectile_list[i].projectile_instance.transform.position +=
                new Vector3(projectile_list[i].weaponary.dir.x, 0, projectile_list[i].weaponary.dir.y) * projectile_list[i].weaponary.projectile_speed;
        }
        to_destroy_list.ForEach(x => destroy_projectile(x));

        kill_count_text.text = (max_enemy_count-enemies.Count).ToString() + "/" + max_enemy_count.ToString();

        //next loop
        yield return new WaitForSeconds(0.01f);
        StartCoroutine(update_entities());

    }

    IEnumerator reload_scene()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion

    #region ui
    IEnumerator LoadNewScene()
    {

        loading_screen.gameObject.SetActive(true);
        // This line waits for 3 seconds before executing the next line in the coroutine.
        // This line is only necessary for this demo. The scenes are so simple that they load too fast to read the "Loading..." text.
        yield return new WaitForSeconds(1f);

        // Start an asynchronous operation to load the scene that was passed to the LoadNewScene coroutine.
        AsyncOperation async = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

        // While the asynchronous operation to load the new scene is not yet complete, continue waiting until it's done.
        while (!async.isDone)
        {
            yield return null;
        }
        loading_screen.gameObject.SetActive(false);
    }

    public void load_game_condition_image(string condition_name)
    {
        string path = "Sprites/GameConditions/" + condition_name;
        Sprite target_image = Resources.Load<Sprite>(path);
        if (target_image == null)
        {
            Debug.LogError("condition sprite non exist");
            return;
        }
        game_condition_sprite.sprite = target_image;
        game_condition_sprite.enabled = true;
        Color color = game_condition_sprite.color;
        color.a = 1;
        game_condition_sprite.color = color;
        StartCoroutine(game_condition_sprite.image_fade_and_disable(this));
    }
  
    public Image init_load_ring(Transform world_transform)
    {
        Vector3 ring_screen = Camera.main.WorldToScreenPoint(world_transform.position) + Vector3.up * 50f;
        GameObject ring = Instantiate(loading_ring, ring_screen, Quaternion.identity, canvas);
        Image image = ring.GetComponent<Image>();
        image.start_round(1, this);
        return image;
    }
    public void ring_follow_transform(Image ring, Transform world_transform)
    {
        Vector3 ring_screen = Camera.main.WorldToScreenPoint(world_transform.position) + Vector3.up * 50f;
        ring.GetComponent<RectTransform>().position = ring_screen;
    }

    public void bars_init()
    {
        for (int i = 0; i < entities.childCount; i++)
        {
            Transform entity = entities.GetChild(i);
            Isometric_unit entity_script = entity.GetComponent<Isometric_unit>();
            damagable damagable = entity_script.health_info;
            GameObject target_prefab;
            if (entity_script.is_player)
            {
                target_prefab = health_bar_player_prefab;
            }
            else if (entity_script.Is_player_team)
            {
                target_prefab = health_bar_ally_prefab;
            }
            else
            {
                target_prefab = health_bar_enemy_prefab;
            }

            if (damagable.health_ui.health_bar_instance == null)
            {
                GameObject health_bar_instance = Instantiate(target_prefab, canvas);
                health_bar new_health_bar = new(health_bar_instance, entity);
                damagable.health_ui = new_health_bar;
            }
            
            if (entity_script.weapon_info.weapon_bar == null)
            {
                GameObject weapon_bar = Instantiate(weapon_bar_prefab,canvas);
                RectTransform target_bar =weapon_bar.GetComponent<RectTransform>();
                entity_script.weapon_info.weapon_bar =target_bar;
            }


        }
    }
    public void get_width_height()
    {
        w = Mathf.FloorToInt(Camera.main.scaledPixelWidth * 0.95f);
        h = Mathf.FloorToInt(Camera.main.scaledPixelHeight * 0.95f);
        camera_center = new Vector2(w / 2, h / 2);
        Debug.Log("width: " + w + " height: " + h);
    }
    float min_abs(float f1, float f2)
    {
        float abs1 = Mathf.Abs(f1);
        float abs2 = Mathf.Abs(f2);
        if (Mathf.Min(abs1, abs2) == abs1)
        {
            return f1;
        }
        return f2;
    }
    public void bar_follow_controller(Transform bar, Transform unit,Vector2 offset,bool show_outside_of_screen)
    {

        Vector3 player_po = unit.transform.position;
        Vector3 player_screen_po = Camera.main.WorldToScreenPoint(player_po);
        bool within_width = player_screen_po.x < w && player_screen_po.x > 0;
        bool within_height = player_screen_po.y < h && player_screen_po.y > 0;
        bool within_rect = within_height && within_width;
        if (within_rect)
        {
            bar.gameObject.SetActive(true);
            bar.GetComponent<RectTransform>().position = player_screen_po + (Vector3)offset;
        }
        else if(show_outside_of_screen)
        {
            Vector2 center_to_point = player_screen_po - (Vector3)camera_center;
            float width_ratio = Mathf.Abs(camera_center.x / center_to_point.x);
            Vector2 imaginary_width_height = center_to_point * width_ratio;
            float final_y = min_abs(imaginary_width_height.y, camera_center.y);
            float height_ratio = Mathf.Abs(final_y / imaginary_width_height.y);
            Vector2 final_width_height = imaginary_width_height * height_ratio * 0.9f + camera_center;
            bar.GetComponent<RectTransform>().position = final_width_height;
        }
        else
        {
            bar.gameObject.SetActive(false);
        }

    }

    #endregion

    #region audio

    public void play_condition_audio(string condition_name)
    {
        string path = "Audios/GameConditions/" + condition_name;
        AudioClip target_audio = Resources.Load<AudioClip>(path);
        if (target_audio == null)
        {
            Debug.LogError("condition audio non exist");
            return;
        }
        StartCoroutine(audio_manager.play(target_audio));
    }
    public void play_sound_effects(string sound_name,float duration=0)
    {
        string path = "Audios/SoundEffect/" + sound_name;
        AudioClip target_audio = Resources.Load<AudioClip>(path);
        if (target_audio == null)
        {
            Debug.LogError(" sound effect non exist");
            return;
        }
        StartCoroutine(audio_manager.play(target_audio,duration));
    }

    public void SetAmbience(string sound_name)
    {
        string path = "Audios/Ambience/" + sound_name;
        AudioClip target_audio = Resources.Load<AudioClip>(path);
        if (target_audio == null)
        {
            Debug.LogError(" ambience non exist");
            return;
        }
        audio_manager.ambience_clip =target_audio;
    }

    #endregion

    #region combat_system

    public void apply_damage_to_unit(Isometric_unit defender,weaponary attacker)
    {
        if (defender.dead)
            return;
        damage_info info = new() { damage = attacker.damage };
        defender.health_info.apply_damage(info, this);
        play_sound_effects(audio_hit_tank);
        StartCoroutine(damage_high_light(defender));
        if (defender.health_info.health.current_hit_point < 1)
        {
            defender.dead = true;
            
            Kill(attacker.damage_giver,defender);
        }
    }
    public void apply_damage_to_unit(Isometric_unit defender, int damage)
    {
        if (defender.dead)
            return;
        damage_info info = new() { damage = damage };
        defender.health_info.apply_damage(info, this);
        StartCoroutine(damage_high_light(defender));
        if (defender.health_info.health.current_hit_point < 1)
        {
            defender.dead = true;

            Kill("", defender);
        }
    }

    public void destroy_projectile(Projectile target)
    {
        if (projectile_list.Contains(target))
        {
            projectile_list.Remove(target);
        }
        weaponary weapon = target.weaponary;
        GameObject spark = Instantiate(weapon.explosion_prefab, target.projectile_instance.transform.position, Quaternion.identity);
        spark.transform.forward = -new Vector3(weapon.dir.x, 0, weapon.dir.y);
        Destroy(spark, 1f);
        if (target.projectile_instance)
            Destroy(target.projectile_instance);
    }
    public void create_projectile(GameObject prefab, Vector2 point, weaponary target_weapon)
    {
        GameObject instance = Instantiate(prefab, new Vector3(point.x, 0, point.y), Quaternion.identity, projectiles);
        instance.transform.forward = new Vector3(target_weapon.dir.x, 0, target_weapon.dir.y);
        Projectile new_p = new() { weaponary = (weaponary)target_weapon.Clone(), projectile_instance = instance };
        projectile_list.Add(new_p);
        //audio
        play_sound_effects(audio_weapon_firing);
    }
    public void Kill(string killer, Isometric_unit target)
    {
        StartCoroutine(check_victory());
        if (!target.is_player_team)
        {
     
            quintable_kill_count++;
            if (quintable_kill_count == 5)
            {
                quintable_kill_count = 0;
                kill_award();
            }
        }
        if (target.is_player)
        {
            con_defeat.run(this);
        }
        this.explode(target.transform);

        //audio
        play_sound_effects(audio_explosion,1f);

        GameObject explosion = Instantiate(explosion_prefab, target.transform.position, Quaternion.identity);
        if (target.speed < 1)
        {

          cell empty=  cells[target.flat_po_grid];
            empty.accessible = true;
            cells[target.flat_po_grid] = empty;
        }
        Destroy(explosion, 3f);
        Destroy(target.health_info.health_ui.health_bar_instance);
        Destroy(target.weapon_info.weapon_bar.gameObject);
        Destroy(target.gameObject, 1f);
    }
    void damage_wall(Vector2Int wall_cell)
    {
        health_info_building info = destructible_health_infos[wall_cell];
        info.current_hit_point -= 1;

        if (info.current_hit_point < 1)
        {
            destroy_wall(wall_cell, info);
            destructible_health_infos[wall_cell] = info;
            return;
        }
        GameObject new_instance = Instantiate(model2, info.instance.transform.position, Quaternion.identity, destructibles);
        GameObject smoke = Instantiate(smoke_prefab1, info.instance.transform.position, Quaternion.identity);
        GameObject wall_pieces = Instantiate(brick_break_particle_prefab, info.instance.transform.position, Quaternion.identity);

        Destroy(wall_pieces, 5f);
        Destroy(smoke, 5f);
        Destroy(info.instance);
        info.instance = new_instance;
        destructible_health_infos[wall_cell] = info;


    }
    void destroy_wall(Vector2Int coordinate, health_info_building info)
    {
        cell new_cell = cells[coordinate];
        new_cell.accessible = true;
        new_cell.height = 0;
        cells[coordinate] = new_cell;
        GameObject new_instance = Instantiate(model3, info.instance.transform.position, Quaternion.identity, destructibles);
        GameObject smoke = Instantiate(smoke_prefab1, info.instance.transform.position, Quaternion.identity);
        GameObject wall_pieces = Instantiate(brick_break_particle_prefab, info.instance.transform.position, Quaternion.identity);

        Destroy(wall_pieces, 5f);
        Destroy(smoke, 5f);
        Destroy(info.instance);
        info.instance = new_instance;
    }

    public IEnumerator damage_high_light(Isometric_unit defender)
    {
        List<MeshRenderer> meshes = defender.GetComponentsInChildren<MeshRenderer>().ToList();
        foreach (Renderer mesh in meshes)
        {
            if (mesh.material == null)
            {
                yield break;
            }
            mesh.material = new(highlight);
        }

        yield return new WaitForSeconds(0.5f);
        if (defender == null)
            yield break;

        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i] != null)
            {
                meshes[i].material = defender.self_mats[i];
            }

        }
    }
    public IEnumerator check_victory()
    {
        if (win == true)
            yield break;
        yield return new WaitForSeconds(1f);
        if (enemies.Count < 1)
        {
            win = true;
            cond_win.run(this);
            yield return new WaitForSeconds(2f);
            if (type == LevelType.formal)
            {

                int progress = g_data.progress;
                if (!File.Exists(Application.dataPath + "/LevelDatas/level" + (progress + 1).ToString()))
                {

                    SceneManager.LoadScene(4);
                    yield break;
                }
                General data = new() { progress = progress + 1, test_level = g_data.test_level };
                data.Save();
                SceneManager.LoadScene(3);

            }
        }

    }
    #endregion

    #region test
    public void goto_editor()
    {
        SceneManager.LoadScene(1);
    }
    public void GotoMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void Unfreeze()
    {
        pause = false;
    }
    public void Quit()
    {
        Application.Quit();
    }
    #endregion

    #region player
    public void repair_player()
    {
        cond_repair.run(this);
    }
    public void kill_award()
    {
        cond_quintuple.run(this);

    }
    public void reinforce()
    {
        cond_reinforce.run(this);
        bars_init();
    }

    #endregion

    //game conditions:
    condition con_defeat
    {
        get
        {
            return new()
            {
                name = "Defeat",
                action = delegate
            {
                StartCoroutine(reload_scene());
            }
            };
        }
    }

    condition cond_win
    {
        get
        {
            return new()
            {
                name = "Victory",
                action = delegate
                {
                    
                }
            };
        }
    }

    condition cond_repair
    {
        get
        {
            return new()
            {
                name = "Repair",
                action = delegate
                {
                    player.health_info.repair_all();
                }
            };
        }
    }

    condition cond_quintuple
    {
        get
        {
            return new()
            {
                name = "Quintuple",
                action = delegate
                {
                    Vector2 rnd_near = player.flat_po.random_center_near(3, cells);
                    Vector3 real_position = new(rnd_near.x, 0, rnd_near.y);
                   GameObject dropping_supply= Instantiate(supply, real_position, Quaternion.identity);
                    Dropping_Supply script = dropping_supply.GetComponent<Dropping_Supply>();
                    script.Init();
                }
            };
        }
    }

    condition cond_reinforce
    {
        get
        {
            return new()
            {
                name = "Reinforce",
                action = delegate
                {

                }

            };
        }
    }
}


namespace stg_grid
{
    public struct cell
    {
        public Vector2Int coordinate;
        public bool accessible;
        public int height;
    }

    public struct path_node
    {
        public cell self_cell;
        public Vector2Int coordinate
        {
            get
            {
                return self_cell.coordinate;
            }
        }
        public Vector2Int parent_node_co;
    }

    public static class the_grid
    {
        public static Vector2Int block_2_grid(Vector2 input)
        {
            return new Vector2Int() { x = Mathf.RoundToInt(input.x - 0.5f), y = Mathf.RoundToInt(input.y - 0.5f) };
        }
        public static Vector2Int object_2_grid(Vector2 input)
        {
            return new Vector2Int() { x = Mathf.FloorToInt(input.x), y = Mathf.FloorToInt(input.y) };
        }
        public static Vector2Int world_2_grid(this Vector2 input)
        {
            return new Vector2Int() { x = Mathf.FloorToInt(input.x), y = Mathf.FloorToInt(input.y) };
        }
        public static Vector2 flat(this Vector3 input)
        {
            return new Vector2(input.x, input.z);
        }
        public static Vector2Int flat_int(this Vector3 input)
        {
            return new Vector2Int(Mathf.FloorToInt(input.x), Mathf.FloorToInt(input.y));

        }
        public static Vector2Int random_point_near(this Vector2Int self, int max_dis, Dictionary<Vector2Int, cell> cells)
        {
            Vector2Int near = new();
            int step = 0;
            while (step < 99)
            {
                near = new() { x = UnityEngine.Random.Range(0, max_dis)+self.x, y = UnityEngine.Random.Range(0, max_dis)+self.y };
                float distance = Vector2Int.Distance(near, self);
                step++;
                if (distance > max_dis)
                {
                    continue;
                }
                if (!cells.ContainsKey(near) || cells[near].accessible == false)
                {
                    continue;
                }
                else
                {
                    break;
                }


            }
            return near;
        }
        public static Vector2 random_center_near(this Vector2 self, int max_dis, Dictionary<Vector2Int, cell> cells)
        {
            Vector2Int self_cell = self.world_2_grid();
            Vector2Int near_cell = self_cell.random_point_near(max_dis, cells);
            return (Vector2)near_cell + Vector2.one * 0.5f;
        }

        public static bool point_within_entity(Vector2 point, Vector2 bottom_left, Vector2 top_right)
        {
            return point.x >= bottom_left.x && point.y >= bottom_left.y && point.x <= top_right.x && point.y <= top_right.y;
        }

        public static bool entity_within_entity(Vector3 self_position, float self_width, Vector3 entity_position, float entity_width)
        {
            Vector2 entity_flat = new Vector2(entity_position.x, entity_position.z);
            Vector2 bottom_left = entity_flat - Vector2.one * entity_width / 2;
            Vector2 top_right = entity_flat + Vector2.one * entity_width / 2;

            Vector2 self_flat = new Vector2(self_position.x, self_position.z);
            Vector2 self_bottom_left = self_flat - Vector2.one * self_width / 2;
            Vector2 self_bottom_right = self_flat + new Vector2(self_width / 2, -self_width / 2);
            Vector2 self_top_left = self_flat + new Vector2(-self_width / 2, self_width / 2);
            Vector2 self_top_right = self_flat + Vector2.one * self_width / 2;

            return point_within_entity(self_bottom_left, bottom_left, top_right) || point_within_entity(self_bottom_right, bottom_left, top_right)
                || point_within_entity(self_top_left, bottom_left, top_right) || point_within_entity(self_top_right, bottom_left, top_right);
        }

        public static Vector3 blocked_velocity(Vector2 object_position, Vector2 object_velocity, Dictionary<Vector2Int, cell> cells, float self_size)
        {
            float half_size = self_size / 2;
            Vector2 dir_horizontal = object_velocity.normalized * half_size;
            Vector2 dir_vertical = new Vector2(dir_horizontal.y, -dir_horizontal.x);
            Vector2 next_position = object_position + object_velocity * 0.1f;
            List<Vector2Int> next_grids = new();
            next_grids.Add(object_2_grid(next_position - dir_horizontal - dir_vertical));
            next_grids.Add(object_2_grid(next_position + dir_horizontal - dir_vertical));
            next_grids.Add(object_2_grid(next_position - dir_horizontal + dir_vertical));
            next_grids.Add(object_2_grid(next_position + dir_horizontal + dir_vertical));

            bool blocked = next_grids.Any(x => cells.ContainsKey(x) && cells[x].accessible == false);
            if (!blocked)
            {
                return Vector3.zero;
            }
            return -new Vector3(object_velocity.x, 0, object_velocity.y);
        }

        #region path finding
        //path finding
        public static bool has_path(cell start, cell end, game_manager manager, out List<path_node> path)
        {
            path = new List<path_node>();
            path = expand_path(path, start, end, manager);
            bool result = path.Count > 0 && path.Last().coordinate == end.coordinate;
            if (result == false)
                path = new();
            return result;
        }


        static List<path_node> expand_path(List<path_node> path, cell start, cell end, game_manager manager)
        {
            int step = 0;
            path_node start_node = new() { self_cell = start };
            path.Add(start_node);
            //Debug.Log("start coordinate: " + start.coordinate + " end coordinate: " + end.coordinate);
            while (step < 100)
            {
                path_node target_node = closest_node_from_list(path, start, end, manager);
                path_node closest_neighbor_from_node = closest_neighbor_from_cell(target_node, end, manager, path);

                //Debug.Log("closest neighbor: " + closest_neighbor_from_node.coordinate);

                if (!path.Any(x => x.coordinate == closest_neighbor_from_node.coordinate))
                    path.Add(closest_neighbor_from_node);

                if (closest_neighbor_from_node.self_cell.coordinate == end.coordinate)
                {
                    path_node pointer = closest_neighbor_from_node;
                    List<path_node> final_path = new() { closest_neighbor_from_node };
                    while (pointer.coordinate != start.coordinate)
                    {
                        pointer = path.First(x => x.coordinate == pointer.parent_node_co);
                        final_path.Insert(0, pointer);
                    }
                    return final_path;
                }
                step++;
            }
            return path;
        }

        static float neighbor_cell_cost(cell cell1, cell cell2, cell end_cell)
        {
            Vector2 dir = cell2.coordinate - cell1.coordinate;
            Vector2 cell_to_end = end_cell.coordinate - cell2.coordinate;
            float cell_to_end_cost = MathF.Abs(cell_to_end.x) + Mathf.Abs(cell_to_end.y);
            //Debug.Log("self: " + cell2.coordinate + " end: " + end_cell.coordinate + " vector: " + cell_to_end + " result: " + cell_to_end_cost);

            if (dir.x != 0 && dir.y != 0)
            {
                return (1.5f + cell_to_end_cost);
            }
            return (1f + cell_to_end_cost);
        }

        static int cell_two_sides_cost(cell cell, cell start, cell end)
        {
            //int result = Mathf.Abs(cell.coordinate.x - end.coordinate.x) + Mathf.Abs(cell.coordinate.y - end.coordinate.y);
            int result = Mathf.RoundToInt(Vector2.Distance(cell.coordinate, end.coordinate));
            return result;
        }

        static bool is_avialable_cell(this Vector2Int coordinate, game_manager manager)
        {
            return manager.cells.ContainsKey(coordinate) && manager.cells[coordinate].accessible == true;
        }

        static List<cell> neighbors(cell self, game_manager manager)
        {
            List<cell> result = new();
            Dictionary<Vector2Int, cell> cells = manager.cells;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if ((i == 0 && j == 0))
                    {
                        continue;
                    }
                    Vector2Int neighbor_coordinate = new Vector2Int(i + self.coordinate.x, j + self.coordinate.y);
                    if (!cells.ContainsKey(neighbor_coordinate) || cells[neighbor_coordinate].accessible == false)
                    {
                        continue;
                    }
                    result.Add(cells[neighbor_coordinate]);
                }
            }
            return result;
        }
        static List<cell> neighbors(cell self, game_manager manager, List<path_node> exception)
        {
            List<cell> result = new();
            Dictionary<Vector2Int, cell> cells = manager.cells;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if ((i == 0 && j == 0))
                    {
                        continue;
                    }

                    Vector2Int neighbor_coordinate = new Vector2Int(i + self.coordinate.x, j + self.coordinate.y);

                    if (i != 0 && j != 0)
                    {
                        Vector2Int neighbor_coordinate1 = new Vector2Int(i + self.coordinate.x, self.coordinate.y);
                        Vector2Int neighbor_coordinate2 = new Vector2Int(self.coordinate.x, j + self.coordinate.y);
                        if (!neighbor_coordinate1.is_avialable_cell(manager) || !neighbor_coordinate2.is_avialable_cell(manager))
                        {
                            continue;
                        }
                    }
                    if (!neighbor_coordinate.is_avialable_cell(manager))
                    {
                        continue;
                    }
                    if (exception.Any(x => x.coordinate == neighbor_coordinate))
                    {
                        continue;
                    }
                    result.Add(cells[neighbor_coordinate]);
                }
            }
            return result;
        }
        static List<cell> neighbors(cell self, game_manager manager, List<cell> exception)
        {
            List<cell> result = new();
            Dictionary<Vector2Int, cell> cells = manager.cells;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if ((i == 0 && j == 0))
                    {
                        continue;
                    }
                    Vector2Int neighbor_coordinate = new Vector2Int(i + self.coordinate.x, j + self.coordinate.y);
                    if (!cells.ContainsKey(neighbor_coordinate) || cells[neighbor_coordinate].accessible == false)
                    {
                        continue;
                    }
                    if (exception.Any(x => x.coordinate == neighbor_coordinate))
                    {
                        continue;
                    }
                    result.Add(cells[neighbor_coordinate]);
                }
            }
            return result;
        }
        static List<path_node> fresh_nodes(List<path_node> nodes, game_manager manager)
        {
            List<path_node> fresh_nodes = new();
            nodes.ForEach(x =>
            {
                cell self_cell = x.self_cell;
                List<cell> neighbor_cells = neighbors(self_cell, manager, nodes);
                if (neighbor_cells.Count > 0)
                {
                    fresh_nodes.Add(x);
                }
            });
            return fresh_nodes;
        }

        static path_node closest_neighbor_from_cell(path_node node, cell end, game_manager manager, List<path_node> old_nodes)
        {
            cell self_cell = node.self_cell;
            List<cell> neighbor_cells = neighbors(node.self_cell, manager, old_nodes);
            if (neighbor_cells.Count < 1)
            {
                return node;
            }
            path_node neighbor_node = new();
            neighbor_node.self_cell = neighbor_cells.OrderBy(x => neighbor_cell_cost(node.self_cell, x, end)).First();
            neighbor_node.parent_node_co = node.coordinate;
            return neighbor_node;
        }

        static path_node closest_node_from_list(List<path_node> path_nodes, cell start, cell end, game_manager manager)
        {
            List<path_node> fresh = fresh_nodes(path_nodes, manager);
            if (fresh.Count < 1)
            {
                return path_nodes.First();
            }
            return fresh.OrderBy(x => cell_two_sides_cost(x.self_cell, start, end)).First();
        }
        #endregion

        public static float max_abs(float f1, float f2)
        {
            if (Mathf.Abs(f1) > Mathf.Abs(f2))
            {
                return f1;
            }
            return f2;
        }
        public static float abs_max_abs(float f1, float f2)
        {
            float a1 = Mathf.Abs(f1);
            float a2 = Mathf.Abs(f2);
            return MathF.Max(a1, a2);
        }
        public static bool can_see(Vector2 self_point, Vector2 target_point, Dictionary<Vector2Int, cell> cells)
        {
            Vector2 dir_total = target_point - self_point;
            float max = abs_max_abs(dir_total.x, dir_total.y);
            Vector2 increment = (dir_total / max);
            Vector2 pointer = self_point;
            Vector2Int target_gird = target_point.world_2_grid();
            int step = 0;
            while (pointer.world_2_grid() != target_gird && step < 20)
            {
                step++;
                pointer += increment;
                Vector2Int next_grid = pointer.world_2_grid();

                if (!cells.ContainsKey(next_grid) || cells[next_grid].height > 0)
                {
                    return false;
                }
                if (step == 19)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool point_can_see(this Vector2 self_point, Vector2 target_point, Dictionary<Vector2Int, cell> cells)
        {
            Vector2 dir_total = target_point - self_point;
            float max = abs_max_abs(dir_total.x, dir_total.y);
            Vector2 increment = dir_total / max;
            Vector2 pointer = self_point;
            Vector2Int target_gird = target_point.world_2_grid();
            int step = 0;
            while (pointer.world_2_grid() != target_gird && step < 100)
            {
                step++;
                pointer += increment;
                Vector2Int next_grid = pointer.world_2_grid();

                if (!cells.ContainsKey(next_grid) || cells[next_grid].height > 0)
                {
                    return false;
                }
                if (step == 99)
                {
                    Debug.LogError("Error! Step overflow");
                }
            }
            return true;
        }
        public static List<Vector2Int> draw_line(this Vector2 self_point, Vector2 target_point, Dictionary<Vector2Int, cell> cells)
        {
            Vector2 dir_total = target_point - self_point;
            float max = abs_max_abs(dir_total.x, dir_total.y);
            Vector2 increment = dir_total / max;
            Vector2 pointer = self_point;
            Vector2Int target_gird = target_point.world_2_grid();
            int step = 0;
            List<Vector2Int> result=new();
            while (pointer.world_2_grid() != target_gird && step < 100)
            {
                step++;
                pointer += increment;
                Vector2Int next_grid = pointer.world_2_grid();

                if (!cells.ContainsKey(next_grid) || !cells[next_grid].accessible)
                {
                    continue;
                }
                if (step == 99)
                {
                    Debug.LogError("Error! Step overflow");
                }

                result.Add(next_grid);
            }
            return result;
        }
        public static Vector2Int safe_point(this Vector2 self_point, Vector2 target_point, Dictionary<Vector2Int, cell> cells, int max_dis)
        {
            Vector2 dir_total = self_point - target_point;
            float max = abs_max_abs(dir_total.x, dir_total.y);
            Vector2Int result = new();
            for (int i = -max_dis; i < max_dis; i++)
            {
                for (int j = -max_dis; j < max_dis; j++)
                {
                    Vector2Int new_point = new(i, j);
                    float distance = Vector2.Distance(target_point, new_point);
                    if (!cells.ContainsKey(new_point) || cells[new_point].accessible == false)
                    {
                        continue;
                    }
                    if (distance < dir_total.magnitude)
                    {
                        continue;
                    }
                    if (!target_point.point_can_see(target_point, cells))
                    {
                        return result;
                    }
                }
            }
            return result;
        }
    }

}

namespace Isometric
{
    public static class Isometric_Methods
    {
        public static Vector2 isometric_2_world(Vector2 input)
        {
            Vector2 result;
            float x_in = input.x;
            float y_in = input.y;
            Vector2 x_dir = new Vector2 { x = x_in * 0.7f, y = -x_in * 0.7f };
            Vector2 y_dir = new Vector2 { x = y_in * 0.7f, y = y_in * 0.7f };
            result = x_dir + y_dir;
            return result;
        }
    }
}

namespace Misc
{
    public class condition
    {
        public string name;
        public Action action;


        public void run(game_manager manager)
        {
            manager.load_game_condition_image(name);
            manager.play_condition_audio(name);
            action.Invoke();
        }
    }
}