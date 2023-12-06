using stg_grid;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using static Level_Info_SO;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;

public class Level_Editor : MonoBehaviour
{
    public int size;
    string level_id;

    int recorded_size;
    [HideInInspector]
    public List<GameObject> grid_pens;
    public Dictionary<Vector2Int, GameObject> place_info_pens = new();

    public Level_Info_SO level_Info_instance;

    public List<Place_Data_SO> walls;
    public List<Place_Data_SO> players;
    public List<Place_Data_SO> enemies;

    public GameObject confirm_button;
    public GameObject confirm_button1;

    public GameObject pen_prefab;
    public GameObject blank_button_prefab;
    public Material default_line;
    public InputField level_size;
    public InputField level_ID_input;
    public InputField level_name_input;
    public RectTransform panel;

    public enum selected_panel { walls, player, enemy };
    public selected_panel selcted_;

    Dictionary<Vector2Int, cell> cells = new();
    public class brush
    {
        public LineRenderer l_r;
        public Place_Data_SO data;
        public bool erase=false;
    }
    brush mouse_drawer = new();
    List<Button> buttons = new();
    List<Vector2Int> mouse_past = new();
    bool mouse_drawing;
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

    private void Awake()
    {

        //init
        string test_level_name = g_data.test_level;
        if (test_level_name != null)
            level_ID_input.text = level_id = test_level_name;

        level_Info_instance = ScriptableObject.CreateInstance<Level_Info_SO>();
        ReadLevel.ReadLevelByName(level_id, out level_Info_instance);

        drawing_init();
        mouse_drawer.l_r = Instantiate(pen_prefab).GetComponent<LineRenderer>();
        mouse_drawer.data = walls[0];
        toggle_walls();
    }
    private void Update()
    {


        //size
        char[] array = level_size.text.ToArray();
        int result = 0;

        for (int i = 0; i < array.Length; i++)
        {
            int num = array[i] - '0';
            for (int j = 1; j < array.Length - i; j++)
            {
                num *= 10;
            }
            result += num;
        }

        confirm_button.SetActive(size != result);
        if (confirm_button.activeInHierarchy)
        {
            confirm_button.GetComponent<Button>().onClick.RemoveAllListeners();
            confirm_button.GetComponent<Button>().onClick.AddListener(
                delegate
                {
                    level_Info_instance.map_size = size = result;
                    confirm_size_change();
                });
        }

        //level id
        confirm_button1.SetActive(level_id != level_ID_input.text);
        if (confirm_button1.activeInHierarchy)
        {
            confirm_button1.GetComponent<Button>().onClick.RemoveAllListeners();
            confirm_button1.GetComponent<Button>().onClick.AddListener(
                delegate
                {
                    level_id = level_ID_input.text;
                    ReadLevel.ReadLevelByName(level_id, out level_Info_instance);
                    confirm_size_change();

                });
        }

        // level name
        level_Info_instance.level_name = level_name_input.text;

        //select
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        //edit
        draw_mouse();


    }

    bool out_of_boundary(Vector2Int coordinate)
    {
        return Mathf.Abs(coordinate.x) > size - 1 || Mathf.Abs(coordinate.y) > size - 1;
    }


