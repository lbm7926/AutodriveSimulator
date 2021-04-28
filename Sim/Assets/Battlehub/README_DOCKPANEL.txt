The Dock Panel is a control, that provides an easy docking of content regions to the left, right, top, bottom or center of the panel. 
The control also allow region to become an independent floating window, modal popup or dialog.

Getting Started: 

1. Create Canvas.
2. Add DockPanel.prefab from Assets/Battelhub/UIControls/DockPanels/Prefabs to hierarchy.
3. Create GettingStarted.cs script and add it to DockPanel Game Object.
4. Hit Play.

using Battlehub.UIControls.DockPanels;
using UnityEngine;
using UnityEngine.UI;

public class GettingStarted : MonoBehaviour
{
    DockPanel m_dockPanel;

    void Start()
    {
        m_dockPanel = GetComponent<DockPanel>();

        GameObject testContent = new GameObject();
        testContent.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1.0f);

        LayoutInfo layout = new LayoutInfo(false,
            new LayoutInfo(testContent.transform, "Tab 1"),
                new LayoutInfo(
                    isVertical: true,
                        child0: new LayoutInfo(Instantiate(testContent).transform, "Tab 2"),
                        child1: new LayoutInfo(Instantiate(testContent).transform, "Tab 3"),
                        ratio: 0.5f),
                0.5f);

        m_dockPanel.RootRegion.Build(layout);
    }              
}

Full demo can be found in Assets/Battelhub/UIControls/DockPanels/Demo folder

For more info see online documentation: http://rteditor.battlehub.net/dock-panels/
