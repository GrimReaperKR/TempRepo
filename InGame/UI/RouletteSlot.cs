using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RouletteSlot : MonoBehaviour
{
    [field: SerializeField] public Image ImgSlot { get; private set; }
    [SerializeField] private RectTransform rtRootIcon;
    [SerializeField] private ParticleSystem parsysStop;
    [SerializeField] private GameObject objPrefabIcon;

    [SerializeField] private Sprite[] sprRoll;

    [SerializeField] private AnimationCurve curveSlot;

    private bool isRoll = false;
    private int rollIndex = 0;

    public void SetRoll()
    {
        isRoll = true;
        rtRootIcon.anchoredPosition = Vector3.zero;

        ImgSlot.rectTransform.sizeDelta = new Vector2(184.0f, 440.0f);

        parsysStop.Clear();
        parsysStop.Stop();

        TaskRoll().Forget();
    }

    private async UniTaskVoid TaskRoll()
    {
        while(isRoll)
        {
            ImgSlot.sprite = sprRoll[rollIndex];
            rollIndex++;
            if (rollIndex >= sprRoll.Length)
                rollIndex = 0;

            await UniTask.Delay(25, true, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }

    private List<Sprite> listSprite = new List<Sprite>();
    private int totalPassCnt;
    public void SetResultIcon(List<BoosterData.BoosterInfo> _listInfo)
    {
        listSprite.Clear();
        for (int i = 0; i < _listInfo.Count; i++)
            listSprite.Add(_listInfo[i].Icon);

        totalPassCnt = Random.Range(10, 16);

        if(rtRootIcon.childCount < totalPassCnt)
        {
            int addChildCnt = totalPassCnt - rtRootIcon.childCount;
            while(addChildCnt > 0)
            {
                Instantiate(objPrefabIcon, rtRootIcon.transform);
                addChildCnt--;
            }
        }

        Image imgRouletteIcon;
        for (int i = 0; i < rtRootIcon.childCount; i++)
        {
            rtRootIcon.GetChild(i).gameObject.SetActive(i < totalPassCnt);
            if(i < totalPassCnt)
            {
                imgRouletteIcon = rtRootIcon.GetChild(i).GetComponent<Image>();
                imgRouletteIcon.rectTransform.sizeDelta = new Vector2(175.0f, 175.0f);
                imgRouletteIcon.rectTransform.anchoredPosition = new Vector2(0.0f, i * -300.0f);
                imgRouletteIcon.sprite = listSprite[i % listSprite.Count];
            }
        }
        rtRootIcon.anchoredPosition = new Vector2(0.0f, (totalPassCnt - 1) * 300.0f);

        isRoll = false;

        rtRootIcon.DOKill();
        rtRootIcon.DOAnchorPosY(0.0f, 0.75f).SetEase(curveSlot).SetUpdate(true).OnComplete(() => parsysStop.Play());
    }
}
