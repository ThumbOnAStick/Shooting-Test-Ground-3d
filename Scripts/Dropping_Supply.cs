using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dropping_Supply : game_manager_client
{
    public float start_height;
    public float fall_speed;
    public Transform parachute;


    [HideInInspector]
    public UnityEngine.UI.Image ring;
    [HideInInspector]
    public GameObject line;
    [HideInInspector]   
    public LineRenderer l;

    public GameObject line_prefab;
  

    public void Init()
    {
        transform.position = new() { x = transform.position.x, y = start_height, z = transform.position.z };
    }

    //private void Update()
    //{
    //    if (transform.position.y > 0.2f)
    //    {
    //        transform.position += Vector3.down * fall_speed * Time.deltaTime;
    //    }
    //    else
    //    {
    //        parachute.localPosition += Vector3.down * fall_speed * Time.deltaTime;
    //        transform.position = new() { x = transform.position.x, y = 0, z = transform.position.z };
    //        if (_game_manager.player == null)
    //            return;
    //        Transform player = _game_manager.player.transform;
    //        if (Vector3.Distance(player.position, transform.position) < 1)
    //        {
    //            Destroy(this.gameObject);
    //            _game_manager.repair_player();
    //        }

    //    }

    //}
    private void Update()
    {
        if (_game_manager.player == null)
            return;
        Transform player = _game_manager.player.transform;

        //falling
        if (transform.position.y > 0.2f)
        {
            transform.position += Vector3.down * fall_speed * Time.deltaTime;
        }
        //fallen
        else
        {
            parachute.localPosition += Vector3.down * fall_speed * Time.deltaTime;
            transform.position = new() { x = transform.position.x, y = 0, z = transform.position.z };

            if (ring == null)
            {
                if (Vector3.Distance(player.position, transform.position) < 3)
                {
                    ring = _game_manager.init_load_ring(transform);
                    line = Instantiate(line_prefab, transform.position, Quaternion.identity);
                    l = line.GetComponent<LineRenderer>();
                    Vector3[] positions = { transform.position, player.position };
                    l.SetPositions(positions);
                }
            }
            else
            {
                _game_manager.ring_follow_transform(ring, transform);

                Vector3[] positions = { transform.position, player.position };
                l.SetPositions(positions);

                if (ring.fillAmount >= 1)
                {

                    _game_manager.repair_player();
                    Destroy(ring);
                    Destroy(line);
                    Destroy(gameObject);
                }

                if (Vector3.Distance(player.position, transform.position) > 3)
                {
                    Destroy(ring);
                    Destroy(line);
                }
            }
        }

        

    }
}
