using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visage.StatBarUI.Runtime;

namespace Visage.StatBarUI.Editor
{
    public sealed class StatBarMenu
    {
        private static int INITIAL_MAX_VALUE = 100;

        // Add a menu item to create custom GameObjects.
        // Priority 1 ensures it is grouped with the other menu items of the same kind
        // and propagated to the hierarchy dropdown and hierarchy context menus.
        [MenuItem("GameObject/UI/Visage/StatBar", false, 70)]
        static void CreateStatBar(MenuCommand menuCommand)
        {
            Canvas canvas;
            FindOrCreateCanvas();
            FindOrCreateEventSystem();

            GameObject statBarHolder;
            StatBar statBar;
            GameObject fillArea;
            GameObject fill;
            GameObject infoArea;
            GameObject valueLabel;

            CreateBar();
            CreateBackground();
            CreateFillArea();
            CreateFill();
            CreateInfoArea();
            CreateNameLabel();
            CreateValueLabel();

            statBar.FillRect = fill.GetComponent<RectTransform>();
            statBar.ValueLabel = valueLabel.GetComponent<RectTransform>();
            statBar.Value = statBar.MaxValue;


            #region Locals
            void FindOrCreateCanvas()
            {
                canvas = Object.FindObjectOfType<Canvas>();
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
                var eventSystem = Object.FindObjectOfType<EventSystem>();
                if (eventSystem == null)
                {
                    var go = new GameObject("EventSystem");
                    go.AddComponent<EventSystem>();
                    go.AddComponent<StandaloneInputModule>();
                }
            }
            void CreateBar()
            {
                // Create and set the statBar size
                statBarHolder = new GameObject("StatBar");
                statBar = statBarHolder.AddComponent<StatBar>();
                statBar.MaxValue = INITIAL_MAX_VALUE;
                statBar.WholeNumbers = true;
                var statBarRect = statBarHolder.GetComponent<RectTransform>();
                statBarRect.sizeDelta = new Vector2(160, 20);

                var targetParent = Selection.activeGameObject?? canvas.gameObject;
                // Ensure it gets reparented if this was a context click (otherwise does nothing)
                GameObjectUtility.SetParentAndAlign(statBarHolder, targetParent);

                // Register the creation in the undo system
                Undo.RegisterCreatedObjectUndo(statBarHolder, "Create " + statBarHolder.name);
                Selection.activeObject = statBarHolder;
            }
            void CreateBackground()
            {
                var bg = new GameObject("Background");
                var img = bg.AddComponent<Image>();
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                img.type = Image.Type.Sliced;
                img.SetNativeSize();

                GameObjectUtility.SetParentAndAlign(bg, statBarHolder);
                Stretch(bg, true, true);
            }
            void CreateFillArea()
            {
                fillArea = new GameObject("FillArea");
                fillArea.AddComponent<RectTransform>();

                GameObjectUtility.SetParentAndAlign(fillArea, statBarHolder);
                Stretch(fillArea, true, true);
            }
            void CreateFill()
            {
                fill = new GameObject("Fill");
                fill.AddComponent<RectTransform>();
                var img = fill.AddComponent<Image>();
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
                img.color = Color.red;
                img.SetNativeSize();

                GameObjectUtility.SetParentAndAlign(fill, fillArea.gameObject);
                Stretch(fill, true, true);
            }
            void CreateInfoArea()
            {
                infoArea = new GameObject("InfoArea");
                var rect = infoArea.AddComponent<RectTransform>();

                GameObjectUtility.SetParentAndAlign(infoArea, statBarHolder);
                MatchParentSize(infoArea, true, true);

                //Align top-left
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);

                var pos = rect.anchoredPosition;
                pos.y = 20;
                rect.anchoredPosition = pos;
            }
            void CreateNameLabel()
            {
                var nameLabel = new GameObject("NameLabel");
                nameLabel.AddComponent<RectTransform>();
                var nameText = nameLabel.AddComponent<Text>();
                nameText.text = "HP";
                nameText.alignment = TextAnchor.MiddleLeft;

                GameObjectUtility.SetParentAndAlign(nameLabel, infoArea.gameObject);
                Stretch(nameLabel, true, true);
                ApplyOffset(nameLabel, 2f);
            }
            void CreateValueLabel()
            {
                valueLabel = new GameObject("ValueLabel");
                valueLabel.AddComponent<RectTransform>();
                var nameText = valueLabel.AddComponent<Text>();
                nameText.text = statBar.MaxValue.ToString(statBar.LeadingZeroesString);
                nameText.alignment = TextAnchor.MiddleLeft;

                GameObjectUtility.SetParentAndAlign(valueLabel, infoArea.gameObject);
                Stretch(valueLabel, true, true);
                ApplyOffset(valueLabel, 25f);
            }
            #endregion
        }

        /// <summary>
        /// Match child width or height with its parent's.
        /// </summary>
        /// <remarks>It won't stretch. For that, see <see cref="Stretch"/></remarks>
        private static void MatchParentSize(GameObject child, bool width, bool height)
        {
            var parentRect = child.transform.parent.GetComponent<RectTransform>();
            var childRect = child.GetComponent<RectTransform>();

            childRect.sizeDelta = new Vector2(
                width ? parentRect.sizeDelta.x : childRect.sizeDelta.x,
                height ? parentRect.sizeDelta.y : childRect.sizeDelta.y);
        }

        /// <summary>
        /// Make the child stretch to match the parent size
        /// </summary>
        private static void Stretch(GameObject child, bool horizontal, bool vertical)
        {
            (float min, float max) streteched = (0f, 1f);
            (float min, float max) nonStreteched = (0.5f, 0.5f);

            //Make the child extend horizontal/vertical and centralize
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = new Vector2(
                horizontal ? streteched.min : nonStreteched.min,
                vertical ? streteched.min : nonStreteched.min
                );
            childRect.anchorMax = new Vector2(
                horizontal ? streteched.max : nonStreteched.max,
                vertical ? streteched.max : nonStreteched.max
                );
            childRect.pivot = new Vector2(0.5f, 0.5f);

            //Adjust left and/or bot values
            childRect.offsetMin = new Vector2(
                horizontal ? 0f : childRect.offsetMin.x, //Left
                vertical ? 0f : childRect.offsetMin.y    //Bot
                );

            //Adjust right and/or top values
            childRect.offsetMax = new Vector2(
                horizontal ? 0f : childRect.offsetMax.x, //Right
                vertical ? 0f : childRect.offsetMax.y    //Top
                );

            //Left rectTransform.offsetMin.x;
            //Right rectTransform.offsetMax.x;
            //Top rectTransform.offsetMax.y;
            //Bottom rectTransform.offsetMin.y;
        }

        private static void ApplyOffset(GameObject child, float left = 0f, float bot = 0f, float right = 0f, float top = 0f)
        {
            var childRect = child.GetComponent<RectTransform>();

            var offsetMin = childRect.offsetMin;
            var offsetMax = childRect.offsetMax;

            offsetMin.x += left; //Left
            offsetMin.y += bot; //Bot
            offsetMax.x += right; //Right
            offsetMax.y += top; //Top

            childRect.offsetMin = offsetMin;
            childRect.offsetMax = offsetMax;
        }

    }
}