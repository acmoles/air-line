#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MovingTarget))]
public class MovingTargetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // get the chosen game object
        MovingTarget t = target as MovingTarget;

        if (GUILayout.Button("Place"))
        {
            t.OnPlace();
        }

        if (GUILayout.Button("Place Fake"))
        {
            t.OnPlaceFake();
        }

        if (GUILayout.Button("Play Fake"))
        {
            t.OnPlayFake();
        }

        GUILayout.Space(20);

        GUILayout.Label("Toggle brush up/down");

        if (GUILayout.Button(BrushUpDownState.Up.ToString()))
        {
            t.OnToggleBrushUpDown(BrushUpDownState.Up);
        }

        if (GUILayout.Button(BrushUpDownState.Down.ToString()))
        {
            t.OnToggleBrushUpDown(BrushUpDownState.Down);
        }

        GUILayout.Space(20);

        GUILayout.Label("Color");


        if (GUILayout.Button("Purple"))
        {
            t.OnColor(BrushColor.Purple);
        }

        if (GUILayout.Button("Blue"))
        {
            t.OnColor(BrushColor.Blue);
        }

        if (GUILayout.Button("Orange"))
        {
            t.OnColor(BrushColor.Orange);
        }

        if (GUILayout.Button("Green"))
        {
            t.OnColor(BrushColor.Green);
        }


        GUILayout.Space(20);

        GUILayout.Label("Size");

        if (GUILayout.Button("Large"))
        {
            t.OnSize(BrushSize.Large);
        }

        if (GUILayout.Button("Medium"))
        {
            t.OnSize(BrushSize.Medium);
        }

        if (GUILayout.Button("Small"))
        {
            t.OnSize(BrushSize.Small);
        }


        GUILayout.Space(20);

        GUILayout.Label("Toggle movement state");

        if (GUILayout.Button(TurtleMovementState.FollowMe.ToString()))
        {
            t.OnToggleMovementState(TurtleMovementState.FollowMe);
        }

        if (GUILayout.Button(TurtleMovementState.ExitFollowMe.ToString()))
        {
            t.OnToggleMovementState(TurtleMovementState.ExitFollowMe);
        }

        if (GUILayout.Button(TurtleMovementState.Play.ToString()))
        {
            t.OnToggleMovementState(TurtleMovementState.Play);
        }

        if (GUILayout.Button(TurtleMovementState.Pause.ToString()))
        {
            t.OnToggleMovementState(TurtleMovementState.Pause);
        }
    }
}

#endif