using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeathBehaviour : MonoBehaviour
{
    private int currentHitPoints, maxHitPoints;

    public int CurrentHitPoints { get { return currentHitPoints; } set { UpdateCurrentHearts(value); currentHitPoints = value; } }
    public int MaxHitPoints { get { return maxHitPoints; } set { UpdateMaxHearts(value); maxHitPoints = value; } }

    public Sprite fullHeart, emptyHeart;
    public GameObject heartPrefab;
    public GameObject canvas;
    public Vector2 heartOffset, heartPadding, heartSize;

    private List<Image> heartImages = new List<Image>();

    void UpdateMaxHearts(int newMaxHearts)
    {
        if(newMaxHearts < maxHitPoints) 
        { 
            heartImages.RemoveRange(newMaxHearts, maxHitPoints - newMaxHearts); 
        }

        if(newMaxHearts > maxHitPoints)
        {
            while(newMaxHearts > heartImages.Count)
            {
                GameObject newHeart = Instantiate(heartPrefab);

                newHeart.transform.SetParent(canvas.transform);

                Vector3 heartPosition = (heartImages.Count) * new Vector3(heartSize.x + heartPadding.x, 0) + (Vector3)heartOffset;
                RectTransform heartTransform = newHeart.GetComponent<RectTransform>();
                heartTransform.anchoredPosition = heartPosition;

                heartImages.Add(newHeart.GetComponent<Image>());
            }
        }
    }

    void UpdateCurrentHearts(int fullHearts)
    {
        for(int i = 0; i < maxHitPoints; i++)
        {
            if(i < fullHearts)
            {
                heartImages[i].sprite = fullHeart;
            }
            else
            {
                heartImages[i].sprite = emptyHeart;
            }
        }
    }

}
