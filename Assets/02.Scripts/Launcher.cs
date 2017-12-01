using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour {
    public Transform m_arrow_point_tr;

    private Transform m_transform;
    private Transform m_arrow_tr;

    //const float MIN_X_ANGLE = -85f;
    //const float MAX_X_ANGLE = 45f;

    const float MIN_Y_ANGLE = -15f;
    const float MAX_Y_ANGLE = 15f;

    //const float x_rot_speed = 50f;
    const float y_rot_speed = 50f;

    //float m_x_angle = 0f;
    float m_y_angle = 0f;

    bool m_is_button_pressed = false;
    Vector3 m_mouse_down_pos;

    LineRenderer m_line_renderer;

    // Use this for initialization
    void Start () {
        m_transform = GetComponent<Transform>();
        m_arrow_tr = GameObject.FindGameObjectWithTag("ARROW").GetComponent<Transform>();
        m_line_renderer = GetComponent<LineRenderer>();

        float x_angle = 0f;
        SceneManager.Instance.GetLauncherInitialRotation(ref x_angle, ref m_y_angle);
    }
	
	// Update is called once per frame
	void Update () {
        float y_angle = 0f;

#if false
        float h = Input.GetAxis("Horizontal");
        //float v = -1f * Input.GetAxis("Vertical");

        //float x_angle_delta = v * x_rot_speed * Time.deltaTime;
        //m_x_angle += x_angle_delta;
        //if (m_x_angle < MIN_X_ANGLE) m_x_angle = MIN_X_ANGLE;
        //if (m_x_angle > MAX_X_ANGLE) m_x_angle = MAX_X_ANGLE;

        float y_angle_delta = h * y_rot_speed * Time.deltaTime;
        y_angle = m_y_angle + y_angle_delta;
#else
        if(Input.GetMouseButtonDown(0))
        {
            m_is_button_pressed = true;
            m_mouse_down_pos = Input.mousePosition;
        }
        else if(Input.GetMouseButtonUp(0))
        {
            if(m_is_button_pressed)
            {
                m_is_button_pressed = false;
            }
        }
        else if(Input.GetMouseButton(0) && m_is_button_pressed)
        {
            Vector3 mouse_pos = Input.mousePosition;
            // Debug.Log(mouse_pos);

            float diff_x = mouse_pos.x - Screen.width / 2f;
            Debug.Log("diff_x=" + diff_x);
            float normal_diff_x = diff_x / (Screen.width / 2f * 0.75f);
            Debug.Log("normal_diff_x=" + normal_diff_x);
            normal_diff_x = Mathf.Clamp(normal_diff_x, -1f, 1f);

            y_angle = normal_diff_x * MAX_Y_ANGLE;

            Debug.Log(y_angle);

            // Screen.width

        }

#endif
        RotateHorizontal(y_angle);

        if (SceneManager.Instance.GS == GameState.IDLE
            || SceneManager.Instance.GS == GameState.AIMING
            || SceneManager.Instance.GS == GameState.PULLING)
        {
            m_arrow_tr.position = m_arrow_point_tr.position;
            m_arrow_tr.rotation = m_arrow_point_tr.rotation;
        }
    }

    void RotateHorizontal(float y_angle)
    {
        if (y_angle < MIN_Y_ANGLE) y_angle = MIN_Y_ANGLE;
        if (y_angle > MAX_Y_ANGLE) y_angle = MAX_Y_ANGLE;

        Vector3 angle = new Vector3(0f, y_angle, 0f);
        m_transform.localRotation = Quaternion.Euler(angle);

        m_y_angle = y_angle;
    }

    public void Prepare()
    {
        m_y_angle = 0f;
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        m_line_renderer.enabled = visible;
    }

    private void OnDrawGizmos()
    {
        
    }
}
