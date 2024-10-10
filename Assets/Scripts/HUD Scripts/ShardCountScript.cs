﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShardCountScript : MonoBehaviour
{
    public RectTransform rectTransform;
    public RectTransform[] imageTransforms;
    public Text number;
    public Text gasNumber;
    public Text feNumber;
    public PlayerCore core;
    public static ShardCountScript instance;
    private bool stickySlide;

    void Start()
    {
        instance = this;
        instance.sizeDeltaY = rectTransform.sizeDelta.y;
        instance.stickySlide = false;
    }

    void FixedUpdate()
    {
        foreach (var imageTransform in imageTransforms)
            imageTransform.rotation = Quaternion.Euler(0, 0, Time.fixedTime * 100);
    }


    public static void DisplayCount()
    {
        var save = PlayerCore.Instance.cursave;
        DisplayCount(save.shards, save.gas, save.fusionEnergy);
    }
    private static void DisplayCount(int shardCount, float gasCount, int feCount)
    {
        UpdateNumber(shardCount, gasCount, feCount);
        instance.StopAllCoroutines();
        instance.StartCoroutine("SlideIn");
    }

    private static void UpdateNumber(int shardCount, float gasCount, int feCount)
    {
        instance.number.text = QuantityDisplayScript.GetValueString(shardCount);
        instance.gasNumber.text = QuantityDisplayScript.GetValueString(Mathf.RoundToInt(gasCount));
        instance.feNumber.text = QuantityDisplayScript.GetValueString(feCount);
    }
    float sizeDeltaY;

    /// sticky slides used when you want the player to see their shard count
    public static void StickySlideIn()
    {
        DisplayCount();
        instance.stickySlide = true;
        instance.StopAllCoroutines();
        if (!instance.gameObject.activeSelf) return;
        instance.StartCoroutine("SlideIn");
    }

    public static void StickySlideOut()
    {
        instance.stickySlide = false;
        instance.StopAllCoroutines();
        if (!instance.gameObject.activeSelf) return;
        instance.StartCoroutine("SlideOut");
    }

    IEnumerator SlideIn()
    {
        while (rectTransform.anchoredPosition.y > -sizeDeltaY)
        {
            var minint = Mathf.Min(3F, sizeDeltaY + rectTransform.anchoredPosition.y);
            rectTransform.anchoredPosition = rectTransform.anchoredPosition - new Vector2(0, minint);
            yield return null;
        }

        yield return new WaitForSeconds(3);
        if (!stickySlide)
        {
            instance.StartCoroutine(SlideOut());
        }
        yield return null;
    }

    IEnumerator SlideOut()
    {
        while (rectTransform.anchoredPosition.y < 0)
        {
            var minint = Mathf.Min(3, -rectTransform.anchoredPosition.y);
            rectTransform.anchoredPosition = rectTransform.anchoredPosition + new Vector2(0, minint);
            yield return null;
        }
    }
}
