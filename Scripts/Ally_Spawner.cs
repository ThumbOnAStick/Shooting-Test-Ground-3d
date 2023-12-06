using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Ally_Spawner : game_manager_client
{
    [HideInInspector]
    public UnityEngine.UI.Image ring;
    [HideInInspector]
    public GameObject line;
    public LineRenderer l;

    public GameObject heavy_smoke_prefab;
    public GameObject line_prefab;
    public GameObject ally_prefab;
    private void Update()
    {
        if (_game_manager.player == null)
            return;
        Transform player = _game_manager.player.transform;
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
                GameObject h_smoke = Instantiate(heavy_smoke_prefab, transform.position, Quaternion.identity);
                Instantiate(ally_prefab, transform.position, Quaternion.identity,_game_manager.entities);
                _game_manager.reinforce();
                Destroy(h_smoke, 5f);
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
