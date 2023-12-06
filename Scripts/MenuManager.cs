using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public AudioClip mouse_over;
    public AudioClip mouse_click;
   
    public Button continue_button;
    public List<Button> buttons;

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

    AudioSource source;
    GameObject high_light_one;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        buttons.ForEach(x => x.onClick.AddListener(delegate
        {
            PlayClickSound();
        }));

        if (!File.Exists(general_data_path))
        {
            General data = new() { progress=0,test_level=""};
            data.Save();
        }

        string json_file =File.ReadAllText(general_data_path);
        General data1 =JsonUtility.FromJson<General>(json_file);
        if(data1.progress==0)
            continue_button.interactable = false;
    }

    private void Update()
    {
        if (IsPointerOverNewUIElement())
        {
            source.clip=mouse_over;
            source.Play();
        }
    }

    public bool IsPointerOverNewUIElement()
    {
        return IsPointerOverNewUIElement(GetEventSystemRaycastResults());
    }


    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverNewUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.tag == "Button")
            {
                if (high_light_one == curRaysastResult.gameObject)
                {
                    return false;
                }
                high_light_one = curRaysastResult.gameObject;
                return true;
            }
        }
        high_light_one = null;
        return false;
    }


    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        return raysastResults;
    }

    public void PlayClickSound()
    {
        source.clip =mouse_click;
        source.Play();
    }

    public void Quit()
    {
        Application.Quit();
    }
    public void GotoEditor()
    {
        SceneManager.LoadScene(1);
    }
    public void StartNewGame()
    {
        General data = new() { progress = 0, test_level = g_data.test_level };
        data.Save();
        SceneManager.LoadScene(3);
    }
    public void Continue()
    {
        SceneManager.LoadScene(3);

    }
}
