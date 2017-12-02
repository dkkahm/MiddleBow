using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum GameState
{
    IDLE,
    AIMING,
    PULLING,
    FLYING,
    ZOOM_IN,
    ZOOM_STAY,
    ZOOM_OUT,
    END,
}

public class SceneManager : MonoBehaviour {
    private Launcher m_launcher;
    private Transform m_arrow_tr;
    private Arrow m_arrow;
    private Camera m_camera;
    private Transform m_camera_tr;

    private const float ARROW_SPEED = 30.0f;

    private Vector3 m_camera_normal_pos = new Vector3(0f, 22f, -5f);
    private Quaternion m_camera_normal_rot = Quaternion.Euler(50f, 0f, 0f);
    private float m_camera_normal_size = 9f;

    private Vector3 m_camera_zoom_pos = new Vector3(5f, 4.2f, 23.5f);
    private Quaternion m_camera_zoom_rot = Quaternion.Euler(21f, -35f, 0f);
    private float m_camera_zoom_size = 2f;

    private Vector3 m_camera_target_pos;
    private Quaternion m_camera_target_rot;
    private float m_camera_target_size;
    private float m_camera_zoom_time;

    private const float CAMERA_ZOOM_IN_TIME = 1f;
    private const float CAMERA_ZOOM_STAY_TIME = 60f;
    private const float CAMERA_ZOOM_OUT_TIME = 1f;
    private float m_zoom_start_time = 0f;

    private const int MIN_SCORE = 0;
    private const int MAX_SCORE = 10;
    private const float BULLS_EYE_RANGE = 0.05f;
    private const int BULLS_EYE_SCORE = 20;
    private bool m_is_bulls_eye = false;

    public Text m_score_text;
    Vector3 m_target_pos_for_score;
    float m_target_size = 1f;
    private float m_score = 0f;

    private float m_wind = 0f;
    public RectTransform m_wind_thumb_tr;

    const int INITIAL_ARROW_COUNT = 10;
    private int m_remain_arrow_count = INITIAL_ARROW_COUNT;
    public Image[] m_remain_arrow_images;

    public GameObject m_retry_dialog_window;

    private const int SCORE_SPRITE_MISSED = 11;
    private const int SCORE_SPRITE_BULLS_EYE = 12;
    public Sprite[] m_score_sprites;
    public RectTransform m_score_image_tr;
    private Image m_score_image;
    private float m_show_score_image_start_time;
    private const float SHOW_SCORE_IMAGE_TIME = 1.0f;
    private const float SHOW_SCORE_FADE_IN_FACTOR = 0.3f;
    private const float SHOW_SCORE_FADE_OUT_FACTOR = 0.3f;

    public GameState GS
    {
        get;
        private set;
    }

    private void Awake()
    {
        s_instance = this;

        m_launcher = GameObject.Find("Launcher").GetComponent<Launcher>();

        GameObject arrow_obj = GameObject.FindGameObjectWithTag("ARROW");
        m_arrow = arrow_obj.GetComponent<Arrow>();
        m_arrow_tr = arrow_obj.GetComponent<Transform>();

        Transform target_tr = GameObject.Find("Target").GetComponent<Transform>();
        m_target_pos_for_score = target_tr.position;
        m_target_pos_for_score.z = 0f;
        m_target_size = target_tr.localScale.x;

        m_camera = Camera.main;
        m_camera_tr = m_camera.GetComponent<Transform>();

        m_score_image = m_score_image_tr.gameObject.GetComponent<Image>();
        m_score_image_tr.gameObject.SetActive(false);

        m_camera_target_pos = m_camera_normal_pos;
        m_camera_target_rot = m_camera_normal_rot;
        m_camera_target_size = m_camera_normal_size;
    }

