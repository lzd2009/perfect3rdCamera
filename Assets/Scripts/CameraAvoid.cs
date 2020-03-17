using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAvoid : MonoBehaviour
{

    private float forwordDistance;

    //相机向前向后移动至碰撞点时，将维持的z轴偏移，避免相机与碰撞点完全重合，导致射线检测出问题
    private float offset = 0.001f;

    //填写相机的原始的固定的z轴值。
    public float fixedZ = -4.23f;

    //填写相机的固定的y偏移
    public float fixedY = 2.62f;


    //相机后退的速度
    public float backSpeed = 1.5f;

    //给需要让相机回避的物体设置layer：Env
    private int envLayerMask;

    public Transform characterTrans;

    //四个射线
    public Transform rightRay;
    public Transform leftRay;
    public Transform upRay;
    public Transform downRay;

    //引入这个，解决forwordDistance计算问题。forwordDistance = Vector3.Distance(helpPoint.position,characterCameraContainer.position);
    private Transform helpPoint;


    //引入这个，解决了相机旋转x轴导致的前进后退非z轴问题。
    private Transform characterCameraContainer;

    private void Awake()
    {
        int layer = LayerMask.NameToLayer("Env");
        characterCameraContainer = transform.Find("cameraContainer");
        envLayerMask = (int)Mathf.Pow(2, layer);
        helpPoint = (new GameObject("AvoidCamera_FD_Point")).transform;
        helpPoint.SetParent(characterTrans, false);
        helpPoint.transform.localPosition = new Vector3(0, fixedY, 0);
    }

    public void AvoidEnv()
    {
        AvoidEnvRayCast(leftRay);
        AvoidEnvRayCast(rightRay);
        AvoidEnvRayCast(upRay);
        AvoidEnvRayCast(downRay);
    }


    private void AvoidEnvRayCast(Transform rayTransform)
    {

        forwordDistance = Vector3.Distance(rayTransform.parent.position, helpPoint.position);

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
            characterCameraContainer.localPosition = characterCameraContainer.localPosition + new Vector3(0, 0, finalLocalZ);
            //下面的代码是加了动画的，前进做平滑动画有穿模的风险，所以不加动画
            //Vector3 finalPos = characterCameraContainer.localPosition + new Vector3(0, 0, finalLocalZ);
            //characterCamera.localPosition = Vector3.Lerp(characterCamera.localPosition, finalPos, 10f * Time.fixedDeltaTime);
        }
        else
        {
            //向前运动和向后运动是互斥的，同一时刻只允许存在一个方向的运动
            //射线2，从当前ray位置，朝向ray的后面发出，遇到环境，相机就放在第一个接触的点位
            RaycastHit hitInfo2;
            //这是直线距离
            float currentToFixed = Mathf.Abs(fixedZ - characterCameraContainer.localPosition.z);
            if (currentToFixed <= 0.001f)
                currentToFixed = 0;
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
                    //characterCameraContainer.localPosition = characterCameraContainer.localPosition - new Vector3(0, 0, finalLocalZ);
                    //直接退到点位有点丑，来加一个后退动画，像怪物猎人那样
                    Vector3 finalPos = characterCameraContainer.localPosition - new Vector3(0, 0, finalLocalZ);

                    characterCameraContainer.localPosition = Vector3.Lerp(characterCameraContainer.localPosition, finalPos, backSpeed * Time.fixedDeltaTime);
                }
                else   //可以直接退到fixed位置，因为范围内没有东西
                {
                    //下面这句代码是直接退到最后
                    //characterCamera.localPosition = characterCamera.localPosition- new Vector3(0, 0, currentToFixed);
                    //直接退到最后有点丑,做一个的后退动画。像怪物猎人那样
                    Vector3 fixedPos = characterCameraContainer.localPosition - new Vector3(0, 0, currentToFixed);
                    characterCameraContainer.localPosition = Vector3.Lerp(characterCameraContainer.localPosition, fixedPos, backSpeed * Time.fixedDeltaTime);
                }
            }
        }
    }
}
