using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandMine : game_manager_client
{
    public GameObject explosion_particle;

    private void Awake()
    {
        StartCoroutine(CheckTrigger());
    }

    public IEnumerator CheckTrigger()
    {
        List<Isometric_unit> unit_in_range=new();
        for (int i = 0; i < _game_manager.entities.childCount; i++)
        {
            Transform entity = _game_manager.entities.GetChild(i);
            Vector2 entity_flat = new Vector2(entity.position.x, entity.position.z);
            Vector2 self_falt = new Vector2(transform.position.x, transform.position.z);
            float distance = Vector2.Distance(entity_flat, self_falt);
            if (distance < .75f)
            {
                Isometric_unit new_unit = entity.GetComponent<Isometric_unit>();
                unit_in_range.Add(new_unit);
                explosion_particle.SetActive(true);
            }
        }

        if (unit_in_range.Count > 0)
        {
            unit_in_range.ForEach(x =>
            {
                _game_manager.apply_damage_to_unit(x,30);
            });
            _game_manager.play_sound_effects("Explosion");
            Destroy(gameObject,1.5f);
            yield break;

        }

        yield return new WaitForSeconds(.1f);
        StartCoroutine(CheckTrigger());
    }
}
