using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class testscroll : MonoBehaviour
{
    [SerializeField]
    private List<Text> _numbers;
    [SerializeField]
    private List<Text> _unactiveNumbers;
    /// <summary>
    /// 动画时长
    /// </summary>
    [SerializeField]
    private float _duration = 1.5f;
    /// <summary>
    /// 数字每次滚动时长
    /// </summary>
    [SerializeField]
    private float _rollingDuration = 0.05f;
    /// <summary>
    /// 数字每次变动数值
    /// </summary>
    private int _speed;
    /// <summary>
    /// 滚动延迟（每进一位增加一倍延迟，让滚动看起来更随机自然）
    /// </summary>
    [SerializeField]
    private float _delay = 0.008f;
    /// <summary>
    /// Text文字宽高
    /// </summary>
    private Vector2 _numberSize;
    /// <summary>
    /// 当前数字
    /// </summary>
    private int _curNumber;
    /// <summary>
    /// 起始数字
    /// </summary>
    private int _fromNumber;
    /// <summary>
    /// 最终数字
    /// </summary>
    private int _toNumber;
    /// <summary>
    /// 各位数字的缓动实例
    /// </summary>
    private List<ScrollTask> _lstScrollTask= new List<ScrollTask>();
    private List<ScrollTask> _lstScrollTaskCache =new List<ScrollTask>();
    /// <summary>
    /// 是否处于数字滚动中
    /// </summary>
    private bool _isJumping;
    /// <summary>
    /// 滚动完毕回调
    /// </summary>
    public Action OnComplete;
    private void Awake()
    {
        if (_numbers.Count == 0 || _unactiveNumbers.Count == 0)
        {
            Debug.LogError("[JumpingNumberTextComponent] 还未设置Text组件!");
            return;
        }
        _numberSize = _numbers[0].rectTransform.sizeDelta;
        for (int i=0;i<_numbers.Count*2;i++)
            _lstScrollTaskCache.Add(new ScrollTask());
    }
    public float duration
    {
        get { return _duration; }
        set
        {
            _duration = value;
        }
    }
    private float _different;
    public float different
    {
        get { return _different; }
    }
    public void Change(int from, int to)
    {
        bool isRepeatCall = _isJumping && _fromNumber == from && _toNumber == to;
        if (isRepeatCall) return;
        bool isContinuousChange = (_toNumber == from) && ((to - from > 0 && _different > 0) || (to - from < 0 && _different < 0));
        if (_isJumping && isContinuousChange)
        {
        }
        else
        {
            _fromNumber = from;
            _curNumber = _fromNumber;
        }
        _toNumber = to;
        _different = _toNumber - _fromNumber;
        _speed = (int)Math.Ceiling(_different / (_duration * (1 / _rollingDuration)));
        _speed = _speed == 0 ? (_different > 0 ? 1 : -1) : _speed;
        SetNumber(_curNumber, false);
        _isJumping = true;
        StopCoroutine("DoJumpNumber");
        StartCoroutine("DoJumpNumber");
    }
    public int number
    {
        get { return _toNumber; }
        set
        {
            if (_toNumber == value) return;
            Change(_curNumber, _toNumber);
        }
    }
    IEnumerator DoJumpNumber()
    {
        while (true)
        {
            if (_speed > 0)//增加
            {
                _curNumber = Math.Min(_curNumber + _speed, _toNumber);
            }
            else if (_speed < 0) //减少
            {
                _curNumber = Math.Max(_curNumber + _speed, _toNumber);
            }
            SetNumber(_curNumber, true);
            if (_curNumber == _toNumber)
            {
                StopCoroutine("DoJumpNumber");
                _isJumping = false;
                if (OnComplete != null) OnComplete();
                yield return null;
            }
            yield return new WaitForSeconds(_rollingDuration);
        }
    }
    /// <summary>
    /// 设置数字
    /// </summary>
    /// <param name="v"></param>
    /// <param name="isTween"></param>
    public void SetNumber(int v, bool isTween)
    {
        char[] c = v.ToString().ToCharArray();
        Array.Reverse(c);
        string s = new string(c);
        if (!isTween)
        {
            for (int i = 0; i < _numbers.Count; i++)
            {
                int textIndex=_numbers.Count-i-1;
                if (i < s.Count())
                    _numbers[textIndex].text = s[i] + "";
                else
                    _numbers[textIndex].text = "";
            }
        }
        else
        {
            _lstScrollTask.Clear();
            for (int i = 0; i < _numbers.Count; i++)
            {
                int textIndex=_numbers.Count-i-1;
                if (i < s.Count())
                {
                    _unactiveNumbers[textIndex].text = s[i] + "";
                }
                else
                {
                    _unactiveNumbers[textIndex].text = "";
                }
                //每次unactive转到active位置。
                _unactiveNumbers[textIndex].rectTransform.anchoredPosition = new Vector2(_unactiveNumbers[textIndex].rectTransform.anchoredPosition.x, (_speed > 0 ? -1 : 1) * _numberSize.y);
                _numbers[textIndex].rectTransform.anchoredPosition = new Vector2(_unactiveNumbers[textIndex].rectTransform.anchoredPosition.x, 0);
                if (_unactiveNumbers[textIndex].text != _numbers[textIndex].text)
                {
                    _lstScrollTaskCache[i*2].SetTask(_numbers[textIndex], (_speed > 0 ? 1 : -1) * _numberSize.y, _delay * i);
                    _lstScrollTaskCache[i*2+1].SetTask(_unactiveNumbers[textIndex], 0, _delay * i);
                    _lstScrollTask.Add(_lstScrollTaskCache[i*2]);
                    _lstScrollTask.Add(_lstScrollTaskCache[i*2+1]);
                    Text tmp = _numbers[textIndex];
                    _numbers[textIndex] = _unactiveNumbers[textIndex];
                    _unactiveNumbers[textIndex] = tmp;
                }
            }
        }
    }

    private void Update(){
        foreach(var task in _lstScrollTask){
            task.textObj.rectTransform.anchoredPosition=new Vector2(
                task.textObj.rectTransform.anchoredPosition.x,
                Mathf.Lerp(task.startYPos,task.endYPos,(Time.time-task.starttime)/task.time)
            );
        }
    }

    public void SetValue(int num){
        Change(_curNumber,num);
    }
    public void TestChange()
    {
        int num=UnityEngine.Random.Range(1, 999);
        Debug.Log(num);
        SetValue(num);
    }
}
public class ScrollTask{
    public Text textObj;
    public float endYPos;
    public float time;
    public float starttime;
    public float startYPos;
    public ScrollTask(){

    }
    public void SetTask(Text obj,float value,float delay){
        textObj=obj;
        endYPos=value;
        time=delay;
        starttime=Time.time;
        startYPos = textObj.rectTransform.anchoredPosition.y;
    }
}
