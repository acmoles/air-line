using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

[DefaultExecutionOrder(-1)]
public class InputManager : Singleton<InputManager>
{
    public delegate void StartTouchEvent(Vector3 position, float time);
    public event StartTouchEvent OnStartTouch;

    public delegate void EndTouchEvent(Vector3 position, float time);
    public event EndTouchEvent OnEndTouch;
    private TouchInput touchInput = null;

    private Camera mainCamera = null;

    private void Awake()
    {
        touchInput = new TouchInput();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (Application.isPlaying) touchInput.Enable();
        if (Application.isEditor) TouchSimulation.Enable();

        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += FingerDown;
    }

    private void OnDisable()
    {
        if (Application.isPlaying) touchInput.Disable();
        if (Application.isEditor) TouchSimulation.Disable();

        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= FingerDown;
    }

    private void Start()
    {
        touchInput.Touch.TouchPress.started += ctx => StartTouch(ctx);
        touchInput.Touch.TouchPress.canceled += ctx => EndTouch(ctx);
    }

    private void StartTouch(InputAction.CallbackContext ctx)
    {
        if (OnStartTouch != null) OnStartTouch(TouchUtils.ScreenToWorld(mainCamera, touchInput.Touch.TouchPosition.ReadValue<Vector2>()), (float)ctx.startTime);
    }

    private void EndTouch(InputAction.CallbackContext ctx)
    {
        //Debug.Log("Touch ended");
        if (OnEndTouch != null) OnEndTouch(TouchUtils.ScreenToWorld(mainCamera, touchInput.Touch.TouchPosition.ReadValue<Vector2>()), (float)ctx.time);
    }

    // Direct finger API
    private void FingerDown(Finger finger)
    {
        if (OnStartTouch != null) OnStartTouch(TouchUtils.ScreenToWorld(mainCamera, finger.screenPosition), Time.time);
    }

    public Vector3 PrimaryPosition()
    {
        return TouchUtils.ScreenToWorld(mainCamera, touchInput.Touch.TouchPosition.ReadValue<Vector2>());
    }

    public Vector3 PrimaryPosition2D()
    {
        return touchInput.Touch.TouchPosition.ReadValue<Vector2>();
    }
}

public static class TouchUtils
{
    public static Vector3 ScreenToWorld(Camera camera, Vector2 position)
    {
        Vector3 screenCoordinates = new Vector3(position.x, position.y, camera.nearClipPlane);
        return camera.ScreenToWorldPoint(screenCoordinates);
    }
}