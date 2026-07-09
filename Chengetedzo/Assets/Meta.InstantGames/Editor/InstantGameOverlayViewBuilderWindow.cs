// Copyright (c) Facebook, Inc. and its affiliates. All rights reserved.
//
// The examples provided by Facebook are for non-commercial testing and evaluation
// purposes only. Facebook reserves all rights not expressly granted.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// FACEBOOK BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEditor;

public class InstantGameOverlayViewBuilderWindow : EditorWindow
{
    private enum TagType { View, Text, Image, Button, For, If, ElseIf, Else, Condition, ConditionGroup }
    private enum ConditionOperator { EQUALS, NOT_EQUALS, GREATER_THAN, NOT_GREATER_THAN, LESS_THAN, NOT_LESS_THAN, IN, NOT_IN }
    private enum GroupOperator { AND, OR }
    private enum SortOrder { ASC, DESC }
    private enum ScrollStyle { top, bottom, center }

    [Serializable]
    private class OverlayTag
    {
        public int id;
        public TagType type;
        public string className = "", style = "", content = "", onTapEvent = "", src = "", action = "";
        public string source = "", itemName = "", sortKey = "", limit = "", startIndex = "", scrollIndex = "";
        public SortOrder order = SortOrder.ASC;
        public ScrollStyle scrollStyle = ScrollStyle.top;
        public string lhs = "", rhs = "";
        public ConditionOperator conditionOperator = ConditionOperator.EQUALS;
        public GroupOperator groupOperator = GroupOperator.AND;
        public List<OverlayTag> children = new List<OverlayTag>();
        public bool isExpanded = true;
        public OverlayTag parent;
        private static int nextId = 1;
        public OverlayTag(TagType type) { this.id = nextId++; this.type = type; }
        public int depth { get { int d = 0; var p = parent; while (p != null) { d++; p = p.parent; } return d; } }
        public bool CanHaveChildren() => type == TagType.View || type == TagType.For || type == TagType.If || type == TagType.ElseIf || type == TagType.Else || type == TagType.ConditionGroup;
    }

    private static class T
    {
        public const string ClassName = "CSS class from styles.css", Style = "Inline CSS", Content = "Text content";
        public const string ViewTag = "<View> → <div>", OnTapEvent = "Event on tap/click";
        public const string TextTag = "<Text> → <p>", ButtonTag = "<Button> → <button>", Action = "Action on press";
        public const string ImageTag = "<Image> → <img>", Src = "Image path or FBInstant key";
        public const string ForTag = "Loop over data", Source = "Data source", ItemName = "Item variable";
        public const string SortKey = "Sort key", Order = "ASC/DESC", Limit = "Max items", StartIndex = "Start index";
        public const string ScrollStyleTip = "Scroll position", ScrollIndex = "Center index";
        public const string IfTag = "Conditional render", ElseIfTag = "Else-if branch", ElseTag = "Else fallback";
        public const string ConditionTag = "Condition for If", Lhs = "Left side", Operator = "Operator", Rhs = "Right side";
        public const string ConditionGroupTag = "AND/OR group", GroupOp = "AND/OR";
    }

    private List<OverlayTag> rootTags = new List<OverlayTag>();
    private OverlayTag selectedTag, draggedTag, dropTargetTag;
    private OverlayTag lastSelectedTag;
    private Vector2 paletteScrollPos, hierarchyScrollPos, detailScrollPos, outputScrollPos;
    private string generatedXml = "";
    private bool isDragging;
    private int dropPosition;
    private GUIStyle tagButtonStyle, headerStyle, hierarchyLabelStyle;
    private const float ROW_HEIGHT = 22f, INDENT_WIDTH = 18f, FOLDOUT_WIDTH = 14f, ICON_WIDTH = 16f;

    // Resizable panel sizes
    private float paletteWidth = 130f;
    private float rightPanelWidth = 380f;
    private float detailHeight = 320f;
    private bool resizingPalette, resizingRightPanel, resizingDetail;
    private const float MIN_PANEL_WIDTH = 100f, MIN_DETAIL_HEIGHT = 150f, RESIZE_HANDLE = 5f;

    [MenuItem("Window/Instant Games/Overlay View Builder", priority = 3)]
    public static void ShowWindow() => GetWindow<InstantGameOverlayViewBuilderWindow>("Overlay View Builder").minSize = new Vector2(950, 650);

