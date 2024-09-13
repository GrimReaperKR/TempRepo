using UnityEngine;
using Cysharp.Threading.Tasks;

public class BakeCompleteTimer : MonoBehaviour
{
    public float TimePassedAfterCompletion { get; private set; } // 완성되고 얼마나 지났는가
    public int PenaltyScore { get; private set; }
    private bool isEnable; // 시간 체크 활성화 여부 변수

    public void SetComplete(bool _isEnable, int _penalty = 0)
    {
        isEnable = _isEnable;
        if (isEnable)
        {
            PenaltyScore = _penalty;
            TimePassedAfterCompletion = 0.0f;
            TaskUpdate().Forget();
        }
    }

    private async UniTaskVoid TaskUpdate()
    {
        while(isEnable)
        {
            await UniTask.Yield();
            TimePassedAfterCompletion += Time.deltaTime;
        }
    }
}
