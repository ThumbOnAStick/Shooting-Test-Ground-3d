using damage;
using stg_grid;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class camera_follow : MonoBehaviour
{
    //public
    public int camera_speed;
    [HideInInspector]
    public Vector2 camera_position
    {
        get
        {
            return new Vector2(transform.position.x, transform.position.z);
        }
    }
    [HideInInspector]
    public Isometric_controller_player player;

    //private
    Vector2 increment = new();



    private void Update()
    {
        follow_player();
    }

    Vector2 camera_to_ground(Vector3 camera_position)
    {
        return new Vector2(camera_position.x - 30.5f, camera_position.z - 30.5f);
    }

    Vector2Int camera_to_cell(Vector3 camera_position)
    {
        Vector2 ground= camera_to_ground(camera_position);
        return the_grid.object_2_grid(ground);
    }
 

    public void follow_player()
    {
        if (player == null)
            return;
        Vector2 player_position = new Vector2(player.transform.position.x, player.transform.position.z);
        Vector2 camera_to_player = player_position - camera_position;
        float x_dis = Mathf.Abs(camera_to_player.x - 30.5f);
        float y_dis = Mathf.Abs(camera_to_player.y - 30.5f);
        if (x_dis > 5f)
        {
            increment.x = camera_to_player.x - 30;
        }
        else if (Mathf.Abs(increment.x) > 0.001f)
        {
            increment.x -= Time.deltaTime * increment.x;
        }

        if (y_dis > 5f)
        {
            increment.y = camera_to_player.y - 30;
        }
        else if (Mathf.Abs(increment.y) > 0.001f)
        {
            increment.y -= Time.deltaTime * increment.y;
        }
        transform.position += new Vector3(increment.x, 0, increment.y) * camera_speed * Time.deltaTime;
    }


}

namespace tank_ui
{
    public struct health_bar
    {
        public health_bar(GameObject health_bar_instance_set, Transform client)
        {
            health_bar_instance = health_bar_instance_set;
            health_bar_client_transfrom = client;
        }

        public Transform health_bar_client_transfrom;
        public GameObject health_bar_instance;

        public RectTransform bar_rect
        {
            get
            {
                if (health_bar_instance == null)
                {
                    return null;
                }
                return health_bar_instance.transform.GetChild(1).GetComponent<RectTransform>();
            }
        }
        public RectTransform bottom
        {
            get
            {
                return health_bar_instance.transform.GetChild(0).GetComponent<RectTransform>();
            }
        }
        public float max_image_width
        {
            get
            {
                return health_bar_instance.GetComponent<RectTransform>().localScale.x;
            }
        }

        public void recover_all()
        {
            bar_rect.sizeDelta = new Vector2(50f, bar_rect.sizeDelta.y);
            bottom.sizeDelta = new Vector2(50f, bottom.sizeDelta.y);
        }
    }

    public static class health_bar_methods
    {

        public static void health_bar_update(this health_bar bar, damagable damagable_script, int value, game_manager manager)
        {
            if (bar.bar_rect == null)
            {
                return;
            }
            float percentage = (float)(damagable_script.health.current_hit_point) / damagable_script.max_health;
            RectTransform bar_rect = bar.bar_rect.GetComponent<RectTransform>();
            bar_rect.sizeDelta = new Vector2(50f * percentage, bar_rect.sizeDelta.y);
            manager.StartCoroutine(bar.health_bar_bottom_update(manager));
        }

        public static IEnumerator health_bar_bottom_update(this health_bar bar, game_manager manager)
        {
            float distance = bar.bar_rect.sizeDelta.x - bar.bottom.sizeDelta.x;
            float distnace_per_increment = distance / 10f;
            manager.StartCoroutine(health_bar_flow(bar, manager, distnace_per_increment));
            yield return null;
        }

        public static IEnumerator health_bar_flow(health_bar bar, game_manager manager, float increment)
        {
            if (bar.health_bar_instance == null)
                yield break;
            bar.bottom.sizeDelta = new Vector2(Mathf.Max(bar.bar_rect.sizeDelta.x, bar.bottom.sizeDelta.x + increment), bar.bottom.sizeDelta.y);
            yield return new WaitForSeconds(0.1f);
            if (bar.health_bar_instance == null)
                yield break;
            if (Mathf.Abs(bar.bar_rect.sizeDelta.x - bar.bottom.sizeDelta.x) > 0.01f)
            {
                manager.StartCoroutine(health_bar_flow(bar, manager, increment));
            }
        }

