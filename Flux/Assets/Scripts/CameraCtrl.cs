using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    float yaw = 0f, pitch = 0f;
    //public float distToPlayer = 5f;
    public Transform player;
    public Vector3 cameraOffset;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        yaw += Input.GetAxis("Mouse X");
        pitch -= Input.GetAxis("Mouse Y");

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = player.position - transform.TransformVector(cameraOffset);
        // Earlier solution: transform.position = player.position - transform.forward * distToPlayer;
    }
}
