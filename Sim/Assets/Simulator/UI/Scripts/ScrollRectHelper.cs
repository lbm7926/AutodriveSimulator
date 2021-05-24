using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class ScrollRectHelper : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{

    public float smooting = 5;                          //滑动速度
    public List<GameObject> listItem;                   //scrollview item 
    public int pageCount = 3;                           //每页显示的项目

    ScrollRect srect;
    float pageIndex;                                    //总页数
    bool isDrag = false;                                //是否拖拽结束
    List<float> listPageValue = new List<float> { 0 };  //总页数索引比列 0-1
    float targetPos = 0;                                //滑动的目标位置
    float nowindex = 0;                                 //当前位置索引

    public GameObject tempGO;

    void Awake()
    {
        srect = GetComponent<ScrollRect>();
        ListPageValueInit();

        Debug.Log(10/3);
        Debug.Log(10%3);
    }

    //每页比例
    void ListPageValueInit()
    {
        if (listItem.Count%pageCount==0)
        {
            pageIndex = (listItem.Count / pageCount) - 1;
            if (listItem != null && listItem.Count != 0)
            {
                for (float i = 1; i <= pageIndex; i++)
                {
                    listPageValue.Add((i / pageIndex));
                }
            }
        }
        else
        {
            int temp = listItem.Count % pageCount;
            for (int i = 0; i < temp; i++)
            {
                listItem.Add(Instantiate(tempGO));
            }

            pageIndex = (listItem.Count / pageCount) - 1;
            if (listItem != null && listItem.Count != 0)
            {
                for (float i = 1; i <= pageIndex; i++)
                {
                    listPageValue.Add((i / pageIndex));
                }
            }
        }        
    }

    void Update()
    {
        if (!isDrag)
            srect.horizontalNormalizedPosition = Mathf.Lerp(srect.horizontalNormalizedPosition, targetPos, Time.deltaTime * smooting);
    }
    /// <summary>
    /// 拖动开始
    /// </summary>
    /// <param name="eventData"></param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDrag = true;
    }
    /// <summary>
    /// 拖拽结束
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDrag = false;
        var tempPos = srect.horizontalNormalizedPosition; //获取拖动的值
        var index = 0;
        float offset = Mathf.Abs(listPageValue[index] - tempPos);    //拖动的绝对值
        for (int i = 1; i < listPageValue.Count; i++)
        {
            float temp = Mathf.Abs(tempPos - listPageValue[i]);
            if (temp < offset)
            {
                index = i;
                offset = temp;
            }
        }
        targetPos = listPageValue[index];
        nowindex = index;
    }

    public void BtnLeftGo()
    {
        nowindex = Mathf.Clamp(nowindex - 1, 0, pageIndex);
        targetPos = listPageValue[Convert.ToInt32(nowindex)];
    }

    public void BtnRightGo()
    {
        nowindex = Mathf.Clamp(nowindex + 1, 0, pageIndex);
        targetPos = listPageValue[Convert.ToInt32(nowindex)];

    }
}
