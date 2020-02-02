using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{

    [HideInInspector]
    public float HorizontalVal = 0;
    [HideInInspector]
    public float VerticalVal = 0;
    [HideInInspector]
    public float mouseX = 0;
    [HideInInspector]
    public float mouseY = 0;

    public float speed = 1;
    public float runSpeed = 2;
    public float turnSpeed = 0.3f;

    private Animator anim;
    public CameraHandler cameraHandler;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public KeyCode runKey = KeyCode.LeftShift;
    private void Update()
    {
        HorizontalVal = Input.GetAxis("Horizontal");
        VerticalVal = Input.GetAxis("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
    }

    private void FixedUpdate()
    {
        
        if ((HorizontalVal != 0) || (VerticalVal != 0))
        {


            //相机的正方向
            Vector3 cameraForword = cameraHandler.transform.TransformDirection(new Vector3(HorizontalVal, 0, VerticalVal));
            //旋转面向
            transform.forward  =Vector3.Slerp(transform.forward, cameraForword, turnSpeed);
            //不旋转xz，只旋转y
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

            //如果没有按run
            if (!Input.GetKey(runKey))
            {
                //walk的位移
                transform.position = transform.position + transform.forward * Time.fixedDeltaTime * speed;
                //walk的动画
                anim.SetInteger("move", 1);
            }
            else //如果按了run
            {
                
                //run的位移
                transform.position = transform.position + transform.forward * Time.fixedDeltaTime * runSpeed;
                //run的动画
                if (anim.GetInteger("move") == 0) 
                {
                    anim.SetInteger("move", 1);
                }
                else
                {
                    anim.SetInteger("move", 2);
                }
            }
        }
        else
        {//停下了，设置动画
            anim.SetInteger("move", 0);
        }
    }
}
