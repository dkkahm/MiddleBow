using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBackboard : MonoBehaviour {
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "ARROW")
        {
            SceneManager.Instance.HitTargetByArrow();
        }
    }

}
