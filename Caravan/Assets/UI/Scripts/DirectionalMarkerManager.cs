using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Sprites;
using UnityEngine;

public class DirectionalMarkerManager : MonoBehaviour
{
    public List<DirectionalMarker> markers = new List<DirectionalMarker>();

    public int Margin = 10;

    void Start()
    {

    }

    void Update()
    {
        Debug.Log(Screen.width + " x " + Screen.height);
        foreach(DirectionalMarker marker in markers)
        {
            Vector3 vec3 = Camera.main.WorldToScreenPoint(marker.transform.position);

            float maxW = Screen.width - Margin;
            float maxH = Screen.height - Margin;
            //Debug.Log(maxW + " , " + maxH);

            float newX;
            float newY;

            if(vec3.z >= 0f)         // Positive Z, means the marker is in front of camera.
            {
                // Return positive coordinates, lock marker within screen
                newX = Mathf.Min(maxW, Mathf.Max(vec3.x, Margin));
                newY = Mathf.Min(maxH, Mathf.Max(vec3.y, Margin));
            }
            else                     // Negative Z, means the marker is behind the camera.
            {
                // Return negative coordinates, lock marker onto the sides
                Vector2 norm = new Vector2(maxW - vec3.x, maxH - vec3.y).normalized;

                if (Mathf.Abs(norm.y) > Mathf.Abs(norm.x))   // Abs Y higher, point is closer to top/bottom sides of screen
                {
                    newX = (norm.x * maxW/2) + maxW/2;

                    if (norm.y > 0f) newY = maxH;
                    else newY = Margin;
                }
                else                                         // Abs X higher, point is closer to left/right sides of screen
                {
                    newY = (norm.y * maxH/2) + maxH/2;

                    if (norm.x > 0f) newX = maxW;
                    else newX = Margin;
                }
            }
            //Debug.Log(newX + " , " + newY);

            marker.marker.transform.position = new Vector2(newX, newY);
            float distance = Vector3.Distance(marker.transform.position, Camera.main.transform.position) / 10f;
            marker.distanceText.text = distance.ToString().Split('.')[0] + "m";
        }
    }
}
