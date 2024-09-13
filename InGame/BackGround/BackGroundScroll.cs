using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BG_TYPE
{
    BackGround = 0,
    Back_Distance,
    MiddleBack_Distance,
    MiddleNear_Distance,
    Floor,
    Near_Distacne
}

public class BackGroundScroll : MonoBehaviour
{
    private Transform[] arrTfBackGround;
    private SpriteRenderer[] arrSprrdBackGround;
    private SpriteRenderer[] arrSprrdBackGround_Changed;

    private int currBackGroundIndex;
    private int disabledBackGroundIndex;

    private MaterialPropertyBlock mpbFade;

    [SerializeField] private bool isSpriteWidthFilled;
    [SerializeField] private float spriteWidthSpace;
    //_SourceAlphaDissolveFade

    private void Awake()
    {
        arrTfBackGround = new Transform[transform.childCount];
        arrSprrdBackGround = new SpriteRenderer[transform.childCount];
        arrSprrdBackGround_Changed = new SpriteRenderer[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
        {
            arrTfBackGround[i] = transform.GetChild(i);
            arrSprrdBackGround[i] = arrTfBackGround[i].GetComponent<SpriteRenderer>();
        }

        for (int i = 0; i < arrSprrdBackGround.Length; i++)
            arrSprrdBackGround_Changed[i] = arrSprrdBackGround[i].transform.GetChild(0).GetComponent<SpriteRenderer>();

        currBackGroundIndex = 0;
        disabledBackGroundIndex = 1;
    }

    public void SetBackGround(BG_TYPE _bgType)
    {
        Sprite sprImage = null;
        Sprite sprChangedImage = null;
        BackGroundData.ChapterBackGround bgData = MgrBattleSystem.Instance.ChapterBackGroundData;
        switch (_bgType)
        {
            case BG_TYPE.BackGround:
                sprImage = bgData.sprBackGround;
                sprChangedImage = bgData.sprBackGround_Changed;
                transform.position = Vector3.zero + Vector3.up * bgData.yPosBackGround;
                break;
            case BG_TYPE.Back_Distance:
                sprImage = bgData.sprBackDistance;
                sprChangedImage = bgData.sprBackDistance_Changed;
                transform.position = Vector3.zero + Vector3.up * bgData.yPosBackDistance;
                break;
            case BG_TYPE.MiddleBack_Distance:
                sprImage = bgData.sprMiddleBack_Distance;
                sprChangedImage = bgData.sprMiddleBack_Distance_Changed;
                transform.position = Vector3.zero + Vector3.up * bgData.yPosMiddleBack_Distance;
                break;
            case BG_TYPE.MiddleNear_Distance:
                sprImage = bgData.sprNearBack_Distance;
                sprChangedImage = bgData.sprNearBack_Distance_Changed;
                transform.position = Vector3.zero + Vector3.up * bgData.yPosNearBack_Distance;
                break;
            case BG_TYPE.Floor:
                sprImage = bgData.sprFloor;
                sprChangedImage = bgData.sprFloor_Changed;
                transform.position = Vector3.zero + Vector3.up * bgData.yPosFloor;
                break;
            case BG_TYPE.Near_Distacne:
                sprImage = bgData.sprNear;
                sprChangedImage = bgData.sprNear_Changed;
                transform.position = Vector3.zero + Vector3.up * bgData.yPosNear;
                break;
        }

        for (int i = 0; i < arrSprrdBackGround.Length; i++)
        {
            arrSprrdBackGround[i].sprite = sprImage;
            arrSprrdBackGround[i].transform.localPosition = new Vector3(arrSprrdBackGround[i].transform.position.x, 0.0f, 0.0f);
        }
        for (int i = 0; i < arrSprrdBackGround_Changed.Length; i++)
        {
            if (sprChangedImage == null) arrSprrdBackGround_Changed[i].gameObject.SetActive(false);
            else arrSprrdBackGround_Changed[i].sprite = sprChangedImage;
        }
    }

    public async UniTaskVoid TaskChangedBackGround(bool _isToggle)
    {
        float startValue = _isToggle ? 0.0f : 11.0f;
        float endValue = _isToggle ? 11.0f : 0.0f;

        float duration = 1.0f;
        while(duration > 0.0f)
        {
            duration -= Time.deltaTime;

            for (int i = 0; i < arrSprrdBackGround_Changed.Length; i++)
            {
                if (!arrSprrdBackGround_Changed[i].gameObject.activeSelf)
                    continue;

                mpbFade = new MaterialPropertyBlock();
                arrSprrdBackGround_Changed[i].GetPropertyBlock(mpbFade);
                mpbFade.SetFloat("_SourceAlphaDissolveFade", Mathf.Lerp(startValue, endValue, 1.0f - duration));
                arrSprrdBackGround_Changed[i].SetPropertyBlock(mpbFade);
            }

            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }
    }

    public void CalculateBackGround(float _cameraX)
    {
        float widthSpace = isSpriteWidthFilled ? arrSprrdBackGround[0].bounds.size.x + spriteWidthSpace : 49.1f;
        arrTfBackGround[disabledBackGroundIndex].position = arrTfBackGround[currBackGroundIndex].position + widthSpace * (arrTfBackGround[currBackGroundIndex].position.x < _cameraX ? Vector3.right : Vector3.left);
        
        float currBgDist = arrTfBackGround[currBackGroundIndex].position.x - _cameraX;
        currBgDist = currBgDist < 0.0f ? -currBgDist : currBgDist;
        if (currBgDist > widthSpace)
            (currBackGroundIndex, disabledBackGroundIndex) = (disabledBackGroundIndex, currBackGroundIndex);
    }
}
