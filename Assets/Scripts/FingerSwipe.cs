using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using HedgehogTeam.EasyTouch;

public class FingerSwipe : MonoBehaviour {

    //public GameObject trail;
    public Text swipeText;

    Vector3 StartPos;
    Vector3 previousPos;
    Vector3 offset;
    Vector3 finalOffset;
    Vector3 eulerAngle;

    // Subscribe to events
    void OnEnable()
    {
        EasyTouch.On_SwipeStart += On_SwipeStart;
        EasyTouch.On_Swipe += On_Swipe;
        EasyTouch.On_SwipeEnd += On_SwipeEnd;
    }

    void OnDisable()
    {
        UnsubscribeEvent();

    }

    void OnDestroy()
    {
        UnsubscribeEvent();
    }

    void UnsubscribeEvent()
    {
        EasyTouch.On_SwipeStart -= On_SwipeStart;
        EasyTouch.On_Swipe -= On_Swipe;
        EasyTouch.On_SwipeEnd -= On_SwipeEnd;
    }


    // At the swipe beginning 
    private void On_SwipeStart(Gesture gesture)
    {
        StartPos = Input.mousePosition;
        previousPos = Input.mousePosition;
        swipeText.text = "You start a swipe";
    }

    // During the swipe
    private void On_Swipe(Gesture gesture)
    {
        offset = Input.mousePosition - previousPos;
        previousPos = Input.mousePosition;
        transform.Rotate(Vector3.Cross(offset, Vector3.forward).normalized, offset.magnitude, Space.World);
        // the world coordinate from touch for z=5
        //Vector3 position = gesture.GetTouchToWorldPoint(5);
        //trail.transform.position = position;

    }

    // At the swipe end 
    private void On_SwipeEnd(Gesture gesture)
    {
        finalOffset = Input.mousePosition - StartPos;
        // Get the swipe angle
        float angles = gesture.GetSwipeOrDragAngle();
        swipeText.text = "Last swipe : " + gesture.swipe.ToString() + " /  vector : " + gesture.swipeVector.normalized + " / angle : " + angles.ToString("f2");
    }
}
