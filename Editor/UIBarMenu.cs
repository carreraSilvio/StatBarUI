﻿using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIBarMenu
{
    /// <summary>
    /// The root path for scrubber menu items.
    /// </summary>
    private const string MENU_ITEM_PATH = "Assets/Scrubber/";

    /// <summary>
    /// Menu items priority (so they will be grouped/shown next to existing scripting menu items).
    /// </summary>
    private const int MENU_ITEM_PRIORITY = 70;

    // Add a menu item to create custom GameObjects.
    // Priority 1 ensures it is grouped with the other menu items of the same kind
    // and propagated to the hierarchy dropdown and hierarchy context menus.
    [MenuItem("GameObject/UI/Game/StatBar", false, 70)]
    static void CreateCustomGameObject(MenuCommand menuCommand)
    {
        Canvas canvas;
        FindOrCreateCanvas();
        FindOrCreateEventSystem();
        CreateMenu();



        void FindOrCreateCanvas()
        {
            canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("Canvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
                GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            }
        }
        void FindOrCreateEventSystem()
        {
            var eventSystem = GameObject.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }
        void CreateMenu()
        {
            // Create a custom game object
            GameObject go = new GameObject("Bar");
            go.AddComponent<UIBar>();
            go.AddComponent<RectTransform>();
            //go.transform.SetParent(canvas.transform);
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }

    // Add a menu item called "Double Mass" to a Rigidbody's context menu.
    [MenuItem("CONTEXT/Rigidbody/Double Mass")]
    static void DoubleMass(MenuCommand command)
    {
        Rigidbody body = (Rigidbody)command.context;
        body.mass = body.mass * 2;
        Debug.Log("Doubled Rigidbody's Mass to " + body.mass + " from Context Menu.");
    }
}