    private void InitStyles()
    {
        tagButtonStyle ??= new GUIStyle(GUI.skin.button) { padding = new RectOffset(6, 6, 4, 4), margin = new RectOffset(3, 3, 3, 3), fontStyle = FontStyle.Bold, fontSize = 10 };
        headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, padding = new RectOffset(5, 5, 5, 5) };
        hierarchyLabelStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
    }

    private void OnGUI()
    {
        InitStyles();
        HandleXmlFileDragAndDrop();
        HandleResizing();

        // Clear focus when selection changes
        if (selectedTag != lastSelectedTag)
        {
            lastSelectedTag = selectedTag;
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
        }

        EditorGUILayout.BeginHorizontal();
        DrawTagPalette();
        DrawVerticalResizeHandle(ref resizingPalette, true);
        DrawHierarchyPanel();
        DrawVerticalResizeHandle(ref resizingRightPanel, false);
        EditorGUILayout.BeginVertical(GUILayout.Width(rightPanelWidth));
        DrawDetailArea();
        DrawHorizontalResizeHandle();
        DrawOutputArea();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        HandleKeyboardInput();
        HandleDragEnd();
    }

    private void DrawVerticalResizeHandle(ref bool resizing, bool isLeft)
    {
        var rect = GUILayoutUtility.GetRect(RESIZE_HANDLE, position.height);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y, 1, rect.height), resizing ? new Color(0.4f, 0.6f, 1f) : new Color(0.5f, 0.5f, 0.5f, 0.3f));
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) { resizing = true; Event.current.Use(); }
    }

    private void DrawHorizontalResizeHandle()
    {
        var rect = GUILayoutUtility.GetRect(rightPanelWidth, RESIZE_HANDLE);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + 2, rect.width, 1), resizingDetail ? new Color(0.4f, 0.6f, 1f) : new Color(0.5f, 0.5f, 0.5f, 0.3f));
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) { resizingDetail = true; Event.current.Use(); }
    }

    private void HandleResizing()
    {
        var e = Event.current;
        if (e.type == EventType.MouseUp) { resizingPalette = resizingRightPanel = resizingDetail = false; }
        if (e.type == EventType.MouseDrag)
        {
            if (resizingPalette) { paletteWidth = Mathf.Clamp(e.mousePosition.x, MIN_PANEL_WIDTH, position.width * 0.3f); Repaint(); }
            if (resizingRightPanel) { rightPanelWidth = Mathf.Clamp(position.width - e.mousePosition.x, MIN_PANEL_WIDTH, position.width * 0.5f); Repaint(); }
            if (resizingDetail) { detailHeight = Mathf.Clamp(e.mousePosition.y - 20, MIN_DETAIL_HEIGHT, position.height - 150); Repaint(); }
        }
    }

    private void DrawTagPalette()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(paletteWidth));
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Tag Palette", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos);
        EditorGUILayout.LabelField("Layout", EditorStyles.miniBoldLabel);
        PBtn("View", TagType.View, T.ViewTag);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Content", EditorStyles.miniBoldLabel);
        PBtn("Text", TagType.Text, T.TextTag); PBtn("Image", TagType.Image, T.ImageTag); PBtn("Button", TagType.Button, T.ButtonTag);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Control Flow", EditorStyles.miniBoldLabel);
        PBtn("For", TagType.For, T.ForTag); PBtn("If", TagType.If, T.IfTag); PBtn("ElseIf", TagType.ElseIf, T.ElseIfTag); PBtn("Else", TagType.Else, T.ElseTag);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Conditions", EditorStyles.miniBoldLabel);
        PBtn("Condition", TagType.Condition, T.ConditionTag); PBtn("CondGrp", TagType.ConditionGroup, T.ConditionGroupTag);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void PBtn(string label, TagType type, string tip)
    {
        GUI.backgroundColor = GetTagColor(type);
        if (GUILayout.Button(new GUIContent(label, tip), tagButtonStyle, GUILayout.ExpandWidth(true))) AddTag(type);
        GUI.backgroundColor = Color.white;
    }

    private void AddTag(TagType type)
    {
        var tag = new OverlayTag(type);
        if (selectedTag != null && selectedTag.CanHaveChildren()) { tag.parent = selectedTag; selectedTag.children.Add(tag); selectedTag.isExpanded = true; }
        else rootTags.Add(tag);
        selectedTag = tag;
        UpdateXml();
    }

    private void DrawHierarchyPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Hierarchy", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24))) ShowAddMenu();
        EditorGUILayout.EndHorizontal();
        hierarchyScrollPos = EditorGUILayout.BeginScrollView(hierarchyScrollPos);
        if (rootTags.Count == 0) EditorGUILayout.HelpBox("Click tags from palette to add.\nDrag to reorder/nest.", MessageType.Info);
        else for (int i = 0; i < rootTags.Count; i++) DrawItem(rootTags[i], rootTags, i);
        EditorGUILayout.EndScrollView();
        var rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) { selectedTag = null; Repaint(); }
        EditorGUILayout.EndVertical();
    }

    private void ShowAddMenu()
    {
        var m = new GenericMenu();
        m.AddItem(new GUIContent("View"), false, () => AddTag(TagType.View));
        m.AddItem(new GUIContent("Text"), false, () => AddTag(TagType.Text));
        m.AddItem(new GUIContent("Image"), false, () => AddTag(TagType.Image));
        m.AddItem(new GUIContent("Button"), false, () => AddTag(TagType.Button));
        m.AddItem(new GUIContent("For"), false, () => AddTag(TagType.For));
        m.AddItem(new GUIContent("If"), false, () => AddTag(TagType.If));
        m.AddItem(new GUIContent("ElseIf"), false, () => AddTag(TagType.ElseIf));
        m.AddItem(new GUIContent("Else"), false, () => AddTag(TagType.Else));
        m.AddItem(new GUIContent("Condition"), false, () => AddTag(TagType.Condition));
        m.AddItem(new GUIContent("ConditionGroup"), false, () => AddTag(TagType.ConditionGroup));
        m.ShowAsContext();
    }

    private void DrawItem(OverlayTag tag, List<OverlayTag> list, int idx)
    {
        int depth = tag.depth;
        bool sel = tag == selectedTag, hasKids = tag.CanHaveChildren() && tag.children.Count > 0, canKids = tag.CanHaveChildren();
        Rect row = EditorGUILayout.GetControlRect(false, ROW_HEIGHT);
        float indent = depth * INDENT_WIDTH;
        for (int d = 0; d < depth; d++) EditorGUI.DrawRect(new Rect(row.x + d * INDENT_WIDTH + 8, row.y, 1, ROW_HEIGHT), new Color(0.5f, 0.5f, 0.5f, 0.3f));
        if (isDragging && dropTargetTag == tag)
        {
            var dc = new Color(0.2f, 0.6f, 1f, 0.8f);
            if (dropPosition == 0) EditorGUI.DrawRect(new Rect(row.x + indent, row.y, row.width - indent, 2), dc);
            else if (dropPosition == 1 && canKids) { EditorGUI.DrawRect(new Rect(row.x + indent, row.y, 2, ROW_HEIGHT), dc); EditorGUI.DrawRect(new Rect(row.x + indent, row.y, row.width - indent, 2), dc); EditorGUI.DrawRect(new Rect(row.x + indent, row.yMax - 2, row.width - indent, 2), dc); }
            else if (dropPosition == 2) EditorGUI.DrawRect(new Rect(row.x + indent, row.yMax - 2, row.width - indent, 2), dc);
        }
        var bg = new Rect(row.x + indent, row.y, row.width - indent, ROW_HEIGHT);
        if (sel) EditorGUI.DrawRect(bg, new Color(0.24f, 0.49f, 0.91f));
        else if (bg.Contains(Event.current.mousePosition) && !isDragging) EditorGUI.DrawRect(bg, new Color(0.3f, 0.3f, 0.3f, 0.3f));
        float x = indent;
        if (canKids && hasKids) { if (EditorGUI.Foldout(new Rect(row.x + x, row.y, FOLDOUT_WIDTH, ROW_HEIGHT), tag.isExpanded, GUIContent.none, true) != tag.isExpanded) tag.isExpanded = !tag.isExpanded; }
        x += FOLDOUT_WIDTH;
        EditorGUI.DrawRect(new Rect(row.x + x, row.y + 3, ICON_WIDTH, ICON_WIDTH), GetTagColor(tag.type));
        var iStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; iStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(row.x + x, row.y + 3, ICON_WIDTH, ICON_WIDTH), GetIcon(tag.type), iStyle);
        x += ICON_WIDTH + 4;
        var lStyle = new GUIStyle(hierarchyLabelStyle); if (sel) lStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(row.x + x, row.y, row.width - x - 65, ROW_HEIGHT), new GUIContent(GetName(tag), GetTip(tag.type)), lStyle);
        float bx = row.xMax - 65;
        if (GUI.Button(new Rect(bx, row.y + 1, 20, ROW_HEIGHT - 2), "▲", EditorStyles.miniButtonLeft) && idx > 0) { list.RemoveAt(idx); list.Insert(idx - 1, tag); UpdateXml(); }
        if (GUI.Button(new Rect(bx + 20, row.y + 1, 20, ROW_HEIGHT - 2), "▼", EditorStyles.miniButtonMid) && idx < list.Count - 1) { list.RemoveAt(idx); list.Insert(idx + 1, tag); UpdateXml(); }
        if (GUI.Button(new Rect(bx + 40, row.y + 1, 25, ROW_HEIGHT - 2), "✕", EditorStyles.miniButtonRight)) { list.RemoveAt(idx); if (selectedTag == tag) selectedTag = null; UpdateXml(); GUIUtility.ExitGUI(); }
        var e = Event.current;
        var iRect = new Rect(row.x + indent, row.y, row.width - indent - 65, ROW_HEIGHT);
        if (e.type == EventType.MouseDown && iRect.Contains(e.mousePosition) && e.button == 0) { selectedTag = tag; draggedTag = tag; GUI.FocusControl(null); e.Use(); Repaint(); }
        if (e.type == EventType.MouseDrag && draggedTag != null && !isDragging) { isDragging = true; e.Use(); }
        if (isDragging && draggedTag != tag && iRect.Contains(e.mousePosition)) { dropTargetTag = tag; float ry = e.mousePosition.y - row.y; dropPosition = ry < ROW_HEIGHT * 0.25f ? 0 : ry > ROW_HEIGHT * 0.75f ? 2 : canKids ? 1 : 2; Repaint(); }
        if (e.type == EventType.ContextClick && iRect.Contains(e.mousePosition)) { selectedTag = tag; ShowCtxMenu(tag, list, idx); e.Use(); }
        if (tag.CanHaveChildren() && tag.isExpanded) for (int i = 0; i < tag.children.Count; i++) DrawItem(tag.children[i], tag.children, i);
    }

    private string GetTip(TagType t) => t switch { TagType.View => T.ViewTag, TagType.Text => T.TextTag, TagType.Image => T.ImageTag, TagType.Button => T.ButtonTag, TagType.For => T.ForTag, TagType.If => T.IfTag, TagType.ElseIf => T.ElseIfTag, TagType.Else => T.ElseTag, TagType.Condition => T.ConditionTag, TagType.ConditionGroup => T.ConditionGroupTag, _ => "" };
    private string GetIcon(TagType t) => t switch { TagType.View => "V", TagType.Text => "T", TagType.Image => "I", TagType.Button => "B", TagType.For => "∀", TagType.If => "?", TagType.ElseIf => "⁇", TagType.Else => "!", TagType.Condition => "=", TagType.ConditionGroup => "&", _ => "•" };
    private Color GetTagColor(TagType t) => t switch { TagType.View => new Color(0.3f, 0.5f, 0.85f), TagType.Text => new Color(0.45f, 0.75f, 0.35f), TagType.Image => new Color(0.9f, 0.55f, 0.25f), TagType.Button => new Color(0.85f, 0.35f, 0.55f), TagType.For => new Color(0.35f, 0.7f, 0.7f), TagType.If => new Color(0.65f, 0.45f, 0.85f), TagType.ElseIf => new Color(0.55f, 0.35f, 0.75f), TagType.Else => new Color(0.5f, 0.3f, 0.7f), TagType.Condition => new Color(0.8f, 0.65f, 0.3f), TagType.ConditionGroup => new Color(0.7f, 0.55f, 0.2f), _ => Color.white };
    private string GetName(OverlayTag t) { var n = t.type.ToString(); if (!string.IsNullOrEmpty(t.className)) n += $" .{t.className}"; else if (t.type == TagType.For && !string.IsNullOrEmpty(t.source)) n += $" [{t.source}]"; else if (t.type == TagType.Condition && !string.IsNullOrEmpty(t.lhs)) n += $" ({t.lhs})"; else if (t.type == TagType.ConditionGroup) n += $" ({t.groupOperator})"; return n; }

    private void HandleDragEnd()
    {
        var e = Event.current;
        if (e.type == EventType.MouseUp && isDragging && draggedTag != null)
        {
            if (dropTargetTag != null && dropTargetTag != draggedTag && !IsDesc(dropTargetTag, draggedTag))
            {
                if (FindTag(draggedTag, out var srcList, out var srcIdx)) { srcList.RemoveAt(srcIdx); if (dropPosition == 0 && FindTag(dropTargetTag, out var tl, out var ti)) { draggedTag.parent = dropTargetTag.parent; tl.Insert(ti, draggedTag); } else if (dropPosition == 1 && dropTargetTag.CanHaveChildren()) { draggedTag.parent = dropTargetTag; dropTargetTag.children.Insert(0, draggedTag); dropTargetTag.isExpanded = true; } else if (dropPosition == 2 && FindTag(dropTargetTag, out var tl2, out var ti2)) { draggedTag.parent = dropTargetTag.parent; tl2.Insert(ti2 + 1, draggedTag); } UpdateXml(); }
            }
            isDragging = false; draggedTag = null; dropTargetTag = null; dropPosition = -1; e.Use(); Repaint();
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && isDragging) { isDragging = false; draggedTag = null; dropTargetTag = null; dropPosition = -1; e.Use(); Repaint(); }
    }

    private bool IsDesc(OverlayTag d, OverlayTag a) { var c = d.parent; while (c != null) { if (c == a) return true; c = c.parent; } return false; }

    private void ShowCtxMenu(OverlayTag tag, List<OverlayTag> list, int idx)
    {
        var m = new GenericMenu();
        if (tag.CanHaveChildren()) { m.AddItem(new GUIContent("Add Child/View"), false, () => AddChild(tag, TagType.View)); m.AddItem(new GUIContent("Add Child/Text"), false, () => AddChild(tag, TagType.Text)); m.AddItem(new GUIContent("Add Child/Image"), false, () => AddChild(tag, TagType.Image)); m.AddItem(new GUIContent("Add Child/Button"), false, () => AddChild(tag, TagType.Button)); m.AddItem(new GUIContent("Add Child/For"), false, () => AddChild(tag, TagType.For)); m.AddItem(new GUIContent("Add Child/If"), false, () => AddChild(tag, TagType.If)); m.AddItem(new GUIContent("Add Child/Condition"), false, () => AddChild(tag, TagType.Condition)); m.AddItem(new GUIContent("Add Child/ConditionGroup"), false, () => AddChild(tag, TagType.ConditionGroup)); m.AddSeparator(""); }
        m.AddItem(new GUIContent("Duplicate"), false, () => Dup(tag, list, idx));
        m.AddSeparator("");
        if (idx > 0) m.AddItem(new GUIContent("Move Up"), false, () => { list.RemoveAt(idx); list.Insert(idx - 1, tag); UpdateXml(); }); else m.AddDisabledItem(new GUIContent("Move Up"));
        if (idx < list.Count - 1) m.AddItem(new GUIContent("Move Down"), false, () => { list.RemoveAt(idx); list.Insert(idx + 1, tag); UpdateXml(); }); else m.AddDisabledItem(new GUIContent("Move Down"));
        m.AddSeparator("");
        m.AddItem(new GUIContent("Delete"), false, () => { list.RemoveAt(idx); if (selectedTag == tag) selectedTag = null; UpdateXml(); });
        m.ShowAsContext();
    }

    private void AddChild(OverlayTag p, TagType t) { var n = new OverlayTag(t) { parent = p }; p.children.Add(n); p.isExpanded = true; selectedTag = n; UpdateXml(); }
    private void Dup(OverlayTag t, List<OverlayTag> l, int i) { var d = Clone(t); d.parent = t.parent; l.Insert(i + 1, d); selectedTag = d; UpdateXml(); }
    private OverlayTag Clone(OverlayTag o) { var c = new OverlayTag(o.type) { className = o.className, style = o.style, content = o.content, onTapEvent = o.onTapEvent, src = o.src, action = o.action, source = o.source, itemName = o.itemName, sortKey = o.sortKey, order = o.order, limit = o.limit, startIndex = o.startIndex, scrollStyle = o.scrollStyle, scrollIndex = o.scrollIndex, lhs = o.lhs, conditionOperator = o.conditionOperator, rhs = o.rhs, groupOperator = o.groupOperator, isExpanded = o.isExpanded }; foreach (var ch in o.children) { var cc = Clone(ch); cc.parent = c; c.children.Add(cc); } return c; }

    private void HandleKeyboardInput() { var e = Event.current; if (e.type == EventType.KeyDown && selectedTag != null && e.keyCode == KeyCode.Delete && FindTag(selectedTag, out var l, out var i)) { l.RemoveAt(i); selectedTag = null; UpdateXml(); e.Use(); } }
    private bool FindTag(OverlayTag t, out List<OverlayTag> l, out int i) { i = rootTags.IndexOf(t); if (i >= 0) { l = rootTags; return true; } return FindInKids(rootTags, t, out l, out i); }
    private bool FindInKids(List<OverlayTag> tags, OverlayTag tgt, out List<OverlayTag> l, out int i) { foreach (var t in tags) if (t.CanHaveChildren()) { i = t.children.IndexOf(tgt); if (i >= 0) { l = t.children; return true; } if (FindInKids(t.children, tgt, out l, out i)) return true; } l = null; i = -1; return false; }

    private void DrawDetailArea()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(detailHeight));
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Tag Details", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);
        if (selectedTag != null)
        {
            EditorGUI.BeginChangeCheck();
            GUI.backgroundColor = GetTagColor(selectedTag.type);
            EditorGUILayout.LabelField($"Type: {selectedTag.type}", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(5);
            if (selectedTag.type == TagType.View || selectedTag.type == TagType.Text || selectedTag.type == TagType.Image || selectedTag.type == TagType.Button)
            { selectedTag.className = EditorGUILayout.TextField(new GUIContent("className", T.ClassName), selectedTag.className); selectedTag.style = EditorGUILayout.TextField(new GUIContent("style", T.Style), selectedTag.style); EditorGUILayout.Space(3); }
            switch (selectedTag.type)
            {
                case TagType.View: selectedTag.onTapEvent = EditorGUILayout.TextField(new GUIContent("onTapEvent", T.OnTapEvent), selectedTag.onTapEvent); EditorGUILayout.HelpBox($"Container: {selectedTag.children.Count} children\nConverts to <div>", MessageType.Info); break;
                case TagType.Text: EditorGUILayout.LabelField(new GUIContent("content", T.Content)); selectedTag.content = EditorGUILayout.TextArea(selectedTag.content, GUILayout.Height(50)); EditorGUILayout.HelpBox("Converts to <p>\n{{variable}} supported", MessageType.Info); break;
                case TagType.Image: selectedTag.src = EditorGUILayout.TextField(new GUIContent("src", T.Src), selectedTag.src); EditorGUILayout.HelpBox("Converts to <img>\nFBInstant.player.photo for profile", MessageType.Info); break;
                case TagType.Button: EditorGUILayout.LabelField(new GUIContent("content", T.Content)); selectedTag.content = EditorGUILayout.TextArea(selectedTag.content, GUILayout.Height(40)); selectedTag.action = EditorGUILayout.TextField(new GUIContent("action", T.Action), selectedTag.action); EditorGUILayout.HelpBox("Converts to <button>, only accepts {{FBInstant.action.switchContext(id)}} and {{FBInstant.action.contextCreate(id)}}", MessageType.Info); break;
                case TagType.For:
                    selectedTag.source = EditorGUILayout.TextField(new GUIContent("source", T.Source), selectedTag.source);
                    selectedTag.itemName = EditorGUILayout.TextField(new GUIContent("itemName", T.ItemName), selectedTag.itemName);
                    EditorGUILayout.LabelField("Optional", EditorStyles.miniBoldLabel);
                    selectedTag.sortKey = EditorGUILayout.TextField(new GUIContent("sortKey", T.SortKey), selectedTag.sortKey);
                    selectedTag.order = (SortOrder)EditorGUILayout.EnumPopup(new GUIContent("order", T.Order), selectedTag.order);
                    selectedTag.limit = EditorGUILayout.TextField(new GUIContent("limit", T.Limit), selectedTag.limit);
                    selectedTag.startIndex = EditorGUILayout.TextField(new GUIContent("startIndex", T.StartIndex), selectedTag.startIndex);
                    selectedTag.scrollStyle = (ScrollStyle)EditorGUILayout.EnumPopup(new GUIContent("scrollStyle", T.ScrollStyleTip), selectedTag.scrollStyle);
                    if (selectedTag.scrollStyle == ScrollStyle.center) selectedTag.scrollIndex = EditorGUILayout.TextField(new GUIContent("scrollIndex", T.ScrollIndex), selectedTag.scrollIndex);
                    EditorGUILayout.HelpBox($"Loop: {selectedTag.children.Count} children", MessageType.Info); break;
                case TagType.If: EditorGUILayout.HelpBox($"Conditional: {selectedTag.children.Count} children\nAdd Condition child", MessageType.Info); break;
                case TagType.ElseIf: EditorGUILayout.HelpBox($"Else-If: {selectedTag.children.Count} children\nMust follow If", MessageType.Info); break;
                case TagType.Else: EditorGUILayout.HelpBox($"Else: {selectedTag.children.Count} children\nLast in chain", MessageType.Info); break;
                case TagType.Condition:
                    selectedTag.lhs = EditorGUILayout.TextField(new GUIContent("lhs", T.Lhs), selectedTag.lhs);
                    selectedTag.conditionOperator = (ConditionOperator)EditorGUILayout.EnumPopup(new GUIContent("operator", T.Operator), selectedTag.conditionOperator);
                    selectedTag.rhs = EditorGUILayout.TextField(new GUIContent("rhs", T.Rhs), selectedTag.rhs);
                    EditorGUILayout.HelpBox("Use in If/ElseIf/ConditionGroup", MessageType.Info); break;
                case TagType.ConditionGroup:
                    selectedTag.groupOperator = (GroupOperator)EditorGUILayout.EnumPopup(new GUIContent("operator", T.GroupOp), selectedTag.groupOperator);
                    EditorGUILayout.HelpBox($"Group: {selectedTag.children.Count} conditions\nAND/OR", MessageType.Info); break;
            }
            if (EditorGUI.EndChangeCheck()) UpdateXml();
        }
        else EditorGUILayout.HelpBox("Select a tag to edit.", MessageType.Info);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawOutputArea()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Generated XML", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        outputScrollPos = EditorGUILayout.BeginScrollView(outputScrollPos);
        EditorStyles.textArea.wordWrap = true;
        EditorGUILayout.TextArea(generatedXml, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy", GUILayout.Height(28))) { EditorGUIUtility.systemCopyBuffer = generatedXml; ShowNotification(new GUIContent("Copied!")); }
        if (GUILayout.Button("Clear All", GUILayout.Height(28)) && EditorUtility.DisplayDialog("Clear", "Clear all?", "Yes", "No")) { rootTags.Clear(); selectedTag = null; UpdateXml(); }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void UpdateXml() { var sb = new StringBuilder(); foreach (var t in rootTags) GenXml(t, sb, 0); generatedXml = sb.ToString(); Repaint(); }
    private void GenXml(OverlayTag t, StringBuilder sb, int ind)
    {
        var s = new string(' ', ind * 2);
        sb.Append($"{s}<{t.type}");
        AppendAttr(t, sb);
        if (t.CanHaveChildren() && t.children.Count > 0) { sb.AppendLine(">"); foreach (var c in t.children) GenXml(c, sb, ind + 1); sb.AppendLine($"{s}</{t.type}>"); }
        else if (t.CanHaveChildren()) sb.AppendLine($"></{t.type}>");
        else sb.AppendLine(" />");
    }

    private void AppendAttr(OverlayTag t, StringBuilder sb)
    {
        if (!string.IsNullOrEmpty(t.className)) sb.Append($" className=\"{Esc(t.className)}\"");
        if (!string.IsNullOrEmpty(t.style)) sb.Append($" style=\"{Esc(t.style)}\"");
        switch (t.type)
        {
            case TagType.View: if (!string.IsNullOrEmpty(t.onTapEvent)) sb.Append($" onTapEvent=\"{Esc(t.onTapEvent)}\""); break;
            case TagType.Text: if (!string.IsNullOrEmpty(t.content)) sb.Append($" content=\"{Esc(t.content)}\""); break;
            case TagType.Image: if (!string.IsNullOrEmpty(t.src)) sb.Append($" src=\"{Esc(t.src)}\""); break;
            case TagType.Button: if (!string.IsNullOrEmpty(t.content)) sb.Append($" content=\"{Esc(t.content)}\""); if (!string.IsNullOrEmpty(t.action)) sb.Append($" action=\"{Esc(t.action)}\""); break;
            case TagType.For:
                if (!string.IsNullOrEmpty(t.source)) sb.Append($" source=\"{Esc(t.source)}\"");
                if (!string.IsNullOrEmpty(t.itemName)) sb.Append($" itemName=\"{Esc(t.itemName)}\"");
                if (!string.IsNullOrEmpty(t.sortKey)) { sb.Append($" sortKey=\"{Esc(t.sortKey)}\""); sb.Append($" order=\"{t.order}\""); }
                if (!string.IsNullOrEmpty(t.limit)) sb.Append($" limit=\"{Esc(t.limit)}\"");
                if (!string.IsNullOrEmpty(t.startIndex)) sb.Append($" startIndex=\"{Esc(t.startIndex)}\"");
                if (t.scrollStyle != ScrollStyle.top) sb.Append($" scrollStyle=\"{t.scrollStyle}\"");
                if (t.scrollStyle == ScrollStyle.center && !string.IsNullOrEmpty(t.scrollIndex)) sb.Append($" scrollIndex=\"{Esc(t.scrollIndex)}\"");
                break;
            case TagType.Condition: if (!string.IsNullOrEmpty(t.lhs)) sb.Append($" lhs=\"{Esc(t.lhs)}\""); sb.Append($" operator=\"{t.conditionOperator}\""); if (!string.IsNullOrEmpty(t.rhs)) sb.Append($" rhs=\"{Esc(t.rhs)}\""); break;
            case TagType.ConditionGroup: sb.Append($" operator=\"{t.groupOperator}\""); break;
        }
    }

    private string Esc(string v) => v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private void HandleXmlFileDragAndDrop()
    {
        var e = Event.current;
        var area = new Rect(0, 0, position.width, position.height);
        if (e.type == EventType.DragUpdated && area.Contains(e.mousePosition)) { foreach (var p in DragAndDrop.paths) if (p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; e.Use(); break; } }
        else if (e.type == EventType.DragPerform && area.Contains(e.mousePosition)) { DragAndDrop.AcceptDrag(); foreach (var p in DragAndDrop.paths) if (p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) { LoadXml(p); break; } e.Use(); }
    }

    private void LoadXml(string path)
    {
        try
        {
            var doc = new XmlDocument(); doc.LoadXml(File.ReadAllText(path));
            rootTags.Clear(); selectedTag = null;
            foreach (XmlNode n in doc.ChildNodes) if (n.NodeType == XmlNodeType.Element) { var t = Parse(n, null); if (t != null) rootTags.Add(t); }
            UpdateXml();
            ShowNotification(new GUIContent($"Loaded: {Path.GetFileName(path)}"));
        }
        catch (Exception ex) { Debug.LogError($"XML load failed: {ex.Message}"); ShowNotification(new GUIContent("Load failed")); }
    }

    private OverlayTag Parse(XmlNode n, OverlayTag p)
    {
        if (n.NodeType != XmlNodeType.Element) return null;
        TagType type;
        switch (n.Name.ToLower()) { case "view": type = TagType.View; break; case "text": type = TagType.Text; break; case "image": type = TagType.Image; break; case "button": type = TagType.Button; break; case "for": type = TagType.For; break; case "if": type = TagType.If; break; case "elseif": type = TagType.ElseIf; break; case "else": type = TagType.Else; break; case "condition": type = TagType.Condition; break; case "conditiongroup": type = TagType.ConditionGroup; break; default: return null; }
        var t = new OverlayTag(type) { parent = p };
        if (n.Attributes != null) foreach (XmlAttribute a in n.Attributes)
            {
                switch (a.Name.ToLower())
                {
                    case "classname": t.className = a.Value; break;
                    case "style": t.style = a.Value; break;
                    case "content": t.content = a.Value; break;
                    case "ontapevent": t.onTapEvent = a.Value; break;
                    case "src": t.src = a.Value; break;
                    case "action": t.action = a.Value; break;
                    case "source": t.source = a.Value; break;
                    case "itemname": t.itemName = a.Value; break;
                    case "sortkey": t.sortKey = a.Value; break;
                    case "order": if (Enum.TryParse<SortOrder>(a.Value, true, out var o)) t.order = o; break;
                    case "limit": t.limit = a.Value; break;
                    case "startindex": t.startIndex = a.Value; break;
                    case "scrollstyle": if (Enum.TryParse<ScrollStyle>(a.Value, true, out var ss)) t.scrollStyle = ss; break;
                    case "scrollindex": t.scrollIndex = a.Value; break;
                    case "lhs": t.lhs = a.Value; break;
                    case "rhs": t.rhs = a.Value; break;
                    case "operator": if (type == TagType.Condition && Enum.TryParse<ConditionOperator>(a.Value, true, out var co)) t.conditionOperator = co; else if (type == TagType.ConditionGroup && Enum.TryParse<GroupOperator>(a.Value, true, out var go)) t.groupOperator = go; break;
                }
            }
        if (t.CanHaveChildren() && n.HasChildNodes) foreach (XmlNode cn in n.ChildNodes) { var ct = Parse(cn, t); if (ct != null) t.children.Add(ct); }
        return t;
    }
}
