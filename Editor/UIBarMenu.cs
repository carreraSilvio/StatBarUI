using UnityEditor;
using UnityEngine;

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
    [MenuItem("GameObject/UI/Bar", false, 70)]
    static void CreateCustomGameObject(MenuCommand menuCommand)
    {
        // Create a custom game object
        GameObject go = new GameObject("Bar");
        go.AddComponent<UIBar>();
        go.AddComponent<RectTransform>();
        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
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