        public static Vector2 bottom_left_on_screen(this Transform target, float width)
        {
            Vector3 world_top_right = target.transform.position + new Vector3(-1, 0, 0) * width / 2;
            return (Vector2)Camera.main.WorldToScreenPoint(world_top_right);
        }


        public static Vector2 top_right_on_screen(this Transform target, float width)
        {
            Vector3 world_top_right = target.transform.position + new Vector3(1, 0, 0) * width / 2;
            return (Vector2)Camera.main.WorldToScreenPoint(world_top_right);
        }

    }

    public static class ui_movement_methods
    {
        static int total_frames = 50;
        public static void ui_move(RectTransform self_rect, Vector2 target_position, game_manager manager)
        {
            Vector2 total_descrapency = target_position - (Vector2)self_rect.position;
            Vector2 increment_per_frame = total_descrapency / total_frames;
            manager.StartCoroutine(ui_movement(self_rect, target_position, increment_per_frame, manager));
        }

        public static IEnumerator ui_movement(RectTransform self_rect, Vector2 target_position, Vector2 increment, game_manager manager)
        {
            self_rect.position += (Vector3)increment;
            yield return new WaitForSeconds(0.01f);
            if (Vector2.Distance((Vector2)self_rect.position, target_position) < 0.1f)
            {
                manager.StartCoroutine(ui_movement(self_rect, target_position, increment, manager));
            }
        }

        public static IEnumerator text_change_color(this Text text, Color target_color)
        {
            Color origin = text.color;
            text.color = target_color;
            yield return new WaitForSeconds(0.4f);
            text.color = origin;
        }

        public static IEnumerator image_fade_and_disable(this UnityEngine.UI.Image image, game_manager manager)
        {
            if (image.gameObject.activeSelf == false)
            {
                yield return null;
            }
            image.color = new() { r = 255f, b = 255f, g = 255f, a = image.color.a - 0.01f };
            if (image.color.a > 0)
            {
                yield return new WaitForSeconds(0.1f);
                if (manager != null)
                    manager.StartCoroutine(image_fade_and_disable(image, manager));
            }
            else
            {
                image.enabled = false;
                image.color = new() { a = 1 };
            }
        }

        public static IEnumerator text_fade_and_disable(this Text text, game_manager manager)
        {
            if (text.gameObject.activeSelf == false)
            {
                yield return null;
            }
            text.color = new() { r = text.color.r, b = text.color.b, g = text.color.g, a = text.color.a - 0.01f };
            if (text.color.a > 0)
            {
                yield return new WaitForSeconds(0.1f);
                if (manager != null)
                    manager.StartCoroutine(text_fade_and_disable(text, manager));
            }
            else
            {
                text.enabled = false;
                text.color = new() { a = 1 };
            }
        }

        public static class reloading_bar_methods
        {
            //public static void start_round(this UnityEngine.UI.Image bar, float spin_time, game_manager manager)
            //{
            //    bar.fillAmount = 0;
            //    float spin_amount = 0.1f / spin_time;
            //    manager.StartCoroutine(spin_clockwise(bar, spin_amount, manager));
            //}
            //public static IEnumerator spin_clockwise(this UnityEngine.UI.Image bar, float spin_amount, game_manager manager)
            //{
            //    bar.fillAmount += spin_amount;
            //    yield return new WaitForSeconds(0.1f);
            //    if (bar == null)
            //    {
            //        yield break;
            //    }
            //    if (bar.fillAmount < 1)
            //    {
            //        manager.StartCoroutine(spin_clockwise(bar, spin_amount, manager));
            //    }
            //}
        }
    }
    public static class loading_ring_methods
    {
        public static void start_round(this UnityEngine.UI.Image ring, float spin_time, game_manager manager)
        {
            ring.fillAmount = 0;
            float spin_amount = 0.1f / spin_time;
            manager.StartCoroutine(spin_clockwise(ring, spin_amount, manager));
        }
        public static IEnumerator spin_clockwise(this UnityEngine.UI.Image ring, float spin_amount, game_manager manager)
        {
            ring.fillAmount += spin_amount;
            yield return new WaitForSeconds(0.1f);
            if (ring == null)
            {
                yield break;
            }
            if (ring.fillAmount < 1)
            {
                manager.StartCoroutine(spin_clockwise(ring, spin_amount, manager));
            }
        }
    }
}