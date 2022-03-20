using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BrushSize {
    Small,
    Medium,
    Large
}

public class StyleReporter : Detector
{
    Color _color = Color.white;

    BrushSize _brushSize = BrushSize.Medium;

    protected bool _didChange = false;
    protected bool _shouldChange = false;
    protected int _lastUpdateFrame = -1;

    public Color Color
    {
        get
        {
            ensureUpToDate();
            return _color;
        }
        set
        {
            _color = value;
            _shouldChange = true;
        }
    }

    public BrushSize BrushSize
    {
        get
        {
            ensureUpToDate();
            return _brushSize;
        }
        set
        {
            _brushSize = value;
            _shouldChange = true;
        }
    }

    public virtual bool StyleChanged
    {
        get
        {
            ensureUpToDate();
            return _didChange;
        }
    }

    void ensureUpToDate()
    {
        if (Time.frameCount == _lastUpdateFrame)
        {
            return;
        }

        _lastUpdateFrame = Time.frameCount;

        _didChange = false;

        if (IsActive)
        {
            changeState(false);
            _shouldChange = false;
        }
        else
        {
            if (_shouldChange)
            {
                changeState(true);
                _shouldChange = false;
            }
        }
    }

    protected virtual void changeState(bool shouldBeActive)
    {
        bool currentState = IsActive;
        if (shouldBeActive)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
        if (currentState != IsActive && currentState != true) // Short circuit logic - we only care when on-event fires, not off-event
        {
            _didChange = true;
        }
    }

}