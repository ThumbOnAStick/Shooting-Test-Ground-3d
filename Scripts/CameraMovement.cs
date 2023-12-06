using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    public Level_Editor l_editor;
    Vector2 bottom_left
    {
        get
        {
            int size = l_editor.size;
            return new(-size, -size);
        }
    }
    Vector2 top_right
    {
        get
        {
            int size = l_editor.size;
            return new(size, size);
        }
    }
    private void Awake()
    {
        l_editor = FindObjectOfType<Level_Editor>();
    }
    private void Update()
    {
        Movement();

        Zoom();

        Mouse_Interface();


    }
    public void Movement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 velocity = new(h, v);
        velocity *= Time.deltaTime*3;
        transform.position += (Vector3)velocity;
        float lim_x = Mathf.Clamp(transform.position.x, -l_editor.size, l_editor.size);
        float lim_y = Mathf.Clamp(transform.position.y, -l_editor.size, l_editor.size);

        transform.position = new Vector3(lim_x, lim_y, transform.position.z);
    }
    public void Zoom()
    {
        Vector2 scroll = Input.mouseScrollDelta;
        float current_size=Camera.main.orthographicSize;
        current_size -= scroll.y;
        current_size = Mathf.Clamp(current_size, 5, 12);
        Camera.main.orthographicSize = current_size;
    }

    enum operation_mode { place,erase};
    operation_mode op_mode;
    public void Mouse_Interface()
    {
      
    


    }
}
