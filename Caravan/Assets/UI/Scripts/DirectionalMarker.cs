using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DirectionalMarker : MonoBehaviour
{
    public GameObject marker;
    public TMP_Text distanceText;
    public Color markerColor;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        marker.GetComponent<Image>().color = markerColor;
        distanceText.color = markerColor;
        GameObject.FindObjectOfType<DirectionalMarkerManager>().markers.Add(this);
        marker.transform.SetParent(GameObject.FindObjectOfType<DirectionalMarkerManager>().transform);
    }

    public void Destroy()
    {
        GameObject.FindObjectOfType<DirectionalMarkerManager>().markers.Remove(this);
        Destroy(marker);
        Destroy(gameObject);
    }
}
