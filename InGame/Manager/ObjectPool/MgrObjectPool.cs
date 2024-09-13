using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MgrObjectPool : Singleton<MgrObjectPool>
{
    [System.Serializable]
    public class GameObj
    {
        [HideInInspector] public GameObject objRootFolder; // 각 풀링 오브젝트 마다 묶어놓을 루트 폴더 오브젝트

        public GameObject objPool; // 풀링할 오브젝트 프리펩
        [HideInInspector] public string objectTag = null;
        [HideInInspector] public List<GameObject> listObjUnused = new List<GameObject>(); // 사용 중이 아닌 오브젝트 리스트
        [HideInInspector] public List<GameObject> listObjUsing = new List<GameObject>(); // 사용 중인 오브젝트 리스트

        private Vector3 v3InitScale = Vector3.zero;

        /// <summary>
        /// 오브젝트 풀링 체크
        /// </summary>
        /// <returns></returns>
        public GameObject GainObj(Vector3 _v3Pos)
        {
            CheckHasRootFolder();

            if (GainRemainUnusedObj() > 0) // 사용 중이 아닌 오브젝트가 존재할 경우
            {
                GameObject objTemp;
                if (!listObjUnused[0])
                {
                    objTemp = Instantiate(objPool, objRootFolder.transform); // 제일 앞에 있는 오브젝트를 재활용 (만약 NULL 일 경우 새로 생성)
                    objTemp.name = objectTag;
                }
                else objTemp = listObjUnused[0];
                objTemp.transform.localScale = v3InitScale;
                objTemp.transform.position = _v3Pos;
                objTemp.SetActive(true);

                listObjUnused.RemoveAt(0);
                listObjUsing.Add(objTemp);
                return objTemp;
            }
            else
            {
                GameObject objTemp = Instantiate(objPool, _v3Pos, objPool.transform.rotation, objRootFolder.transform); // 사용 가능한 풀링이 없으므로 새 인스턴스 생성
                if (v3InitScale.Equals(Vector3.zero))
                    v3InitScale = objTemp.transform.localScale;
                objTemp.name = objectTag;
                listObjUsing.Add(objTemp);
                return objTemp;
            }
        }

        /// <summary>
        /// 사용 중인 오브젝트를 미사용 오브젝트 리스트에 반환
        /// </summary>
        /// <param name="_obj">대상 오브젝트</param>
        public void ReturnUsingObj(GameObject _obj)
        {
            if (!_obj || !_obj.activeSelf)
                return;

            listObjUnused.Add(_obj);
            listObjUsing.Remove(_obj);
            _obj.SetActive(false);
            _obj.transform.SetParent(objRootFolder.transform);
        }

        public void ResetUsingList()
        {
            listObjUsing.Clear();
        }
        
        /// <summary>
        /// 비사용 중인 오브젝트 갯수 반환
        /// </summary>
        /// <returns></returns>
        public int GainRemainUnusedObj()
        {
            return listObjUnused.Count;
        }

        private void CheckHasRootFolder()
        {
            if (objRootFolder != null) // 루트 폴더가 없을 경우 폴더 먼저 생성
                return;

            objRootFolder = new GameObject(objectTag);
            objRootFolder.transform.SetParent(Instance.gameObject.transform);
        }
    }

    public Dictionary<string, int> objectPoolMap = new Dictionary<string, int>();
    public List<GameObj> listObjPool; // 풀링할 오브젝트 리스트

    private void Awake()
    {
        for (int i = 0; i < listObjPool.Count; i++)
        {
            if (!listObjPool[i].objPool)
                continue;

            listObjPool[i].objectTag = listObjPool[i].objPool.name;
            try
            {
                objectPoolMap.Add(listObjPool[i].objPool.name, i);
            }
            catch (System.ArgumentException)
            {//중복 프리팹 체크
                Debug.Log($"duplicate prefab : {listObjPool[i].objPool.name}");
            }
        }
    }

    /// <summary>
    /// 풀링 오브젝트 생성
    /// </summary>
    /// <param name="_name">프리팹 이름</param>
    /// <returns></returns>
    public GameObject ShowObj(string _name, Vector3 _v3Pos)
    {
        GameObj objTemp = GainFolderObj(_name);
        if (objTemp is null) return null;
        return objTemp.GainObj(_v3Pos);
    }

    /// <summary>
    /// 풀링 오브젝트 제거
    /// </summary>
    /// <param name="_name">프리팹 이름</param>
    /// <param name="_obj">제거할 오브젝트</param>
    public void HideObj(string _name, GameObject _obj)
    {
        GameObj objTemp = GainFolderObj(_name);
        if (objTemp is null) return;
        objTemp.ReturnUsingObj(_obj);
    }

    public void ResetParent(string _name, GameObject _obj)
    {
        GameObj objTemp = GainFolderObj(_name);
        if (objTemp is null) return;
        _obj.transform.SetParent(objTemp.objRootFolder.transform);
    }

    /// <summary>
    /// 루트 폴더 오브젝트 받아오기
    /// </summary>
    /// <param name="_name">프리팹 이름</param>
    /// <returns></returns>
    private GameObj GainFolderObj(string _name)
    {
        if (objectPoolMap.TryGetValue(_name, out var outIndex))
        {
            return listObjPool[outIndex];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 모든 풀링 오브젝트 반환
    /// </summary>
    public void HideAllPool()
    {
        for (int i = 0; i < listObjPool.Count; i++)
        {
            int objCnt = listObjPool[i].listObjUsing.Count - 1;
            for (int x = objCnt; x >= 0; x--)
            {
                HideObj(listObjPool[i].objectTag, listObjPool[i].listObjUsing[x]);
            }
            listObjPool[i].ResetUsingList();
        }
    }
}
