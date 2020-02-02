using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    public Transform followTarget;
    private Character character;

    public float mouseXSpeed = 1;
    public float mouseYSpeed = 1;

    public float smoothTime = 0.2f;
    public float maxSpeed = 10;

    private Transform characterCamera;
    private int envLayerMask;

    //由上往下看的限制角度
    private float clampX_Up = 60;
    //由下往上看的限制角度
    private float clampX_Down = 60;

    //相机向前向后移动至碰撞点时，将维持的z轴偏移，避免相机与碰撞点完全重合，导致射线检测出问题
    private float offset = 0.01f;

    //填写射线1的射线检测的起始位置（固定在相机的forword向量的forwordDistance距离处，朝相机发长度为forwordDistance的射线）。
    private float forwordDistance = 3;

    //填写相机的原始的固定的z轴值。
    private float fixedLength = -4.23f;


    private void Awake()
    {
        character = followTarget.GetComponent<Character>();
        int layer= LayerMask.NameToLayer("Env");
        characterCamera = Camera.main.transform;
        envLayerMask = (int)Mathf.Pow(2, layer);
    }

    //画线方便debug
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(originalPos, characterCamera.position - originalPos);
        Gizmos.DrawRay(characterCamera.position, targetPos);
    }

    private Vector3 originalPos;
    private Vector3 targetPos;
   
    private void FixedUpdate()
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
            if (transform.eulerAngles.x < (360-clampX_Down))
            {
                transform.eulerAngles = new Vector3(-clampX_Down, transform.eulerAngles.y, 0);
            }
        }
        //丢弃z旋转
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //好看的相机追踪。smoothTime大概0.1，maxSpeed大概15.
        Vector3 currentVelocity = Vector3.zero;
        transform.position = Vector3.SmoothDamp(transform.position, followTarget.position, ref currentVelocity, smoothTime, maxSpeed);


        #region 相机回避环境代码
        //关于相机穿模环境的问题：需要设置多条射线来解决，在相机近平面位置，四个角，都给上双射线的检测。通过提前预支，来避免穿模。
        //射线1，从相机前方3m位置，朝向相机发出，遇到环境，相机就放在第一个接触的点位
        RaycastHit hitInfo;            
        //局部坐标转世界
        Vector3 worldForword = characterCamera.transform.TransformDirection(Vector3.forward * forwordDistance);
        originalPos = characterCamera.transform.position + worldForword;

        bool exist = Physics.Raycast(originalPos, characterCamera.position - originalPos, out hitInfo, forwordDistance, envLayerMask);
        if (exist == true)
        {
            float distanceToHitPoint = forwordDistance - hitInfo.distance;
            characterCamera.localPosition = characterCamera.localPosition + new Vector3(0, 0, distanceToHitPoint + offset);        
        }
        else
        {
            //向前运动和向后运动是互斥的，同一时刻只允许存在一个方向的运动
            //射线2，从相机位置，朝向相机的后面发出，遇到环境，相机就放在第一个接触的点位
            RaycastHit hitInfo2;
            float currentToFixed = Mathf.Abs(fixedLength - characterCamera.localPosition.z);

            if (currentToFixed > 0) //这个逻辑里面全是向后退，内部区别只是向后退到哪里而已
            {
                Vector3 worldBack = characterCamera.transform.TransformDirection(-Vector3.forward * currentToFixed);
                targetPos = worldBack;
                bool exist2 = Physics.Raycast(characterCamera.position, targetPos, out hitInfo2, currentToFixed, envLayerMask);
                if (exist2 == true) //没法退到fixed位置，因为碰到东西了
                {
                    characterCamera.localPosition = characterCamera.localPosition - new Vector3(0, 0, hitInfo2.distance - offset);
                }
                else   //可以直接退到fixed位置，因为范围内没有东西
                {
                    characterCamera.localPosition = characterCamera.localPosition - new Vector3(0, 0, currentToFixed);
                }

            }
        }
        #endregion
    }
}
