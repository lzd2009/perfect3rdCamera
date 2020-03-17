using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform followTarget;
    private Character character;

    private CameraAvoid cameraAvoid;

    public float mouseXSpeed = 1;
    public float mouseYSpeed = 1;

    public float smoothTime = 0.2f;
    public float maxSpeed = 10;

    private Transform characterCameraContainer;
    private int envLayerMask;

    //由上往下看的限制角度
    private float clampX_Up = 60;
    //由下往上看的限制角度
    private float clampX_Down = 60;



    private void Awake()
    {
        character = followTarget.GetComponent<Character>();
        int layer = LayerMask.NameToLayer("Env");
        characterCameraContainer = transform.Find("cameraContainer");
        envLayerMask = (int)Mathf.Pow(2, layer);
        cameraAvoid = GetComponent<CameraAvoid>();
    }

    private void FixedUpdate()
    {
        CameraMove();
        cameraAvoid.AvoidEnv();
    }

    private void CameraMove()
    {
        //相机先旋转后移动，不然抖动
        transform.RotateAround(followTarget.position, Vector3.up, character.mouseX * mouseXSpeed);
        //对于上下轴，下面的注释代码是错误的，这个api中，指定围绕的x轴，应该是当前相机的x轴而不是世界空间的x轴，所以填transform.right，而不是vector3.right
        //transform.RotateAround(followTarget.position, Vector3.right, character.mouseY * mouseYSpeed);
        //正确的代码：
        transform.RotateAround(followTarget.position, transform.right.normalized, character.mouseY * mouseYSpeed);
        //对相机的x轴旋转进行clamp，太上太下都不行
        if (transform.eulerAngles.x > 0 && transform.eulerAngles.x < 180) //向上旋转
        {
            if (transform.eulerAngles.x > clampX_Up)
            {
                transform.eulerAngles = new Vector3(clampX_Up, transform.eulerAngles.y, 0);
            }
        }
        else if (transform.eulerAngles.x > 180 && transform.eulerAngles.x < 360)
        {
            if (transform.eulerAngles.x < (360 - clampX_Down))
            {
                transform.eulerAngles = new Vector3(-clampX_Down, transform.eulerAngles.y, 0);
            }
        }
        //丢弃z旋转
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //好看的相机追踪。smoothTime大概0.1，maxSpeed大概15.
        Vector3 currentVelocity = Vector3.zero;
        transform.position = Vector3.SmoothDamp(transform.position, followTarget.position, ref currentVelocity, smoothTime, maxSpeed);

    }


}