    void drawing_init()
    {

        size = level_Info_instance.map_size;
        level_size.text = size.ToString();
        level_name_input.text = level_Info_instance.level_name;

        grid_pens = new();

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                cell new_cell_top_right = new() { coordinate = { x = i, y = j }, accessible = true };
                cell new_cell_top_left = new() { coordinate = { x = -i, y = j }, accessible = true };
                cell new_cell_bottom_left = new() { coordinate = { x = -i, y = -j }, accessible = true };
                cell new_cell_bottom_right = new() { coordinate = { x = i, y = -j }, accessible = true };

                cells.TryAdd(new Vector2Int(i, j), new_cell_top_right);
                cells.TryAdd(new Vector2Int(-i, j), new_cell_top_left);
                cells.TryAdd(new Vector2Int(-i, -j), new_cell_bottom_left);
                cells.TryAdd(new Vector2Int(i, -j), new_cell_bottom_right);
                if (j == size - 1 || i == size - 1)
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

                }
            }




        }
        for (int i = -size + 1; i < size + 1; i++)
        {
            Vector2Int start1 = Vector2Int.right * i + Vector2Int.down * (size - 1);
            Vector2Int end1 = start1 + ((size - 1) * 2 + 1) * Vector2Int.up;
            line_draw_straight(Color.black, start1, end1);

            Vector2Int start2 = Vector2Int.up * i + Vector2Int.left * (size - 1);
            Vector2Int end2 = start2 + ((size - 1) * 2 + 1) * Vector2Int.right;
            line_draw_straight(Color.black, start2, end2);
        }

        //refresh cells
        int brush_count = place_info_pens.Count;
        var place_info = place_info_pens.ToList();
        for (int j = 0; j < brush_count; j++)
        {
            clear(place_info[j].Key);
        }


        //draw cells
        int level_cells_count = level_Info_instance.cell_infos.Count;
        if (level_cells_count > 0)
        {
            for (int i = 0; i < level_cells_count; i++)
            {
                cell_info cell = level_Info_instance.cell_infos[i];
                place(cell.place_location, cell.place_data);
            }
        }

        recorded_size = size;

    }
    void draw_mouse()
    {
        Vector2 mouse_po = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int mouse_po_round = mouse_po.world_2_grid();
        mouse_drawer.l_r.enabled = !mouse_drawer.erase;
        if (!mouse_drawer.erase)
        {
            constant_draw_square(mouse_drawer.data.color, mouse_drawer.l_r, mouse_po_round);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            mouse_drawer.erase = false;
            mouse_drawing = true;
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            mouse_drawer.erase = true;
            mouse_drawing = true;

        }
        if (mouse_drawing == false)
            return;

        if (!mouse_past.Contains(mouse_po_round))
        {
            mouse_past.Add(mouse_po_round);
        }


        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            mouse_past.ForEach(x => place(x, mouse_drawer.data));
            mouse_past = new();
            mouse_drawing = false;

        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            mouse_past.ForEach(x => erase(x));
            mouse_past = new();
            mouse_drawing = false;
        }
    }
    void constant_draw_square(Color color, LineRenderer l_r, Vector2Int coordinate)
    {
        l_r.widthMultiplier = 1;
        l_r.material = default_line;
        l_r.loop = true;
        l_r.endColor = l_r.startColor = color;
        Vector3[] coors = { (Vector3Int)coordinate + Vector3.up * .5f, (Vector3Int)coordinate + Vector3.up * .5f + Vector3.right };
        if (out_of_boundary(coordinate))
        {
            return;
        }
        l_r.SetPositions(coors);
    }
    void line_draw_square(Color color, Vector2Int coordinate)
    {

        LineRenderer l_r = Instantiate(pen_prefab).GetComponent<LineRenderer>();
        l_r.material = default_line;
        l_r.loop = true;
        l_r.endColor = l_r.startColor = color;
        Vector3[] coors = { (Vector3Int)coordinate + Vector3.up * .5f, (Vector3Int)coordinate + Vector3.up * .5f + Vector3.right };
        l_r.SetPositions(coors);
        grid_pens.Add(l_r.gameObject);
    }
    void line_draw_straight(Color color, Vector2Int start, Vector2Int end)
    {
        LineRenderer l_r = Instantiate(pen_prefab).GetComponent<LineRenderer>();
        l_r.endColor = l_r.startColor = color;
        l_r.material = default_line;
        Vector3[] coors = { (Vector3Int)start, (Vector3Int)end };
        l_r.SetPositions(coors);
        grid_pens.Add(l_r.gameObject);
    }
    void adjust_level_info_size()
    {
        level_Info_instance.map_size = size;
        List<cell_info> infos = level_Info_instance.cell_infos;
        List<cell_info> remove_list = new();
        foreach (cell_info info in infos)
        {
            if (out_of_boundary(info.place_location))
            {
                remove_list.Add(info);
            }
        }
        infos.RemoveAll(x=>remove_list.Contains(x));
    }
    void adjust_place_info_size()
    {
        List<KeyValuePair<Vector2Int,GameObject>> place_pens = place_info_pens.ToList();
        List<KeyValuePair<Vector2Int, GameObject>> destroy_list = new();
        foreach (var pen in place_pens)
        {
            if (out_of_boundary(pen.Key))
            {
                destroy_list.Add(pen);
            }
        }
        place_pens.RemoveAll(x => destroy_list.Contains(x));
        place_info_pens = new();
        place_pens.ForEach(x => place_info_pens.Add(x.Key,x.Value));
        destroy_list.ForEach(x => Destroy(x.Value));
    }
    void load_level_info(Level_Info_SO level)
    {
        size = level.map_size;
        foreach (var pen in place_info_pens.ToList())
        {
            Destroy(pen.Value);
        }
        place_info_pens = new();
        level_size.text = size.ToString();
        level_name_input.text = level.level_name;
        List<cell_info> cell_Infos= level.cell_infos;
        foreach(cell_info info in cell_Infos)
        {
            if (info.place_data == null)
            {
                return;
            }
            place(info.place_location, info.place_data);
        }
    }
    void place(Vector2Int coordinate,Place_Data_SO data)
    {
        if (out_of_boundary(coordinate))
        {
            return;
        }
        GameObject pen = Instantiate(pen_prefab);
        LineRenderer l_r = pen.GetComponent<LineRenderer>();
        l_r.widthMultiplier = 1;
        l_r.material = default_line;
        l_r.loop = true;
        l_r.endColor = l_r.startColor = data.color;
        Vector3[] coors = { (Vector3Int)coordinate + Vector3.up * .5f, (Vector3Int)coordinate + Vector3.up * .5f + Vector3.right };
 
        l_r.SetPositions(coors);

        if (place_info_pens.ContainsKey(coordinate))
        {
            Destroy(place_info_pens[coordinate]); 
            place_info_pens[coordinate] =pen;
        }
        else
        {
            place_info_pens.Add(coordinate,pen);
        }


        if (level_Info_instance.cell_infos.Any(x=>x.place_location==coordinate))
        {
            level_Info_instance.cell_infos.Find(x => x.place_location == coordinate).place_data = data;
        }
        else
        {
            level_Info_instance.cell_infos.Add(new() { place_location=coordinate,place_data=data});
        }
    }
    void clear(Vector2Int coordinate)
    {

        if (place_info_pens.ContainsKey(coordinate))
        {
            Destroy(place_info_pens[coordinate].gameObject);
            place_info_pens.Remove(coordinate);
        }
    }
    void erase(Vector2Int coordinate)
    {
        clear(coordinate);

        if (level_Info_instance.cell_infos.Any(x => x.place_location == coordinate))
        {
            level_Info_instance.cell_infos.RemoveAll(x => x.place_location == coordinate);
        }
 
    }

    public void toggle_walls()
    {
        buttons.ForEach(x => Destroy(x.gameObject));
        buttons = new();
        selcted_ = selected_panel.walls;
        for (int i = 0; i < walls.Count; i++)
        {
            add_button(walls[i],walls[i].color,i);
        }
    }
    public void toggle_player()
    {
        buttons.ForEach(x => Destroy(x.gameObject));
        buttons = new();
        selcted_ = selected_panel.player;
        for (int i = 0; i < players.Count; i++)
        {
            add_button(players[i],players[i].color, i);
        }
    }
    public void toggle_enemy()
    {
        buttons.ForEach(x => Destroy(x.gameObject));
        buttons = new();
        selcted_ = selected_panel.enemy;
        for (int i = 0; i < enemies.Count; i++)
        {
            add_button(enemies[i],enemies[i].color, i);
        }
    }

    public void add_button(Place_Data_SO data, Color color, int index)
    {

        GameObject button_instance = Instantiate(blank_button_prefab, panel);
        button_instance.GetComponent<RectTransform>().position += (index) * 110 * Vector3.right;
        Image image = button_instance.GetComponent<Image>();
        Button button = button_instance.GetComponent<Button>();
        Text text = button_instance.GetComponentInChildren<Text>();
        text.text = data.name;
        image.color = color;
        button.onClick.AddListener(delegate
        {
            mouse_drawer.data = data;
        });
        buttons.Add(button);
    }

    public void save_current_level()
    {
        level_Info_instance.Save(level_ID_input.text);
        General new_data = new() { progress = g_data.progress, test_level = level_ID_input.text };
        new_data.Save();
    }
 

    public void load_test_level()
    {
        save_current_level();
        SceneManager.LoadScene(2);
    }

    public void confirm_size_change()
    {
        grid_pens.ForEach(x => Destroy(x));
        drawing_init();
        adjust_level_info_size();
        adjust_place_info_size();
    }

    public void GotoMenu()
    {
        SceneManager.LoadScene(0);
    }

    public static Level_Info_File CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<Level_Info_File>(jsonString);
    }
}