    private void Start()
    {
        GS = GameState.IDLE;
    }

    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        switch (GS)
        {
            case GameState.IDLE:
                PrepareForAiming();
                break;

            case GameState.AIMING:
                if (Input.GetAxis("Fire1") == 1.0f)
                {
                    PullArrow();
                }
                break;

            case GameState.PULLING:
                if (Input.GetAxis("Fire1") == 0.0f)
                {
                    FireArrow();
                }
                break;

            case GameState.ZOOM_IN:
            case GameState.ZOOM_OUT:
                ZoomCamera();
                break;

            case GameState.ZOOM_STAY:
                if(Input.GetMouseButtonUp(0))
                {
                    ZoomOut();
                }
                break;

            case GameState.END:
                break;
        }
    }

    void PrepareForAiming()
    {
        if (m_remain_arrow_count > 0)
        {
            -- m_remain_arrow_count;

            for(int i = m_remain_arrow_count; i < m_remain_arrow_images.Length; ++ i)
            {
                m_remain_arrow_images[i].enabled = false;
            }

            GS = GameState.AIMING;

            m_launcher.Prepare();

            m_wind = Random.Range(-1f, 1f);
            m_wind_thumb_tr.anchoredPosition = new Vector3(m_wind * 100f, m_wind_thumb_tr.anchoredPosition.y);
        }
        else
        {
            GS = GameState.END;

            ShowRetryDialog();
        }
    }

    void PullArrow()
    {
        GS = GameState.PULLING;
    }

    void FireArrow()
    {
        GS = GameState.FLYING;

        Vector3 arrow_pos = Vector3.zero;
        Quaternion arrow_rot = Quaternion.identity;

        m_arrow.Fly(ARROW_SPEED);
        m_launcher.SetVisible(false);
    }

    public void HitTargetByArrow()
    {
        GS = GameState.ZOOM_IN;

        m_arrow.Hold();

        m_zoom_start_time = Time.time;
        m_camera_target_pos = m_camera_zoom_pos;
        m_camera_target_rot = m_camera_zoom_rot;
        m_camera_target_size = m_camera_zoom_size;
        m_camera_zoom_time = CAMERA_ZOOM_IN_TIME;

        int score = CalculateScore();
        m_score += score;
        m_score_text.text = m_score.ToString();

        StartShowingScoreImage(score);
    }

    public void Missed()
    {
        GS = GameState.IDLE;

        m_score_image.sprite = m_score_sprites[SCORE_SPRITE_MISSED];

        StartShowingScoreImage(-1);
    }

    void ZoomCamera()
    {
        if(Time.time < m_zoom_start_time + m_camera_zoom_time)
        {
            float t = (Time.time - m_zoom_start_time) / m_camera_zoom_time;

            m_camera_tr.position = Vector3.Lerp(m_camera_tr.position, m_camera_target_pos, t);
            m_camera_tr.rotation = Quaternion.Slerp(m_camera_tr.rotation, m_camera_target_rot, t);
            m_camera.orthographicSize = Mathf.Lerp(m_camera.orthographicSize, m_camera_target_size, t);
        }
        else
        {
            m_camera_tr.position = m_camera_target_pos;
            m_camera_tr.rotation = m_camera_target_rot;
            m_camera.orthographicSize = m_camera_target_size;

            switch(GS)
            {
                case GameState.ZOOM_IN:
                    GS = GameState.ZOOM_STAY;

                    m_zoom_start_time = Time.time;
                    m_camera_zoom_time = CAMERA_ZOOM_STAY_TIME;

                    break;

                case GameState.ZOOM_STAY:
                    ZoomOut();
                    break;

                case GameState.ZOOM_OUT:
                    GS = GameState.IDLE;
                    break;
            }
        }
    }

    void ZoomOut()
    {
        GS = GameState.ZOOM_OUT;

        m_zoom_start_time = Time.time;
        m_camera_target_pos = m_camera_normal_pos;
        m_camera_target_rot = m_camera_normal_rot;
        m_camera_target_size = m_camera_normal_size;
        m_camera_zoom_time = CAMERA_ZOOM_OUT_TIME;
    }

    public float GetWind()
    {
        return m_wind;
    }

    void StartShowingScoreImage(int score)
    {
        if (m_is_bulls_eye)
        {
            m_score_image.sprite = m_score_sprites[SCORE_SPRITE_BULLS_EYE];
        }
        else if(score < 0)
        {
            m_score_image.sprite = m_score_sprites[SCORE_SPRITE_MISSED];
        }
        else
        {
            m_score_image.sprite = m_score_sprites[score];
        }
        // m_score_image.sprite = m_score_sprites[SCORE_SPRITE_BULLS_EYE];

        m_score_image_tr.sizeDelta = m_score_image.sprite.rect.size;
        m_score_image.color = new Color(1f, 1f, 1f, 0.0f);

        m_show_score_image_start_time = Time.time;

        m_score_image.gameObject.SetActive(true);

        StartCoroutine(ShowScoreSprite());
    }

    IEnumerator ShowScoreSprite()
    {
        while(Time.time < m_show_score_image_start_time + SHOW_SCORE_IMAGE_TIME)
        {
            yield return new WaitForEndOfFrame();

            float t = (Time.time - m_show_score_image_start_time) / SHOW_SCORE_IMAGE_TIME;

            if(t < SHOW_SCORE_FADE_IN_FACTOR)
            {
                float alpha = t / SHOW_SCORE_FADE_IN_FACTOR;
                m_score_image.color = new Color(1f, 1f, 1f, alpha);
            }
            else if(t < 1.0f - SHOW_SCORE_FADE_OUT_FACTOR)
            {

            }
            else
            {
                float alpha = 1f - (t - (1 - SHOW_SCORE_FADE_OUT_FACTOR)) / SHOW_SCORE_FADE_OUT_FACTOR;
                m_score_image.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        m_score_image.gameObject.SetActive(false);
    }

    int CalculateScore()
    {
        int score = 0;

        Vector3 arrow_pos = m_arrow_tr.position;
        arrow_pos.z = 0f;

        float dist = Vector3.Distance(m_target_pos_for_score, arrow_pos) / m_target_size;
        m_is_bulls_eye = dist < BULLS_EYE_RANGE;

        if(m_is_bulls_eye)
        {
            score = BULLS_EYE_SCORE;
        }
        else
        {
            score = (int)Mathf.Floor(dist * -MAX_SCORE + MAX_SCORE);
            if (score < MIN_SCORE) score = MIN_SCORE;
        }

        Debug.Log(score);

        return score;
    }

    void ShowRetryDialog()
    {
        m_retry_dialog_window.SetActive(true);
    }

    public void OnClickRetryYes()
    {
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Application.LoadLevel("scBow");
    }

    public void OnClickRetryNo()
    {
        Application.Quit();
    }

    public void GetLauncherInitialRotation(ref float x_angle, ref float y_angle)
    {
        //x_angle = -50.0f;
        //y_angle = 0f;
    }

    private static SceneManager s_instance;

    public static SceneManager Instance
    {
        get
        {
            return s_instance;
        }
    }
}
