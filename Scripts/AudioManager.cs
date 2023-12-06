using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : game_manager_client
{
    readonly public int audio_capacity = 10;
    [HideInInspector]
    public int audio_taken = 0;
    [HideInInspector]
    public List<AudioSource> sources = new();

    [HideInInspector]
    public AudioSource ambience;
    [HideInInspector]
    public AudioClip ambience_clip;

    public override void awake_calls()
    {
        base.awake_calls();
        for (int i = 0; i < audio_capacity; i++)
        {
            AudioSource new_source = this.AddComponent<AudioSource>();
            sources.Add(new_source);
        }
        ambience = this.AddComponent<AudioSource>();
        ambience.loop = true;
        ambience.clip = ambience_clip;
        ambience.Play();
        ambience.Pause();
    }

    public void ContinueAmbience()
    {
        ambience.UnPause();
    }

    public void PauseAmbience()
    {
        ambience.Pause();
    }

    public IEnumerator play(AudioClip clip,float duration=0)
    {
        yield return new WaitForSeconds(duration);
        if (audio_taken == audio_capacity)
        {
            yield break;
        }
        audio_taken++;
        AudioSource source = sources.Find(x => x.clip == null);
        source.clip=clip;
        source.Play();  
        yield return new WaitForSeconds(clip.length);
        source.clip = null;
        audio_taken--;
    }

}
