using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour {
    private Transform m_transform;
    private float m_speed = 0f;
    private float m_wind = 0f;

	// Use this for initialization
	void Start () {
        m_transform = GetComponent<Transform>();
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 pos_delta = (m_transform.forward * m_speed + m_transform.right * m_wind) * Time.deltaTime;
        m_transform.position += pos_delta;
    }

    public void Fly(float speed)
    {
        m_speed = speed;
        m_wind = SceneManager.Instance.GetWind();
    }

    public void Hold()
    {
        m_speed = 0f;
        m_wind = 0f;
    }
}
