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

    //填写射线1的射线检测的起始位置（固定在对应ray向量forword方向的forwordDistance距离处，朝相机发长度为forwordDistance的射线）。
    private float forwordDistance = 3;

    //填写相机的原始的固定的z轴值。
    private float fixedLength = -4.23f;


    //四个射线朝向
    public Transform rightRay;
    public Transform leftRay;
    public Transform upRay;
    public Transform downRay;


    private void Awake()
    {
        character = followTarget.GetComponent<Character>();
        int layer= LayerMask.NameToLayer("Env");
        characterCamera = Camera.main.transform;
        envLayerMask = (int)Mathf.Pow(2, layer);

        rightRay = transform.Find("Main Camera/rightRay");
        leftRay = transform.Find("Main Camera/leftRay");
        upRay = transform.Find("Main Camera/upRay");
        downRay = transform.Find("Main Camera/downRay");
    }


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
        AvoidEnv();
        #endregion
    }


    private void AvoidEnv()
    {
        AvoidEnvRayCast(leftRay);
        AvoidEnvRayCast(rightRay);
        AvoidEnvRayCast(upRay);
        AvoidEnvRayCast(downRay);
    }



    private void AvoidEnvRayCast(Transform rayTransform)
    {
        RaycastHit hitInfo;
        //局部坐标转世界
        Vector3 worldForword = rayTransform.TransformDirection(Vector3.forward * forwordDistance);
        Vector3 originalPos = rayTransform.position + worldForword;
        bool exist = Physics.Raycast(originalPos, rayTransform.position - originalPos, out hitInfo, forwordDistance, envLayerMask);
        if (exist == true)
        {
            float distanceToHitPoint = forwordDistance - hitInfo.distance;
            //在加上一点offset，最终相机位置
            float finalLocalZ = distanceToHitPoint + offset;
            //下面这句代码直接前进到点位，无动画
            characterCamera.localPosition = characterCamera.localPosition + new Vector3(0, 0, finalLocalZ);
            //下面的代码是加了动画的，发现前进加动画还是不行啊，会穿模，后退可以加，前进不可以加
            //Vector3 finalPos = characterCamera.localPosition + new Vector3(0, 0, finalLocalZ);
            //characterCamera.localPosition = Vector3.Lerp(characterCamera.localPosition, finalPos, 10f * Time.fixedDeltaTime);
        }
        else
        {
            //向前运动和向后运动是互斥的，同一时刻只允许存在一个方向的运动
            //射线2，从当前ray位置，朝向ray的后面发出，遇到环境，相机就放在第一个接触的点位
            RaycastHit hitInfo2;
            //这是直线距离
            float currentToFixed = Mathf.Abs(fixedLength - characterCamera.localPosition.z);

            if (currentToFixed > 0) //这个逻辑里面全是向后退，内部区别只是向后退到哪里而已
            {
                Vector3 worldBack = rayTransform.TransformDirection(-Vector3.forward * currentToFixed);
                Vector3 targetPos = worldBack;
                bool exist2 = Physics.Raycast(rayTransform.position, targetPos, out hitInfo2, currentToFixed, envLayerMask);
                if (exist2 == true) //没法退到fixed位置，因为碰到东西了
                {
                    //在来一点offset，最终相机位置
                    
                    float finalLocalZ = hitInfo2.distance - offset;
                    //下面这句代码退到碰到的东西的位置
                    //characterCamera.localPosition = characterCamera.localPosition - new Vector3(0, 0, finalLocalZ);
                    //直接退到点位有点丑，来加一个后退动画，像怪物猎人那样
                    Vector3 finalPos = characterCamera.localPosition - new Vector3(0, 0, finalLocalZ);
                    characterCamera.localPosition = Vector3.Lerp(characterCamera.localPosition, finalPos, 1f * Time.fixedDeltaTime);
                }
                else   //可以直接退到fixed位置，因为范围内没有东西
                {
                    //下面这句代码是直接退到最后
                    //characterCamera.localPosition = characterCamera.localPosition - new Vector3(0, 0, currentToFixed);
                    //直接退到最后有点丑，我们来做一个漂亮的后退动画。像怪物猎人那样
                    Vector3 fixedPos = characterCamera.localPosition - new Vector3(0, 0, currentToFixed);
                    characterCamera.localPosition = Vector3.Lerp(characterCamera.localPosition, fixedPos,1f * Time.fixedDeltaTime);
                }

            }
        }
    }


    //用cos和斜边来算，射线必须是从camera的位置发出。这样就能根据point.distance和cos值，算出相机应该前进多少了（记得前进得和之前一样，加一点offset）
    //现在首先，我需要一个api，将想要发射的角度表达出来，用vector3表达。为了方便计算，我发射的4条射线，分别向上下左右的30度。不弄侧着的射线了。
    //不行，斜着的射线，逻辑本质上就是有问题的，必须用平行的射线来做。在camera的上下左右，0.3左右发射平行射线
    //private void AvoidEnvRayCast(Transform rayTransform,float angle)
    //{


    //    //射线1，固定在对应ray向量forword方向的forwordDistance距离处，朝相机发长度为forwordDistance的射线
    //    RaycastHit hitInfo;
    //    //局部坐标转世界
    //    Vector3 worldForword = rayTransform.TransformDirection(Vector3.forward * forwordDistance);
    //    originalPos = rayTransform.position + worldForword;

    //    bool exist = Physics.Raycast(originalPos, rayTransform.position - originalPos, out hitInfo, forwordDistance, envLayerMask);
    //    if (exist == true)
    //    {
    //        Debug.Log("刚才前面有遮挡，已经向前调整完毕");
    //        float distanceToHitPoint = forwordDistance - hitInfo.distance;
    //        //根据当前检测的ray向量的不同，需要前进不同距离
    //        //相机应当前进的量/distanceToHitPoint=cosValue
    //        float cosValue = Mathf.Cos(angle);
    //        //相机应当前进的量
    //        float cameraShouldMoveZ = distanceToHitPoint * cosValue;
    //        //在加上一点offset，最终相机位置
    //        float finalLocalZ = cameraShouldMoveZ + offset;
    //        characterCamera.localPosition = characterCamera.localPosition + new Vector3(0, 0, finalLocalZ);
    //    }
    //    else
    //    {
    //        Debug.Log("前方无遮挡");
    //        //向前运动和向后运动是互斥的，同一时刻只允许存在一个方向的运动
    //        //射线2，从当前ray位置，朝向ray的后面发出，遇到环境，相机就放在第一个接触的点位
    //        RaycastHit hitInfo2;
    //        //这是直线距离
    //        float currentToFixed = Mathf.Abs(fixedLength - characterCamera.localPosition.z);

    //        //currentToFixed/fixed时的ray位置=cosValue
    //        float cosValue = Mathf.Cos(angle);
    //        //这是当前ray位置，到fixed时的ray位置，的距离
    //        float currentRayToRayFixed = currentToFixed / cosValue;

    //        if (currentToFixed > 0) //这个逻辑里面全是向后退，内部区别只是向后退到哪里而已
    //        {
    //            Vector3 worldBack = rayTransform.TransformDirection(-Vector3.forward * currentRayToRayFixed);
    //            targetPos = worldBack;
    //            bool exist2 = Physics.Raycast(rayTransform.position, targetPos, out hitInfo2, currentRayToRayFixed, envLayerMask);
    //            if (exist2 == true) //没法退到fixed位置，因为碰到东西了
    //            {
    //                Debug.Log("后面有遮挡，只能退到指定极限位置");
    //                //相机应当后退的量，由cosValue =  cameraShouldMoveZ / hitInfo2.distance，可得：
    //                float cameraShouldMoveZ = hitInfo2.distance * cosValue;
    //                //在来一点offset，最终相机位置
    //                float finalLocalZ = cameraShouldMoveZ - offset;
    //                characterCamera.localPosition = characterCamera.localPosition - new Vector3(0, 0, finalLocalZ);
    //            }
    //            else   //可以直接退到fixed位置，因为范围内没有东西
    //            {
    //                Debug.Log("后面无遮挡，可以退到最后");
    //                characterCamera.localPosition = characterCamera.localPosition - new Vector3(0, 0, currentToFixed);
    //            }

    //        }
    //    }
    //}
}
