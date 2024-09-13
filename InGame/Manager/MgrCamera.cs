using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine.Serialization;

public class MgrCamera : Singleton<MgrCamera>
{
    [SerializeField] private Transform tfCameraRoot;
    [SerializeField] private Camera cameraMain;
    public Camera CameraMain => cameraMain;
    private Transform tfCamMain;
    [field: SerializeField] public Camera CameraUI { get; private set; }

    private int backGroundCnt;
    [SerializeField] private Transform[] arrTfBackGround;
    public BackGroundScroll[] ArrBgScroll { get; private set; }

    private MgrBattleSystem battleSys;

    [SerializeField] private float[] parallaxScales;	// The proportion of the camera's movement to move the backgrounds by
    private Vector3 startCamPos;	// the position of the camera in the previous frame
    
    private void Start()
    {
        battleSys = MgrBattleSystem.Instance;

        backGroundCnt = arrTfBackGround.Length;

        // The previous frame had the current frame's camera position
        tfCamMain = cameraMain.transform;
        startCamPos = tfCamMain.position;

        ArrBgScroll = new BackGroundScroll[backGroundCnt];

        for (int i = 0; i < backGroundCnt; i++)
            ArrBgScroll[i] = arrTfBackGround[i].GetComponent<BackGroundScroll>();
    }

    public void SetBG()
    {
        for (int i = 0; i < ArrBgScroll.Length; i++)
            ArrBgScroll[i].SetBackGround((BG_TYPE)i);
    }

    public void SetCameraShake(float _strength, float _duration, int _vibrate)
    {
        tfCameraRoot.DOKill();
        tfCameraRoot.transform.position = Vector3.zero;
        tfCameraRoot.DOShakePosition(_duration, _strength, _vibrate).SetEase(Ease.Linear);
    }

    private void Update()
    {
        if (battleSys.GameMode != GAME_MODE.Pvp || !battleSys.isStageStart)
            return;

    }

    private Vector3 v3TouchPos;
    private Vector3 v3MovedMousePos;
    private bool isTouched = false;
    private void LateUpdate()
    {
        if (!battleSys.isStageStart)
            return;

        if (battleSys.GameMode == GAME_MODE.Pvp)
        {
            if (Input.GetMouseButton(0))
            {
                v3MovedMousePos = cameraMain.ScreenToWorldPoint(Input.mousePosition) - cameraMain.transform.position;
                if (!isTouched)
                {
                    v3TouchPos = cameraMain.ScreenToWorldPoint(Input.mousePosition);
                    isTouched = true;
                }
            }
            else
            {
                isTouched = false;
            }

            if (isTouched)
            {
                Vector3 v3NextPos = new Vector3(v3TouchPos.x - v3MovedMousePos.x, 0.0f, -10.0f);
                
                for (int i = 0; i < backGroundCnt; i++)
                {
                    arrTfBackGround[i].position = new Vector3((v3NextPos.x - startCamPos.x) * parallaxScales[i], arrTfBackGround[i].position.y, 0.0f);
                    ArrBgScroll[i].CalculateBackGround(tfCamMain.position.x);
                }

                cameraMain.transform.position = v3NextPos;
            }

            return;
        }
        
        float xPos = battleSys.GetAllyBase().transform.position.x;
        Vector3 nextPos = Vector3.Lerp(new Vector3(cameraMain.transform.position.x, 0.0f, -10.0f), new Vector3(xPos + 9.5f, 0.0f, -10.0f), Time.deltaTime * 2.0f);
        
        for (int i = 0; i < backGroundCnt; i++)
        {
            arrTfBackGround[i].position = new Vector3((nextPos.x - startCamPos.x) * parallaxScales[i], arrTfBackGround[i].position.y, 0.0f);
            ArrBgScroll[i].CalculateBackGround(tfCamMain.position.x);
        }

        cameraMain.transform.position = nextPos;
    }
}
