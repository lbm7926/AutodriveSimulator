using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTTerrain;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGeneralSettings : MonoBehaviour
{
    [SerializeField]
    private Toggle m_handlesToggle = null;

    [SerializeField]
    private VirtualizingTreeView m_commandsList = null;

    [SerializeField]
    private BoolEditor m_zTestEditor = null;

    [SerializeField]
    private RangeIntEditor m_spacingEditor = null;

    private ToolCmd[] m_commands;
    private ITerrainTool m_terrainTool;
    private IRTE m_editor;

    private bool m_isTerrainSelected = false;
    private bool m_isTerrainHandleSelected = false;

    private void Awake()
    {
        m_editor = IOC.Resolve<IRTE>();

        m_terrainTool = IOC.Resolve<ITerrainTool>();

        m_editor.Selection.SelectionChanged += OnSelectionChanged;

        m_commandsList.ItemClick += OnItemClick;
        m_commandsList.ItemDataBinding += OnItemDataBinding;
        m_commandsList.ItemExpanding += OnItemExpanding;
        m_commandsList.ItemBeginDrag += OnItemBeginDrag;
        m_commandsList.ItemDrop += OnItemDrop;
        m_commandsList.ItemDragEnter += OnItemDragEnter;
        m_commandsList.ItemDragExit += OnItemDragExit;
        m_commandsList.ItemEndDrag += OnItemEndDrag;

        m_commandsList.CanEdit = false;
        m_commandsList.CanReorder = false;
        m_commandsList.CanReparent = false;
        m_commandsList.CanSelectAll = false;
        m_commandsList.CanUnselectAll = true;
        m_commandsList.CanRemove = false;

        if (m_spacingEditor != null)
        {
            m_spacingEditor.Min = 5;
            m_spacingEditor.Max = 40;
            m_spacingEditor.Init(m_terrainTool, m_terrainTool, Strong.PropertyInfo((ITerrainTool x) => x.Spacing), null, "Spacing");
        }

        if (m_zTestEditor != null)
        {
            m_zTestEditor.Init(m_terrainTool, m_terrainTool, Strong.PropertyInfo((ITerrainTool x) => x.EnableZTest), null, "ZTest");
        }

        if(m_handlesToggle != null)
        {
            m_handlesToggle.onValueChanged.AddListener(OnHandlesToggleValueChanged);
        }
    }

    private void OnDestroy()
    {
        if (m_editor != null)
        {
            m_editor.Selection.SelectionChanged -= OnSelectionChanged;
        }

        if (m_commandsList != null)
        {
            m_commandsList.ItemClick -= OnItemClick;
            m_commandsList.ItemDataBinding -= OnItemDataBinding;
            m_commandsList.ItemExpanding -= OnItemExpanding;
            m_commandsList.ItemBeginDrag -= OnItemBeginDrag;
            m_commandsList.ItemDrop -= OnItemDrop;
            m_commandsList.ItemDragEnter -= OnItemDragEnter;
            m_commandsList.ItemDragExit -= OnItemDragExit;
            m_commandsList.ItemEndDrag -= OnItemEndDrag;
        }

        if (m_handlesToggle != null)
        {
            m_handlesToggle.onValueChanged.RemoveListener(OnHandlesToggleValueChanged);
        }
    }

    private void Start()
    {
        UpdateFlags();
        m_commands = GetCommands().ToArray();
        m_commandsList.Items = m_commands;
    }

    private void OnEnable()
    {
        m_terrainTool.Enabled = true;
    }

    private void OnDisable()
    {
        if(m_terrainTool != null)
        {
            m_terrainTool.Enabled = false;
        }
    }

    private List<ToolCmd> GetCommands()
    {
        return new List<ToolCmd>()
            {
                new ToolCmd("Reset Position", () => m_terrainTool.ResetPosition(), () => m_isTerrainHandleSelected),
                new ToolCmd("Cut Holes", () => m_terrainTool.CutHoles(), () => m_editor.Selection.Length > 0)
            };
    }


    private void UpdateFlags()
    {
        GameObject[] selected = m_editor.Selection.gameObjects;
        if (selected != null && selected.Length > 0)
        {
            m_isTerrainSelected = selected.Where(go => go.GetComponent<Terrain>() != null).Any();
            m_isTerrainHandleSelected = selected.Where(go => go.GetComponent<TerrainToolHandle>() != null).Any();
        }
        else
        {
            m_isTerrainSelected = false;
            m_isTerrainHandleSelected = false;
        }
    }

    private void OnSelectionChanged(UnityEngine.Object[] unselectedObjects)
    {
        UpdateFlags();
        m_commandsList.DataBindVisible();
    }

    private void OnProBuilderToolSelectionChanged()
    {
        UpdateFlags();
        m_commandsList.DataBindVisible();
    }

    private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
    {
        TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>();
        ToolCmd cmd = (ToolCmd)e.Item;
        text.text = cmd.Text;

        bool isValid = cmd.Validate();
        Color color = text.color;
        color.a = isValid ? 1 : 0.5f;
        text.color = color;

        e.CanDrag = cmd.CanDrag;
        e.HasChildren = cmd.HasChildren;
    }

    private void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
    {
        ToolCmd cmd = (ToolCmd)e.Item;
        e.Children = cmd.Children;
    }

    private void OnItemClick(object sender, ItemArgs e)
    {
        ToolCmd cmd = (ToolCmd)e.Items[0];
        if (cmd.Validate())
        {
            cmd.Run();
        }
    }

    private void OnItemBeginDrag(object sender, ItemArgs e)
    {
        m_editor.DragDrop.RaiseBeginDrag(this, e.Items, e.PointerEventData);
    }

    private void OnItemDragEnter(object sender, ItemDropCancelArgs e)
    {
        m_editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
        e.Cancel = true;
    }

    private void OnItemDrag(object sender, ItemArgs e)
    {
        m_editor.DragDrop.RaiseDrag(e.PointerEventData);
    }

    private void OnItemDragExit(object sender, EventArgs e)
    {
        m_editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
    }

    private void OnItemDrop(object sender, ItemDropArgs e)
    {
        m_editor.DragDrop.RaiseDrop(e.PointerEventData);
    }

    private void OnItemEndDrag(object sender, ItemArgs e)
    {
        m_editor.DragDrop.RaiseDrop(e.PointerEventData);
    }

    private void OnHandlesToggleValueChanged(bool value)
    {
        m_terrainTool.Enabled = value;
    }
}
